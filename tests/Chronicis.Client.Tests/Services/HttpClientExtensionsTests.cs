using System.Net;
using Chronicis.Client.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chronicis.Client.Tests.Services;

public class HttpClientExtensionsTests
{
    private sealed class TestDto
    {
        public string? Name { get; set; }
    }

    [Fact]
    public async Task GetEntityAsync_ReturnsEntity_OnSuccess()
    {
        var http = TestHttpMessageHandler.CreateClient(HttpStatusCode.OK, "{\"name\":\"ok\"}");

        var result = await http.GetEntityAsync<TestDto>("entity", NullLogger.Instance, "entity");

        Assert.NotNull(result);
        Assert.Equal("ok", result.Name);
    }

    [Fact]
    public async Task GetEntityAsync_ReturnsNull_On404()
    {
        var handler = new TestHttpMessageHandler((_, _) =>
            throw new HttpRequestException("not found", null, HttpStatusCode.NotFound));
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };

        var result = await http.GetEntityAsync<TestDto>("entity", NullLogger.Instance, "entity");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetEntityAsync_ReturnsNull_OnException()
    {
        var handler = new TestHttpMessageHandler((_, _) => throw new InvalidOperationException("boom"));
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };

        var result = await http.GetEntityAsync<TestDto>("entity", NullLogger.Instance, "entity");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetListAsync_ReturnsListOrEmpty()
    {
        var ok = TestHttpMessageHandler.CreateClient(HttpStatusCode.OK, "[{\"name\":\"a\"}]");
        var empty = TestHttpMessageHandler.CreateClient(HttpStatusCode.OK, "null");

        var result1 = await ok.GetListAsync<TestDto>("list", NullLogger.Instance, "list");
        var result2 = await empty.GetListAsync<TestDto>("list", NullLogger.Instance, "list");

        Assert.Single(result1);
        Assert.Empty(result2);
    }

    [Fact]
    public async Task GetListAsync_ReturnsEmpty_OnException()
    {
        var handler = new TestHttpMessageHandler((_, _) => throw new InvalidOperationException("boom"));
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };

        var result = await http.GetListAsync<TestDto>("list", NullLogger.Instance, "list");

        Assert.Empty(result);
    }

    [Fact]
    public async Task PostEntityAsync_ReturnsEntityOrNull_AndHandlesException()
    {
        var success = TestHttpMessageHandler.CreateClient(HttpStatusCode.OK, "{\"name\":\"created\"}");
        var fail = TestHttpMessageHandler.CreateClient(HttpStatusCode.BadRequest, "{}");
        var boomHandler = new TestHttpMessageHandler((_, _) => throw new InvalidOperationException("boom"));
        var boom = new HttpClient(boomHandler) { BaseAddress = new Uri("http://localhost/") };

        var ok = await success.PostEntityAsync<TestDto>("post", new { }, NullLogger.Instance, "entity");
        var bad = await fail.PostEntityAsync<TestDto>("post", new { }, NullLogger.Instance, "entity");
        var ex = await boom.PostEntityAsync<TestDto>("post", new { }, NullLogger.Instance, "entity");

        Assert.NotNull(ok);
        Assert.Equal("created", ok.Name);
        Assert.Null(bad);
        Assert.Null(ex);
    }

    [Fact]
    public async Task PutEntityAsync_ReturnsEntityOrNull_AndHandlesException()
    {
        var success = TestHttpMessageHandler.CreateClient(HttpStatusCode.OK, "{\"name\":\"updated\"}");
        var fail = TestHttpMessageHandler.CreateClient(HttpStatusCode.BadRequest, "{}");
        var boomHandler = new TestHttpMessageHandler((_, _) => throw new InvalidOperationException("boom"));
        var boom = new HttpClient(boomHandler) { BaseAddress = new Uri("http://localhost/") };

        var ok = await success.PutEntityAsync<TestDto>("put", new { }, NullLogger.Instance, "entity");
        var bad = await fail.PutEntityAsync<TestDto>("put", new { }, NullLogger.Instance, "entity");
        var ex = await boom.PutEntityAsync<TestDto>("put", new { }, NullLogger.Instance, "entity");

        Assert.NotNull(ok);
        Assert.Equal("updated", ok.Name);
        Assert.Null(bad);
        Assert.Null(ex);
    }

    [Fact]
    public async Task DeleteEntityAsync_ReturnsExpectedValues()
    {
        var success = TestHttpMessageHandler.CreateClient(HttpStatusCode.NoContent);
        var fail = TestHttpMessageHandler.CreateClient(HttpStatusCode.BadRequest);
        var boomHandler = new TestHttpMessageHandler((_, _) => throw new InvalidOperationException("boom"));
        var boom = new HttpClient(boomHandler) { BaseAddress = new Uri("http://localhost/") };

        Assert.True(await success.DeleteEntityAsync("delete", NullLogger.Instance, "entity"));
        Assert.False(await fail.DeleteEntityAsync("delete", NullLogger.Instance, "entity"));
        Assert.False(await boom.DeleteEntityAsync("delete", NullLogger.Instance, "entity"));
    }

    [Fact]
    public async Task PatchEntityAsync_ReturnsExpectedValues()
    {
        var success = TestHttpMessageHandler.CreateClient(HttpStatusCode.OK);
        var fail = TestHttpMessageHandler.CreateClient(HttpStatusCode.BadRequest, "failed");
        var boomHandler = new TestHttpMessageHandler((_, _) => throw new InvalidOperationException("boom"));
        var boom = new HttpClient(boomHandler) { BaseAddress = new Uri("http://localhost/") };

        Assert.True(await success.PatchEntityAsync("patch", new { }, NullLogger.Instance, "entity"));
        Assert.False(await fail.PatchEntityAsync("patch", new { }, NullLogger.Instance, "entity"));
        Assert.False(await boom.PatchEntityAsync("patch", new { }, NullLogger.Instance, "entity"));
    }

    [Fact]
    public async Task PutBoolAsync_ReturnsExpectedValues()
    {
        var success = TestHttpMessageHandler.CreateClient(HttpStatusCode.OK);
        var fail = TestHttpMessageHandler.CreateClient(HttpStatusCode.BadRequest, "failed");
        var boomHandler = new TestHttpMessageHandler((_, _) => throw new InvalidOperationException("boom"));
        var boom = new HttpClient(boomHandler) { BaseAddress = new Uri("http://localhost/") };

        Assert.True(await success.PutBoolAsync("put", new { }, NullLogger.Instance, "entity"));
        Assert.False(await fail.PutBoolAsync("put", new { }, NullLogger.Instance, "entity"));
        Assert.False(await boom.PutBoolAsync("put", new { }, NullLogger.Instance, "entity"));
    }

    [Fact]
    public async Task Methods_HandleNullEntityDescription()
    {
        var ok = TestHttpMessageHandler.CreateClient(HttpStatusCode.OK, "{\"name\":\"x\"}");
        var list = TestHttpMessageHandler.CreateClient(HttpStatusCode.OK, "[]");
        var fail = TestHttpMessageHandler.CreateClient(HttpStatusCode.BadRequest, "{}");

        Assert.NotNull(await ok.GetEntityAsync<TestDto>("e", NullLogger.Instance, null));
        Assert.Empty(await list.GetListAsync<TestDto>("l", NullLogger.Instance, null));
        Assert.Null(await fail.PostEntityAsync<TestDto>("p", new { }, NullLogger.Instance, null));
        Assert.Null(await fail.PutEntityAsync<TestDto>("u", new { }, NullLogger.Instance, null));
        Assert.False(await fail.DeleteEntityAsync("d", NullLogger.Instance, null));
        Assert.False(await fail.PatchEntityAsync("x", new { }, NullLogger.Instance, null));
        Assert.False(await fail.PutBoolAsync("b", new { }, NullLogger.Instance, null));
    }

    [Fact]
    public async Task GetEntityAndGetList_HandleFailures_WhenEntityDescriptionIsNull()
    {
        var notFound = new HttpClient(new TestHttpMessageHandler((_, _) =>
            throw new HttpRequestException("not found", null, HttpStatusCode.NotFound)))
        { BaseAddress = new Uri("http://localhost/") };
        var boom = new HttpClient(new TestHttpMessageHandler((_, _) => throw new InvalidOperationException("boom")))
        { BaseAddress = new Uri("http://localhost/") };

        Assert.Null(await notFound.GetEntityAsync<TestDto>("e", NullLogger.Instance, null));
        Assert.Null(await boom.GetEntityAsync<TestDto>("e", NullLogger.Instance, null));
        Assert.Empty(await boom.GetListAsync<TestDto>("l", NullLogger.Instance, null));
    }

    [Fact]
    public async Task MutationMethods_HandleExceptions_WhenEntityDescriptionIsNull()
    {
        var boomPost = new HttpClient(new TestHttpMessageHandler((_, _) => throw new InvalidOperationException("boom")))
        { BaseAddress = new Uri("http://localhost/") };
        var boomPut = new HttpClient(new TestHttpMessageHandler((_, _) => throw new InvalidOperationException("boom")))
        { BaseAddress = new Uri("http://localhost/") };
        var boomDelete = new HttpClient(new TestHttpMessageHandler((_, _) => throw new InvalidOperationException("boom")))
        { BaseAddress = new Uri("http://localhost/") };
        var boomPatch = new HttpClient(new TestHttpMessageHandler((_, _) => throw new InvalidOperationException("boom")))
        { BaseAddress = new Uri("http://localhost/") };
        var boomPutBool = new HttpClient(new TestHttpMessageHandler((_, _) => throw new InvalidOperationException("boom")))
        { BaseAddress = new Uri("http://localhost/") };

        Assert.Null(await boomPost.PostEntityAsync<TestDto>("p", new { }, NullLogger.Instance, null));
        Assert.Null(await boomPut.PutEntityAsync<TestDto>("u", new { }, NullLogger.Instance, null));
        Assert.False(await boomDelete.DeleteEntityAsync("d", NullLogger.Instance, null));
        Assert.False(await boomPatch.PatchEntityAsync("x", new { }, NullLogger.Instance, null));
        Assert.False(await boomPutBool.PutBoolAsync("b", new { }, NullLogger.Instance, null));
    }
}

