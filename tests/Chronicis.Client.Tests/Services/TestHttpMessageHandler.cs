using System.Net;

namespace Chronicis.Client.Tests.Services;

internal sealed class TestHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

    public List<HttpRequestMessage> Requests { get; } = new();

    public TestHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        _handler = handler;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        return _handler(request, cancellationToken);
    }

    public static HttpClient CreateClient(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        var messageHandler = new TestHttpMessageHandler((request, _) => Task.FromResult(handler(request)));
        return new HttpClient(messageHandler)
        {
            BaseAddress = new Uri("http://localhost/")
        };
    }

    public static HttpClient CreateClient(HttpStatusCode statusCode, string content = "{}")
    {
        return CreateClient(_ => new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content)
        });
    }
}

