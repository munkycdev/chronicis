using System.Net;
using Chronicis.Client.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chronicis.Client.Tests.Services;

public class QuoteServiceTests
{
    [Fact]
    public async Task GetRandomQuoteAsync_UsesApiQuote_WhenPresent()
    {
        var json = "[{\"q\":\"Test quote\",\"a\":\"Author\",\"h\":\"\"}]";
        var sut = new QuoteService(TestHttpMessageHandler.CreateClient(HttpStatusCode.OK, json), NullLogger<QuoteService>.Instance);

        var result = await sut.GetRandomQuoteAsync();

        Assert.NotNull(result);
        Assert.Equal("Test quote", result.Content);
        Assert.Equal("Author", result.Author);
    }

    [Fact]
    public async Task GetRandomQuoteAsync_FallsBack_OnEmptyOrException()
    {
        var empty = new QuoteService(TestHttpMessageHandler.CreateClient(HttpStatusCode.OK, "[]"), NullLogger<QuoteService>.Instance);
        var ex = new QuoteService(new HttpClient(new TestHttpMessageHandler((_, _) => throw new InvalidOperationException("boom"))) { BaseAddress = new Uri("http://localhost/") }, NullLogger<QuoteService>.Instance);

        var a = await empty.GetRandomQuoteAsync();
        var b = await ex.GetRandomQuoteAsync();

        Assert.Contains("The world is indeed full of peril", a!.Content);
        Assert.Equal("J.R.R. Tolkien", a.Author);
        Assert.Contains("The world is indeed full of peril", b!.Content);
    }

    [Fact]
    public async Task GetRandomQuoteAsync_FallsBack_OnNullPayload()
    {
        var sut = new QuoteService(TestHttpMessageHandler.CreateClient(HttpStatusCode.OK, "null"), NullLogger<QuoteService>.Instance);

        var result = await sut.GetRandomQuoteAsync();

        Assert.Equal("J.R.R. Tolkien", result!.Author);
    }

    [Fact]
    public void ZenQuote_And_Quote_Defaults()
    {
        var zen = new ZenQuote();
        var q = new Quote();

        Assert.Equal(string.Empty, zen.Quote);
        Assert.Equal(string.Empty, zen.Author);
        Assert.Equal(string.Empty, zen.Html);
        Assert.Equal(string.Empty, q.Content);
        Assert.Equal(string.Empty, q.Author);
    }
}

