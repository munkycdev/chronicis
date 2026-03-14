using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.IO.Compression;
using Azure.Core.Pipeline;
using Azure.Storage;
using Azure.Storage.Blobs;
using Chronicis.Api.Services;
using Chronicis.Api.Tests.TestDoubles;
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
    public void Constructor_Succeeds_WithLocalBlobEndpoint()
    {
        using var server = RawAzureTestServer.Start(request => new RawAzureResponse(
            201,
            Headers: BuildAzureHeaders()));

        var accountKey = Convert.ToBase64String(Enumerable.Repeat((byte)3, 32).ToArray());
        var connectionString =
            $"DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey={accountKey};BlobEndpoint={server.ServiceBaseUri}";

        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["BlobStorage:ConnectionString"] = connectionString,
        }).Build();

        var sut = new AzureBlobMapBlobStore(config, NullLogger<AzureBlobMapBlobStore>.Instance);

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

    [Fact]
    public void BuildFeatureGeometryBlobKey_ReturnsCorrectPath()
    {
        var sut = CreateUninitializedStore();
        var mapId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
        var layerId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");
        var featureId = Guid.Parse("cccccccc-0000-0000-0000-000000000003");

        var key = sut.BuildFeatureGeometryBlobKey(mapId, layerId, featureId);

        Assert.Equal($"maps/{mapId}/layers/{layerId}/features/{featureId}.geojson.gz", key);
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

    [Fact]
    public async Task FeatureGeometry_SaveLoadDelete_RoundTripsCompressedJson()
    {
        var blobKey = "maps/map-id/layers/layer-id/features/feature-id.geojson.gz";
        const string geometryJson = """{"type":"Polygon","coordinates":[[[0.1,0.2],[0.8,0.2],[0.1,0.2]]]}""";
        var requests = new List<(string Method, string Path, string Query)>();
        byte[]? storedPayload = null;

        using var handler = new StubHttpMessageHandler(request =>
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            var query = request.RequestUri?.Query ?? string.Empty;
            requests.Add((request.Method.Method, path, query));

            if (request.Method == HttpMethod.Put && path.Contains(blobKey, StringComparison.Ordinal))
            {
                storedPayload = request.Content?.ReadAsByteArrayAsync().GetAwaiter().GetResult();
                var response = new HttpResponseMessage(HttpStatusCode.Created);
                response.Headers.ETag = new EntityTagHeaderValue("\"feature-etag\"");
                return response;
            }

            if (request.Method == HttpMethod.Head && path.Contains(blobKey, StringComparison.Ordinal))
            {
                return new HttpResponseMessage(storedPayload == null ? HttpStatusCode.NotFound : HttpStatusCode.OK);
            }

            if (request.Method == HttpMethod.Get && path.Contains(blobKey, StringComparison.Ordinal))
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(storedPayload ?? []),
                };
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                response.Content.Headers.ContentEncoding.Add("gzip");
                return response;
            }

            if (request.Method == HttpMethod.Delete && path.Contains(blobKey, StringComparison.Ordinal))
            {
                return new HttpResponseMessage(HttpStatusCode.Accepted);
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        using var httpClient = new HttpClient(handler, disposeHandler: false);
        var options = new BlobClientOptions
        {
            Transport = new HttpClientTransport(httpClient),
        };
        var accountKey = Convert.ToBase64String(Enumerable.Repeat((byte)2, 32).ToArray());
        var credential = new StorageSharedKeyCredential("devstoreaccount1", accountKey);
        var blobServiceClient = new BlobServiceClient(new Uri("https://devstoreaccount1.blob.core.windows.net"), credential, options);
        var sut = CreateUninitializedStore(blobServiceClient);

        var etag = await sut.SaveFeatureGeometryAsync(blobKey, geometryJson);
        var loaded = await sut.LoadFeatureGeometryAsync(blobKey);
        await sut.DeleteFeatureGeometryAsync(blobKey);

        Assert.Equal("\"feature-etag\"", etag);
        Assert.Equal(geometryJson, loaded);
        Assert.Equal(geometryJson, ReadGzipPayload(storedPayload));
        Assert.Contains(requests, r => r.Method == "PUT" && r.Path.Contains(blobKey, StringComparison.Ordinal));
        Assert.Contains(requests, r => r.Method == "HEAD" && r.Path.Contains(blobKey, StringComparison.Ordinal));
        Assert.Contains(requests, r => r.Method == "GET" && r.Path.Contains(blobKey, StringComparison.Ordinal));
        Assert.Contains(requests, r => r.Method == "DELETE" && r.Path.Contains(blobKey, StringComparison.Ordinal));
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
        var key = Convert.ToBase64String(Enumerable.Repeat((byte)2, 32).ToArray());
        var cred = new StorageSharedKeyCredential("devstoreaccount1", key);
        var serviceClient = new BlobServiceClient(
            new Uri(serviceBaseUri ?? "https://devstoreaccount1.blob.core.windows.net"), cred);
        return CreateUninitializedStore(serviceClient);
    }

    private static AzureBlobMapBlobStore CreateUninitializedStore(BlobServiceClient serviceClient)
    {
        var sut = (AzureBlobMapBlobStore)RuntimeHelpers.GetUninitializedObject(typeof(AzureBlobMapBlobStore));
        RemainingApiBranchCoverageTestHelpers.SetField(sut, "_logger", NullLogger<AzureBlobMapBlobStore>.Instance);
        RemainingApiBranchCoverageTestHelpers.SetField(sut, "_blobServiceClient", serviceClient);

        return sut;
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Ownership transferred to TestBlobEndpoint.")]
    private static TestBlobEndpoint StartBlobListDeleteEndpoint(string blobName)
    {
        var requests = new List<(string Method, string Path, string Query)>();
        var deleteObserved = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var server = RawAzureTestServer.Start(request =>
        {
            requests.Add((request.Method, request.Path, request.Query));

            if (request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase)
                && request.Query.Contains("comp=list", StringComparison.OrdinalIgnoreCase))
            {
                var xml = $$"""
                                    <?xml version="1.0" encoding="utf-8"?>
                                    <EnumerationResults ServiceEndpoint="{{request.BaseUri}}" ContainerName="chronicis-maps">
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
                return new RawAzureResponse(
                    200,
                    "application/xml",
                    Encoding.UTF8.GetBytes(xml),
                    BuildAzureHeaders());
            }

            if (request.Method.Equals("DELETE", StringComparison.OrdinalIgnoreCase))
            {
                deleteObserved.TrySetResult();
                return new RawAzureResponse(202, Headers: BuildAzureHeaders());
            }

            return new RawAzureResponse(404, Headers: BuildAzureHeaders());
        });

        return new TestBlobEndpoint(
            server.ServiceBaseUri,
            deleteObserved.Task,
            server,
            requests);
    }

    private sealed record TestBlobEndpoint(
        string ServiceBaseUri,
        Task DeleteObserved,
        RawAzureTestServer Server,
        List<(string Method, string Path, string Query)> Requests) : IDisposable
    {
        public void Dispose()
        {
            Server.Dispose();
        }
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Ownership transferred to TestFeatureGeometryEndpoint.")]
    private static TestFeatureGeometryEndpoint StartFeatureGeometryEndpoint(string blobKey)
    {
        var requests = new List<(string Method, string Path, string Query)>();
        var deleteObserved = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        byte[]? storedPayload = null;
        var server = RawAzureTestServer.Start(request =>
        {
            requests.Add((request.Method, request.Path, request.Query));

            if (request.Method.Equals("PUT", StringComparison.OrdinalIgnoreCase))
            {
                storedPayload = request.Body;
                var headers = BuildAzureHeaders();
                headers["ETag"] = "\"feature-etag\"";
                return new RawAzureResponse(201, Headers: headers);
            }

            if (request.Method.Equals("HEAD", StringComparison.OrdinalIgnoreCase))
            {
                return new RawAzureResponse(storedPayload == null ? 404 : 200, Headers: BuildAzureHeaders());
            }

            if (request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase))
            {
                var headers = BuildAzureHeaders();
                headers["Content-Encoding"] = "gzip";
                return new RawAzureResponse(200, "application/json", storedPayload ?? [], headers);
            }

            if (request.Method.Equals("DELETE", StringComparison.OrdinalIgnoreCase))
            {
                deleteObserved.TrySetResult();
                return new RawAzureResponse(202, Headers: BuildAzureHeaders());
            }

            return new RawAzureResponse(404, Headers: BuildAzureHeaders());
        });

        return new TestFeatureGeometryEndpoint(
            server.ServiceBaseUri,
            deleteObserved.Task,
            server,
            requests,
            () => ReadGzipPayload(storedPayload));
    }

    private static string? ReadGzipPayload(byte[]? payload)
    {
        if (payload == null)
        {
            return null;
        }

        using var input = new MemoryStream(payload);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private sealed record TestFeatureGeometryEndpoint(
        string ServiceBaseUri,
        Task DeleteObserved,
        RawAzureTestServer Server,
        List<(string Method, string Path, string Query)> Requests,
        Func<string?> ReadStoredJson) : IDisposable
    {
        public string? StoredJson => ReadStoredJson();

        public void Dispose()
        {
            Server.Dispose();
        }
    }

    private static Dictionary<string, string> BuildAzureHeaders() =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["x-ms-request-id"] = Guid.NewGuid().ToString("N"),
            ["x-ms-version"] = "2023-11-03",
            ["Date"] = DateTime.UtcNow.ToString("R"),
        };

    private sealed record RawAzureRequest(
        string Method,
        string Path,
        string Query,
        byte[] Body,
        string BaseUri);

    private sealed record RawAzureResponse(
        int StatusCode,
        string? ContentType = null,
        byte[]? Body = null,
        Dictionary<string, string>? Headers = null);

    private sealed class RawAzureTestServer : IDisposable
    {
        private readonly TcpListener _listener;
        private readonly CancellationTokenSource _cts = new();
        private readonly Func<RawAzureRequest, RawAzureResponse> _handler;
        private readonly Task _loop;

        private RawAzureTestServer(TcpListener listener, Func<RawAzureRequest, RawAzureResponse> handler)
        {
            _listener = listener;
            _handler = handler;
            ServiceBaseUri = $"http://127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}/devstoreaccount1/";
            _loop = Task.Run(() => RunAsync(_cts.Token));
        }

        public string ServiceBaseUri { get; }

        public Task Completion => _loop;

        [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Ownership transferred to RawAzureTestServer.")]
        public static RawAzureTestServer Start(Func<RawAzureRequest, RawAzureResponse> handler)
        {
            var listener = new TcpListener(System.Net.IPAddress.Loopback, 0);
            listener.Start();
            return new RawAzureTestServer(listener, handler);
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
            while (!cancellationToken.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await _listener.AcceptTcpClientAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }

                using (client)
                {
                    await HandleClientAsync(client, cancellationToken);
                }
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, leaveOpen: true);

            var requestLine = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(requestLine))
            {
                return;
            }

            var requestParts = requestLine.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
            var method = requestParts[0];
            var rawTarget = requestParts[1];
            var uri = new Uri(new Uri(ServiceBaseUri), rawTarget.TrimStart('/'));

            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string? headerLine;
            while (!string.IsNullOrEmpty(headerLine = await reader.ReadLineAsync(cancellationToken)))
            {
                var separatorIndex = headerLine.IndexOf(':');
                if (separatorIndex <= 0)
                {
                    continue;
                }

                headers[headerLine[..separatorIndex]] = headerLine[(separatorIndex + 1)..].Trim();
            }

            var body = Array.Empty<byte>();
            if (headers.TryGetValue("Content-Length", out var contentLengthValue)
                && int.TryParse(contentLengthValue, out var contentLength)
                && contentLength > 0)
            {
                body = new byte[contentLength];
                var offset = 0;
                while (offset < contentLength)
                {
                    var read = await stream.ReadAsync(body.AsMemory(offset, contentLength - offset), cancellationToken);
                    if (read == 0)
                    {
                        break;
                    }

                    offset += read;
                }
            }

            var response = _handler(new RawAzureRequest(
                method,
                uri.AbsolutePath,
                uri.Query,
                body,
                ServiceBaseUri.TrimEnd('/')));

            var responseBody = response.Body ?? [];
            var responseHeaders = response.Headers ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var statusText = response.StatusCode switch
            {
                200 => "OK",
                201 => "Created",
                202 => "Accepted",
                404 => "Not Found",
                _ => "OK",
            };

            var builder = new StringBuilder();
            builder.Append($"HTTP/1.1 {response.StatusCode} {statusText}\r\n");
            if (!string.IsNullOrWhiteSpace(response.ContentType))
            {
                builder.Append($"Content-Type: {response.ContentType}\r\n");
            }

            foreach (var header in responseHeaders)
            {
                builder.Append($"{header.Key}: {header.Value}\r\n");
            }

            builder.Append($"Content-Length: {responseBody.Length}\r\n");
            builder.Append("Connection: close\r\n\r\n");

            var headerBytes = Encoding.ASCII.GetBytes(builder.ToString());
            await stream.WriteAsync(headerBytes, cancellationToken);
            if (!method.Equals("HEAD", StringComparison.OrdinalIgnoreCase) && responseBody.Length > 0)
            {
                await stream.WriteAsync(responseBody, cancellationToken);
            }

            await stream.FlushAsync(cancellationToken);
        }
    }
}
