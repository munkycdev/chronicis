using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Chronicis.Shared.Extensions;

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
    private readonly string? _customDomain;

    public BlobStorageService(
        IConfiguration configuration,
        ILogger<BlobStorageService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        var connectionString = configuration["BlobStorage:ConnectionString"]
            ?? configuration["BlobStorage__ConnectionString"];  // Try double underscore format

        if (string.IsNullOrEmpty(connectionString))
        {
            _logger.LogError("BlobStorage:ConnectionString not configured. Check Azure app settings.");
            throw new InvalidOperationException("BlobStorage:ConnectionString not configured. Please add BlobStorage__ConnectionString to Azure configuration.");
        }

        _containerName = configuration["BlobStorage:ContainerName"]
            ?? configuration["BlobStorage__ContainerName"]
            ?? "chronicis-documents";

        // Optional custom domain (e.g., "http://docs.chronicis.app" or "https://docs.chronicis.app")
        _customDomain = configuration["BlobStorage:CustomDomain"]
            ?? configuration["BlobStorage__CustomDomain"];

        if (!string.IsNullOrEmpty(_customDomain))
        {
            _logger.LogDebug("Using custom domain for blob URLs: {CustomDomain}", _customDomain);
        }

        try
        {
            _blobServiceClient = new BlobServiceClient(connectionString);

            // Ensure container exists (idempotent)
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            containerClient.CreateIfNotExists(PublicAccessType.None);

            _logger.LogDebug("BlobStorageService initialized with container: {ContainerName}", _containerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize BlobStorageService. Connection string may be invalid.");
            throw;
        }
    }

    /// <inheritdoc/>
    public string BuildBlobPath(Guid worldId, Guid documentId, string fileName)
    {
        // Sanitize filename: remove path separators, keep only safe chars
        var sanitized = SanitizeFileName(fileName);
        return $"worlds/{worldId}/documents/{documentId}/{sanitized}";
    }

    /// <inheritdoc/>
    public Task<string> GenerateUploadSasUrlAsync(
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

        var sasUrl = BuildSasUrl(blobClient, sasBuilder);

        _logger.LogDebugSanitized("Generated upload SAS URL for blob: {BlobPath}", blobPath);

        return Task.FromResult(sasUrl);
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
                _logger.LogWarningSanitized("Blob not found: {BlobPath}", blobPath);
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
            _logger.LogErrorSanitized(ex, "Error getting blob metadata for: {BlobPath}", blobPath);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<Stream> OpenReadAsync(string blobPath)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient(blobPath);

        if (!await blobClient.ExistsAsync())
        {
            throw new FileNotFoundException($"Blob not found: {blobPath}");
        }

        return await blobClient.OpenReadAsync();
    }

    /// <inheritdoc/>
    public async Task DeleteBlobAsync(string blobPath)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(blobPath);

            await blobClient.DeleteIfExistsAsync();

            _logger.LogDebugSanitized("Deleted blob: {BlobPath}", blobPath);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogErrorSanitized(ex, "Error deleting blob: {BlobPath}", blobPath);
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<string> GenerateDownloadSasUrlAsync(string blobPath)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = containerClient.GetBlobClient(blobPath);

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

        var sasUrl = BuildSasUrl(blobClient, sasBuilder);

        _logger.LogDebugSanitized("Generated download SAS URL for blob: {BlobPath}", blobPath);

        return Task.FromResult(sasUrl);
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

    /// <summary>
    /// Build a SAS URL using either the custom domain or the default blob endpoint.
    /// </summary>
    private string BuildSasUrl(BlobClient blobClient, BlobSasBuilder sasBuilder)
    {
        if (!string.IsNullOrEmpty(_customDomain))
        {
            // Generate SAS token only (query string)
            var sasToken = blobClient.GenerateSasUri(sasBuilder).Query;

            // Build custom URL: {customDomain}/{container}/{blobPath}?{sasToken}
            var customUrl = $"{_customDomain.TrimEnd('/')}/{_containerName}/{blobClient.Name}{sasToken}";

            return customUrl;
        }
        else
        {
            // Use default blob endpoint with SAS
            return blobClient.GenerateSasUri(sasBuilder).ToString();
        }
    }
}
