using Chronicis.Api.Infrastructure;
using Chronicis.Api.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace Chronicis.Api.Tests;

public class ReservedSlugProviderTests
{
    [Fact]
    public void IsReserved_KnownSlug_ReturnsTrue()
    {
        var sut = Create(["dashboard", "settings"]);

        Assert.True(sut.IsReserved("dashboard"));
        Assert.True(sut.IsReserved("settings"));
    }

    [Fact]
    public void IsReserved_UnknownSlug_ReturnsFalse()
    {
        var sut = Create(["dashboard"]);

        Assert.False(sut.IsReserved("my-world"));
    }

    [Fact]
    public void IsReserved_IsCaseInsensitive()
    {
        var sut = Create(["dashboard"]);

        Assert.True(sut.IsReserved("DASHBOARD"));
        Assert.True(sut.IsReserved("Dashboard"));
        Assert.True(sut.IsReserved("DaSHBoaRD"));
    }

    [Fact]
    public void All_ReturnsConfiguredSlugs()
    {
        var sut = Create(["dashboard", "settings", "admin"]);

        Assert.Equal(3, sut.All.Count);
        Assert.Contains("dashboard", sut.All);
        Assert.Contains("settings", sut.All);
        Assert.Contains("admin", sut.All);
    }

    [Fact]
    public void EmptyConfig_AllIsEmpty_IsReservedAlwaysFalse()
    {
        var sut = Create([]);

        Assert.Empty(sut.All);
        Assert.False(sut.IsReserved("anything"));
    }

    [Fact]
    public void NullReservedSlugsInOptions_TreatedAsEmpty()
    {
        var options = Options.Create(new RoutingOptions { ReservedSlugs = null! });
        var sut = new ReservedSlugProvider(options);

        Assert.Empty(sut.All);
        Assert.False(sut.IsReserved("dashboard"));
    }

    private static ReservedSlugProvider Create(IEnumerable<string> slugs)
    {
        var options = Options.Create(new RoutingOptions
        {
            ReservedSlugs = new HashSet<string>(slugs, StringComparer.OrdinalIgnoreCase)
        });
        return new ReservedSlugProvider(options);
    }
}
