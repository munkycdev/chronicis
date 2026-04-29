using Chronicis.Client.Services.Routing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Chronicis.Client.Tests.Services.Routing;

public class ClientReservedSlugProviderTests
{
    private static IClientReservedSlugProvider Create(params string[] slugs)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(slugs.Select((s, i) =>
                new KeyValuePair<string, string?>($"Routing:ReservedSlugs:{i}", s)))
            .Build();
        return new ClientReservedSlugProvider(config);
    }

    private static IClientReservedSlugProvider CreateEmpty()
    {
        var config = new ConfigurationBuilder().Build();
        return new ClientReservedSlugProvider(config);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Known reserved slug → true
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void IsReserved_KnownSlug_ReturnsTrue()
    {
        var sut = Create("dashboard", "settings");

        Assert.True(sut.IsReserved("dashboard"));
    }

    // ─────────────────────────────────────────────────────────────────────
    // Unknown slug → false
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void IsReserved_UnknownSlug_ReturnsFalse()
    {
        var sut = Create("dashboard", "settings");

        Assert.False(sut.IsReserved("my-world"));
    }

    // ─────────────────────────────────────────────────────────────────────
    // Case-insensitive matching
    // ─────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("DASHBOARD")]
    [InlineData("Dashboard")]
    [InlineData("DashBoard")]
    public void IsReserved_DifferentCase_ReturnsTrue(string slug)
    {
        var sut = Create("dashboard");

        Assert.True(sut.IsReserved(slug));
    }

    // ─────────────────────────────────────────────────────────────────────
    // Missing Routing config section → empty set (no throws)
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void IsReserved_MissingConfig_ReturnsFalse()
    {
        var sut = CreateEmpty();

        Assert.False(sut.IsReserved("dashboard"));
    }

    // ─────────────────────────────────────────────────────────────────────
    // Multiple slugs configured → all are reserved
    // ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void IsReserved_MultipleConfiguredSlugs_AllRecognised()
    {
        var sut = Create("dashboard", "settings", "cosmos", "w");

        Assert.True(sut.IsReserved("dashboard"));
        Assert.True(sut.IsReserved("settings"));
        Assert.True(sut.IsReserved("cosmos"));
        Assert.True(sut.IsReserved("w"));
        Assert.False(sut.IsReserved("my-world"));
    }
}
