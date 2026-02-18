using System.Runtime.CompilerServices;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Chronicis.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chronicis.Api.Tests;

public class BlobStorageServiceBranchCoverageTests
{
    [Fact]
    public void BlobStorageService_ConstructorAndPrivateHelpers_CoverRemainingBranches()
    {
        var logger = NullLogger<BlobStorageService>.Instance;

        var missingConfig = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        Assert.Throws<InvalidOperationException>(() => new BlobStorageService(missingConfig, logger));

        var primaryConnectionConfig = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["BlobStorage:ConnectionString"] = "not-a-valid-connection-string",
            ["BlobStorage:CustomDomain"] = "https://docs.example.test"
        }).Build();
        Assert.ThrowsAny<Exception>(() => new BlobStorageService(primaryConnectionConfig, logger));

        var alternateConnectionConfig = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["BlobStorage__ConnectionString"] = "not-a-valid-connection-string"
        }).Build();
        Assert.ThrowsAny<Exception>(() => new BlobStorageService(alternateConnectionConfig, logger));

        var sanitize = RemainingApiBranchCoverageTestHelpers.GetMethod(typeof(BlobStorageService), "SanitizeFileName");
        var longFile = new string('x', 210) + ".txt";
        var sanitized = (string)sanitize.Invoke(null, [longFile])!;
        Assert.True(sanitized.Length <= 200);

        var buildSas = RemainingApiBranchCoverageTestHelpers.GetMethod(typeof(BlobStorageService), "BuildSasUrl");
        var blobService = (BlobStorageService)RuntimeHelpers.GetUninitializedObject(typeof(BlobStorageService));

        RemainingApiBranchCoverageTestHelpers.SetField(blobService, "_containerName", "container");

        var key = Convert.ToBase64String(Enumerable.Repeat((byte)1, 32).ToArray());
        var cred = new StorageSharedKeyCredential("account", key);
        var blobClient = new BlobClient(new Uri("https://account.blob.core.windows.net/container/file.txt"), cred);
        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = "container",
            BlobName = "file.txt",
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(5)
        };
        sasBuilder.SetPermissions(BlobSasPermissions.Read);

        RemainingApiBranchCoverageTestHelpers.SetField(blobService, "_customDomain", "https://docs.example.test");
        var customUrl = (string)buildSas.Invoke(blobService, [blobClient, sasBuilder])!;
        Assert.StartsWith("https://docs.example.test/container/file.txt?", customUrl);

        RemainingApiBranchCoverageTestHelpers.SetField<string?>(blobService, "_customDomain", null);
        var defaultUrl = (string)buildSas.Invoke(blobService, [blobClient, sasBuilder])!;
        Assert.StartsWith("https://account.blob.core.windows.net/container/file.txt?", defaultUrl);
    }
}
