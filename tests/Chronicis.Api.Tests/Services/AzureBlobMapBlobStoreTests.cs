using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using Azure.Storage;
using Azure.Storage.Blobs;
using Chronicis.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chronicis.Api.Tests.Services;

[ExcludeFromCodeCoverage]
public class AzureBlobMapBlobStoreTests
{
    // ── Constructor ───────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_Throws_WhenConnectionStringMissing()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        Assert.Throws<InvalidOperationException>(() =>
            new AzureBlobMapBlobStore(config, NullLogger<AzureBlobMapBlobStore>.Instance));
    }

    [Fact]
    public void Constructor_Throws_WhenConnectionStringInvalid()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["BlobStorage:ConnectionString"] = "not-a-valid-connection-string",
        }).Build();
        Assert.ThrowsAny<Exception>(() =>
            new AzureBlobMapBlobStore(config, NullLogger<AzureBlobMapBlobStore>.Instance));
    }

    [Fact]
    public void Constructor_Throws_WhenAlternateKeyInvalid()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["BlobStorage__ConnectionString"] = "not-a-valid-connection-string",
        }).Build();
        Assert.ThrowsAny<Exception>(() =>
            new AzureBlobMapBlobStore(config, NullLogger<AzureBlobMapBlobStore>.Instance));
    }

    [Fact]
    public async Task Constructor_Succeeds_WithLocalBlobEndpoint()
    {
        using var tcp = new TcpListener(IPAddress.Loopback, 0);
        tcp.Start();
        var port = ((IPEndPoint)tcp.LocalEndpoint).Port;
        tcp.Stop();

        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        listener.Start();

        var serverTask = Task.Run(async () =>
        {
            var context = await listener.GetContextAsync();
            context.Response.StatusCode = 201;
            context.Response.Headers["x-ms-request-id"] = Guid.NewGuid().ToString("N");
            context.Response.Headers["x-ms-version"] = "2023-11-03";
            context.Response.Headers["Date"] = DateTime.UtcNow.ToString("R");
            context.Response.Close();
        });

        var accountKey = Convert.ToBase64String(Enumerable.Repeat((byte)3, 32).ToArray());
        var connectionString =
            $"DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey={accountKey};BlobEndpoint=http://127.0.0.1:{port}/devstoreaccount1;";

        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["BlobStorage:ConnectionString"] = connectionString,
        }).Build();

        var sut = new AzureBlobMapBlobStore(config, NullLogger<AzureBlobMapBlobStore>.Instance);
        await serverTask;

        Assert.NotNull(sut);
    }

    // ── BuildBasemapBlobKey ───────────────────────────────────────────────────

    [Fact]
    public void BuildBasemapBlobKey_ReturnsCorrectPath()
    {
        var sut = CreateUninitializedStore();
        var mapId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");

        var key = sut.BuildBasemapBlobKey(mapId, "world-map.png");

        Assert.Equal($"maps/{mapId}/basemap/world-map.png", key);
    }

    // ── GenerateUploadSasUrlAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GenerateUploadSasUrlAsync_ReturnsUrlContainingSig()
    {
        var sut = CreateUninitializedStore();
        var mapId = Guid.NewGuid();

        var url = await sut.GenerateUploadSasUrlAsync(mapId, "basemap.png", "image/png");

        Assert.Contains("sig=", url);
    }

    // ── GenerateReadSasUrlAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GenerateReadSasUrlAsync_ReturnsUrlContainingSig()
    {
        var sut = CreateUninitializedStore();
        var blobKey = "maps/some-id/basemap/img.png";

        var url = await sut.GenerateReadSasUrlAsync(blobKey);

        Assert.Contains("sig=", url);
    }

    [Fact]
    public async Task DeleteMapFolderAsync_DeletesAllBlobsUnderMapPrefix()
    {
        var mapId = Guid.NewGuid();
        var blobName = $"maps/{mapId}/basemap/test.png";

        using var endpoint = StartBlobListDeleteEndpoint(blobName);
        var sut = CreateUninitializedStore(endpoint.ServiceBaseUri);

        await sut.DeleteMapFolderAsync(mapId);
        await endpoint.DeleteObserved;

        Assert.Contains(endpoint.Requests, r => r.Method == "GET" && r.Query.Contains("comp=list", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(endpoint.Requests, r => r.Method == "DELETE" && r.Path.Contains(blobName, StringComparison.Ordinal));
    }

    // ── SanitizeFileName (private static via reflection) ─────────────────────

    [Fact]
    public void SanitizeFileName_RemovesInvalidChars()
    {
        var sanitize = RemainingApiBranchCoverageTestHelpers.GetMethod(
            typeof(AzureBlobMapBlobStore), "SanitizeFileName");

        var result = (string)sanitize.Invoke(null, ["folder/sub\\file?.txt"])!;

        foreach (var invalid in Path.GetInvalidFileNameChars())
        {
            Assert.DoesNotContain(invalid, result);
        }
    }

    [Fact]
    public void SanitizeFileName_TruncatesLongFilename()
    {
        var sanitize = RemainingApiBranchCoverageTestHelpers.GetMethod(
            typeof(AzureBlobMapBlobStore), "SanitizeFileName");

        var longName = new string('x', 210) + ".png";
        var result = (string)sanitize.Invoke(null, [longName])!;

        Assert.True(result.Length <= 200);
        Assert.EndsWith(".png", result);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static AzureBlobMapBlobStore CreateUninitializedStore(string? serviceBaseUri = null)
    {
        var sut = (AzureBlobMapBlobStore)RuntimeHelpers.GetUninitializedObject(typeof(AzureBlobMapBlobStore));
        RemainingApiBranchCoverageTestHelpers.SetField(sut, "_logger", NullLogger<AzureBlobMapBlobStore>.Instance);

        var key = Convert.ToBase64String(Enumerable.Repeat((byte)2, 32).ToArray());
        var cred = new StorageSharedKeyCredential("devstoreaccount1", key);
        var serviceClient = new BlobServiceClient(
            new Uri(serviceBaseUri ?? "https://devstoreaccount1.blob.core.windows.net"), cred);
        RemainingApiBranchCoverageTestHelpers.SetField(sut, "_blobServiceClient", serviceClient);

        return sut;
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Ownership transferred to TestBlobEndpoint.")]
    private static TestBlobEndpoint StartBlobListDeleteEndpoint(string blobName)
    {
        using var tcp = new TcpListener(IPAddress.Loopback, 0);
        tcp.Start();
        var port = ((IPEndPoint)tcp.LocalEndpoint).Port;
        tcp.Stop();

        var listener = new HttpListener();
        listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        listener.Start();

        var requests = new List<(string Method, string Path, string Query)>();
        var deleteObserved = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        _ = Task.Run(async () =>
        {
            try
            {
                while (listener.IsListening)
                {
                    var context = await listener.GetContextAsync();
                    var req = context.Request;
                    requests.Add((req.HttpMethod, req.Url?.AbsolutePath ?? string.Empty, req.Url?.Query ?? string.Empty));

                    if (req.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase)
                        && (req.Url?.Query ?? string.Empty).Contains("comp=list", StringComparison.OrdinalIgnoreCase))
                    {
                        var xml = $$"""
                                    <?xml version="1.0" encoding="utf-8"?>
                                    <EnumerationResults ServiceEndpoint="http://127.0.0.1:{{port}}/devstoreaccount1" ContainerName="chronicis-maps">
                                      <Blobs>
                                        <Blob>
                                          <Name>{{blobName}}</Name>
                                          <Properties>
                                            <BlobType>BlockBlob</BlobType>
                                            <Content-Length>1</Content-Length>
                                          </Properties>
                                        </Blob>
                                      </Blobs>
                                      <NextMarker />
                                    </EnumerationResults>
                                    """;

                        var bytes = Encoding.UTF8.GetBytes(xml);
                        context.Response.StatusCode = 200;
                        context.Response.ContentType = "application/xml";
                        context.Response.ContentLength64 = bytes.Length;
                        context.Response.Headers["x-ms-request-id"] = Guid.NewGuid().ToString("N");
                        context.Response.Headers["x-ms-version"] = "2023-11-03";
                        context.Response.Headers["Date"] = DateTime.UtcNow.ToString("R");
                        await context.Response.OutputStream.WriteAsync(bytes);
                        context.Response.Close();
                        continue;
                    }

                    if (req.HttpMethod.Equals("DELETE", StringComparison.OrdinalIgnoreCase))
                    {
                        context.Response.StatusCode = 202;
                        context.Response.Headers["x-ms-request-id"] = Guid.NewGuid().ToString("N");
                        context.Response.Headers["x-ms-version"] = "2023-11-03";
                        context.Response.Headers["Date"] = DateTime.UtcNow.ToString("R");
                        context.Response.Close();
                        deleteObserved.TrySetResult();
                        break;
                    }

                    context.Response.StatusCode = 404;
                    context.Response.Close();
                }
            }
            catch (Exception ex)
            {
                deleteObserved.TrySetException(ex);
            }
            finally
            {
                listener.Stop();
            }
        });

        return new TestBlobEndpoint(
            $"http://127.0.0.1:{port}/devstoreaccount1/",
            deleteObserved.Task,
            listener,
            requests);
    }

    private sealed record TestBlobEndpoint(
        string ServiceBaseUri,
        Task DeleteObserved,
        HttpListener Listener,
        List<(string Method, string Path, string Query)> Requests) : IDisposable
    {
        public void Dispose()
        {
            Listener.Stop();
        }
    }
}
