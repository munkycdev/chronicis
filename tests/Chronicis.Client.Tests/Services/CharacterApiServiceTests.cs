using System.Net;
using Chronicis.Client.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Chronicis.Client.Tests.Services;

public class CharacterApiServiceTests
{
    [Fact]
    public async Task GetClaimedCharactersAsync_ReturnsEmpty_OnErrorOrNull()
    {
        var nullService = new CharacterApiService(TestHttpMessageHandler.CreateClient(HttpStatusCode.OK, "null"), NullLogger<CharacterApiService>.Instance);
        var exService = new CharacterApiService(new HttpClient(new TestHttpMessageHandler((_, _) => throw new InvalidOperationException("boom"))) { BaseAddress = new Uri("http://localhost/") }, NullLogger<CharacterApiService>.Instance);

        Assert.Empty(await nullService.GetClaimedCharactersAsync());
        Assert.Empty(await exService.GetClaimedCharactersAsync());
    }

    [Fact]
    public async Task ClaimAndUnclaimCharacter_ReturnExpectedStatus()
    {
        var ok = new CharacterApiService(TestHttpMessageHandler.CreateClient(HttpStatusCode.OK), NullLogger<CharacterApiService>.Instance);
        var bad = new CharacterApiService(TestHttpMessageHandler.CreateClient(HttpStatusCode.BadRequest), NullLogger<CharacterApiService>.Instance);
        var ex = new CharacterApiService(new HttpClient(new TestHttpMessageHandler((_, _) => throw new InvalidOperationException("boom"))) { BaseAddress = new Uri("http://localhost/") }, NullLogger<CharacterApiService>.Instance);
        var id = Guid.NewGuid();

        Assert.True(await ok.ClaimCharacterAsync(id));
        Assert.False(await bad.ClaimCharacterAsync(id));
        Assert.False(await ex.ClaimCharacterAsync(id));
        Assert.True(await ok.UnclaimCharacterAsync(id));
        Assert.False(await bad.UnclaimCharacterAsync(id));
        Assert.False(await ex.UnclaimCharacterAsync(id));
    }

    [Fact]
    public async Task GetClaimStatusAsync_ReturnsNull_OnException()
    {
        var ex = new CharacterApiService(new HttpClient(new TestHttpMessageHandler((_, _) => throw new InvalidOperationException("boom"))) { BaseAddress = new Uri("http://localhost/") }, NullLogger<CharacterApiService>.Instance);

        var result = await ex.GetClaimStatusAsync(Guid.NewGuid());

        Assert.Null(result);
    }
}

