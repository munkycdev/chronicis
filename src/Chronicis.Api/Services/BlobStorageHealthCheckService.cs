using Azure.Storage.Blobs;
using Chronicis.Shared.DTOs;

namespace Chronicis.Api.Services;

public class BlobStorageHealthCheckService : HealthCheckServiceBase
{
    private readonly IConfiguration _configuration;

    public BlobStorageHealthCheckService(
        IConfiguration configuration,
        ILogger<BlobStorageHealthCheckService> logger)
        : base(logger)
    {
        _configuration = configuration;
    }

    protected override async Task<(string Status, string? Message)> PerformHealthCheckAsync()
    {
        var connectionString = _configuration["BlobStorage:ConnectionString"];
        var containerName = _configuration["BlobStorage:ContainerName"];

        if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(containerName))
        {
            return (HealthStatus.Unhealthy, "Blob storage configuration missing");
        }

        try
        {
            var blobServiceClient = new BlobServiceClient(connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            // Check if container exists and is accessible
            var exists = await containerClient.ExistsAsync();

            if (!exists)
            {
                return (HealthStatus.Unhealthy, "Container does not exist or is not accessible");
            }

            return (HealthStatus.Healthy, "Blob storage accessible");
        }
        catch (Exception ex)
        {
            return (HealthStatus.Unhealthy, $"Blob storage error: {ex.Message}");
        }
    }
}
