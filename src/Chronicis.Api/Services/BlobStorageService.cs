using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Chronicis.Api.Services;

/// <summary>
/// Azure Blob Storage service for managing world document files.
/// </summary>
public class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BlobStorageService> _logger;
    private readonly string _containerName;

    public BlobStorageService(
        IConfiguration configuration,
        ILogger<BlobStorageService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        var connectionString = configuration["BlobStorage:ConnectionString"]
            ?? throw new InvalidOperationException("BlobStorage:ConnectionString not configured");

        _containerName = configuration["BlobStorage:ContainerName"] ?? "chronicis-documents";

        _blobServiceClient = new BlobServiceClient(connectionString);

        // Ensure container exists (idempotent)
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        containerClient.CreateIfNotExists(PublicAccessType.None);

        _logger.LogInformation("BlobStorageService initialized with container: {ContainerName}", _containerName);
    }

    /// <inheritdoc/>
    public string BuildBlobPath(Guid worldId, Guid documentId, string fileName)
    {
        // Sanitize filename: remove path separators, keep only safe chars
        var sanitized = SanitizeFileName(fileName);
        return $"worlds/{worldId}/documents/{documentId}/{sanitized}";
    }

    /// <inheritdoc/>
    public async Task<string> GenerateUploadSasUrlAsync(
        Guid worldId,
        Guid documentId,
        string fileName,
        string contentType)
    {
        var blobPath = BuildBlobPath(worldId, documentId, fileName);
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient(blobPath);

        // Generate SAS token with write permissions, 15-minute expiry
        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _containerName,
            BlobName = blobPath,
            Resource = "b", // blob
            StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5), // Allow for clock skew
            ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(15),
        };

        sasBuilder.SetPermissions(BlobSasPermissions.Create | BlobSasPermissions.Write);

        var sasToken = blobClient.GenerateSasUri(sasBuilder);

        _logger.LogInformation("Generated upload SAS URL for blob: {BlobPath}", blobPath);

        return sasToken.ToString();
    }

    /// <inheritdoc/>
    public async Task<string> GenerateDownloadSasUrlAsync(string blobPath)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient(blobPath);

        // Verify blob exists
        if (!await blobClient.ExistsAsync())
        {
            throw new FileNotFoundException($"Blob not found: {blobPath}");
        }

        // Generate SAS token with read permissions, 15-minute expiry
        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _containerName,
            BlobName = blobPath,
            Resource = "b", // blob
            StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5), // Allow for clock skew
            ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(15),
        };

        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        var sasToken = blobClient.GenerateSasUri(sasBuilder);

        _logger.LogInformation("Generated download SAS URL for blob: {BlobPath}", blobPath);

        return sasToken.ToString();
    }

    /// <inheritdoc/>
    public async Task<BlobMetadata?> GetBlobMetadataAsync(string blobPath)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(blobPath);

            if (!await blobClient.ExistsAsync())
            {
                _logger.LogWarning("Blob not found: {BlobPath}", blobPath);
                return null;
            }

            var properties = await blobClient.GetPropertiesAsync();

            return new BlobMetadata
            {
                SizeBytes = properties.Value.ContentLength,
                ContentType = properties.Value.ContentType
            };
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Error getting blob metadata for: {BlobPath}", blobPath);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task DeleteBlobAsync(string blobPath)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(blobPath);

            await blobClient.DeleteIfExistsAsync();

            _logger.LogInformation("Deleted blob: {BlobPath}", blobPath);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Error deleting blob: {BlobPath}", blobPath);
            throw;
        }
    }

    private static string SanitizeFileName(string fileName)
    {
        // Remove path separators and keep only safe characters
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));

        // Limit length
        if (sanitized.Length > 200)
        {
            var extension = Path.GetExtension(sanitized);
            var nameWithoutExt = Path.GetFileNameWithoutExtension(sanitized);
            sanitized = nameWithoutExt[..(200 - extension.Length)] + extension;
        }

        return sanitized;
    }
}
