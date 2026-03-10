using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace Chronicis.Api.Services;

/// <summary>
/// Azure Blob Storage service for managing map basemap files.
/// </summary>
public sealed class AzureBlobMapBlobStore : IMapBlobStore
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<AzureBlobMapBlobStore> _logger;
    private const string ContainerName = "chronicis-maps";

    public AzureBlobMapBlobStore(
        IConfiguration configuration,
        ILogger<AzureBlobMapBlobStore> logger)
    {
        _logger = logger;

        var connectionString = configuration["BlobStorage:ConnectionString"]
            ?? configuration["BlobStorage__ConnectionString"];

        if (string.IsNullOrEmpty(connectionString))
        {
            _logger.LogErrorSanitized("BlobStorage:ConnectionString not configured.");
            throw new InvalidOperationException("BlobStorage:ConnectionString not configured. Please add BlobStorage__ConnectionString to Azure configuration.");
        }

        try
        {
            _blobServiceClient = new BlobServiceClient(connectionString);

            var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
            containerClient.CreateIfNotExists(PublicAccessType.None);

            _logger.LogTraceSanitized("AzureBlobMapBlobStore initialized with container: {ContainerName}", ContainerName);
        }
        catch (Exception ex)
        {
            _logger.LogErrorSanitized(ex, "Failed to initialize AzureBlobMapBlobStore. Connection string may be invalid.");
            throw;
        }
    }

    /// <inheritdoc/>
    public string BuildBasemapBlobKey(Guid mapId, string fileName)
    {
        var sanitized = SanitizeFileName(fileName);
        return $"maps/{mapId}/basemap/{sanitized}";
    }

    /// <inheritdoc/>
    public Task<string> GenerateUploadSasUrlAsync(Guid mapId, string fileName, string contentType)
    {
        var blobKey = BuildBasemapBlobKey(mapId, fileName);
        var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
        var blobClient = containerClient.GetBlobClient(blobKey);

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = ContainerName,
            BlobName = blobKey,
            Resource = "b",
            StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
            ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(15),
        };

        sasBuilder.SetPermissions(BlobSasPermissions.Create | BlobSasPermissions.Write);

        var sasUrl = blobClient.GenerateSasUri(sasBuilder).ToString();

        _logger.LogTraceSanitized("Generated upload SAS URL for blob: {BlobKey}", blobKey);

        return Task.FromResult(sasUrl);
    }

    /// <inheritdoc/>
    public Task<string> GenerateReadSasUrlAsync(string blobKey)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
        var blobClient = containerClient.GetBlobClient(blobKey);

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = ContainerName,
            BlobName = blobKey,
            Resource = "b",
            StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
            ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(15),
        };

        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        var sasUrl = blobClient.GenerateSasUri(sasBuilder).ToString();

        _logger.LogTraceSanitized("Generated read SAS URL for blob: {BlobKey}", blobKey);

        return Task.FromResult(sasUrl);
    }

    /// <inheritdoc/>
    public async Task DeleteMapFolderAsync(Guid mapId)
    {
        var prefix = $"maps/{mapId}/";
        var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
        var deletedCount = 0;

        await foreach (var blobItem in containerClient.GetBlobsAsync(prefix: prefix))
        {
            var blobClient = containerClient.GetBlobClient(blobItem.Name);
            await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
            deletedCount++;
        }

        _logger.LogTraceSanitized(
            "Deleted {DeletedCount} blob(s) for map folder prefix {Prefix}",
            deletedCount,
            prefix);
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));

        if (sanitized.Length > 200)
        {
            var extension = Path.GetExtension(sanitized);
            var nameWithoutExt = Path.GetFileNameWithoutExtension(sanitized);
            sanitized = nameWithoutExt[..(200 - extension.Length)] + extension;
        }

        return sanitized;
    }
}
