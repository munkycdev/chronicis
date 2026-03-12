using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
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
    public async Task BlobStorageService_Constructor_CanCompleteWithLocalBlobEndpoint()
    {
        using var server = RawAzureConstructorServer.Start();

        var accountKey = Convert.ToBase64String(Enumerable.Repeat((byte)3, 32).ToArray());
        var connectionString =
            $"DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey={accountKey};BlobEndpoint={server.ServiceBaseUri}";

        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["BlobStorage:ConnectionString"] = connectionString,
            ["BlobStorage:ContainerName"] = "documents"
        }).Build();

        var service = new BlobStorageService(config, NullLogger<BlobStorageService>.Instance);
        await server.Completion;

        Assert.NotNull(service);
    }

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
        var sanitizedUnsafe = (string)sanitize.Invoke(null, ["folder/sub\\file?.txt"])!;
        foreach (var invalidChar in Path.GetInvalidFileNameChars())
        {
            Assert.DoesNotContain(invalidChar, sanitizedUnsafe);
        }

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

    [Fact]
    public async Task BlobStorageService_PublicSasMethods_CoverNonNetworkPaths()
    {
        var blobService = (BlobStorageService)RuntimeHelpers.GetUninitializedObject(typeof(BlobStorageService));
        RemainingApiBranchCoverageTestHelpers.SetField(blobService, "_containerName", "container");
        RemainingApiBranchCoverageTestHelpers.SetField(blobService, "_logger", NullLogger<BlobStorageService>.Instance);
        RemainingApiBranchCoverageTestHelpers.SetField<string?>(blobService, "_customDomain", null);

        var key = Convert.ToBase64String(Enumerable.Repeat((byte)2, 32).ToArray());
        var cred = new StorageSharedKeyCredential("account", key);
        var serviceClient = new BlobServiceClient(new Uri("https://account.blob.core.windows.net"), cred);
        RemainingApiBranchCoverageTestHelpers.SetField(blobService, "_blobServiceClient", serviceClient);

        var worldId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        var blobPath = blobService.BuildBlobPath(worldId, documentId, "Session Notes.pdf");

        Assert.StartsWith($"worlds/{worldId}/documents/{documentId}/", blobPath);

        var uploadUrl = await blobService.GenerateUploadSasUrlAsync(worldId, documentId, "Session Notes.pdf", "application/pdf");
        Assert.StartsWith("https://account.blob.core.windows.net/container/worlds/", uploadUrl);
        Assert.Contains("sig=", uploadUrl);

        var downloadUrl = await blobService.GenerateDownloadSasUrlAsync(blobPath);
        Assert.StartsWith("https://account.blob.core.windows.net/container/worlds/", downloadUrl);
        Assert.Contains("sig=", downloadUrl);
    }

    private sealed class RawAzureConstructorServer : IDisposable
    {
        private readonly TcpListener _listener;
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _loop;

        private RawAzureConstructorServer(TcpListener listener)
        {
            _listener = listener;
            ServiceBaseUri = $"http://127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}/devstoreaccount1/";
            _loop = Task.Run(() => RunAsync(_cts.Token));
        }

        public string ServiceBaseUri { get; }

        public Task Completion => _loop;

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Ownership transferred to RawAzureConstructorServer.")]
        public static RawAzureConstructorServer Start()
        {
            var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
            listener.Start();
            return new RawAzureConstructorServer(listener);
        }

        public void Dispose()
        {
            _cts.Cancel();
            _listener.Stop();
            try
            {
                _loop.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
            }
            catch (SocketException)
            {
            }
            finally
            {
                _cts.Dispose();
            }
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var client = await _listener.AcceptTcpClientAsync(cancellationToken);
                using var stream = client.GetStream();
                using var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, leaveOpen: true);

                while (!string.IsNullOrEmpty(await reader.ReadLineAsync(cancellationToken)))
                {
                }

                var response = new StringBuilder()
                    .Append("HTTP/1.1 201 Created\r\n")
                    .Append($"x-ms-request-id: {Guid.NewGuid():N}\r\n")
                    .Append("x-ms-version: 2023-11-03\r\n")
                    .Append($"Date: {DateTime.UtcNow:R}\r\n")
                    .Append("Content-Length: 0\r\n")
                    .Append("Connection: close\r\n\r\n")
                    .ToString();

                var bytes = Encoding.ASCII.GetBytes(response);
                await stream.WriteAsync(bytes, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }
    }
}
