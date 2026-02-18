using System.Net;
using Chronicis.Client.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Services;

public class ExportApiServiceTests
{
    [Fact]
    public async Task ExportWorldToMarkdownAsync_ReturnsTrue_OnSuccess()
    {
        var js = Substitute.For<IJSRuntime>();
        var http = TestHttpMessageHandler.CreateClient(HttpStatusCode.OK, "abc");
        var sut = new ExportApiService(http, js, NullLogger<ExportApiService>.Instance);

        var result = await sut.ExportWorldToMarkdownAsync(Guid.NewGuid(), "A/B:World");

        Assert.True(result);
    }

    [Fact]
    public async Task ExportWorldToMarkdownAsync_ReturnsFalse_OnFailureOrException()
    {
        var js = Substitute.For<IJSRuntime>();
        var fail = new ExportApiService(TestHttpMessageHandler.CreateClient(HttpStatusCode.BadRequest), js, NullLogger<ExportApiService>.Instance);
        var ex = new ExportApiService(new HttpClient(new TestHttpMessageHandler((_, _) => throw new InvalidOperationException("boom"))) { BaseAddress = new Uri("http://localhost/") }, js, NullLogger<ExportApiService>.Instance);

        Assert.False(await fail.ExportWorldToMarkdownAsync(Guid.NewGuid(), "World"));
        Assert.False(await ex.ExportWorldToMarkdownAsync(Guid.NewGuid(), "World"));
    }

    [Fact]
    public async Task ExportWorldToMarkdownAsync_TruncatesLongName_WithoutFailing()
    {
        var js = Substitute.For<IJSRuntime>();
        var http = TestHttpMessageHandler.CreateClient(HttpStatusCode.OK, "abc");
        var sut = new ExportApiService(http, js, NullLogger<ExportApiService>.Instance);
        var longName = new string('a', 120);

        var result = await sut.ExportWorldToMarkdownAsync(Guid.NewGuid(), longName);

        Assert.True(result);
    }
}

