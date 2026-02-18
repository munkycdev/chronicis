using System.Diagnostics.CodeAnalysis;
using Chronicis.Api.Services.ExternalLinks;
using NSubstitute;
using Xunit;

namespace Chronicis.Api.Tests;

[ExcludeFromCodeCoverage]
public class ExternalLinkProviderRegistryTests
{
    [Fact]
    public void GetProvider_EmptyKey_ReturnsNull()
    {
        var sut = new ExternalLinkProviderRegistry([]);

        Assert.Null(sut.GetProvider(""));
        Assert.Null(sut.GetProvider("   "));
        Assert.Null(sut.GetProvider(null!));
    }

    [Fact]
    public void GetProvider_KnownKey_IsCaseInsensitive()
    {
        var srdProvider = Substitute.For<IExternalLinkProvider>();
        srdProvider.Key.Returns("srd");

        var sut = new ExternalLinkProviderRegistry([srdProvider]);

        Assert.Same(srdProvider, sut.GetProvider("SRD"));
    }

    [Fact]
    public void Constructor_DuplicateKeys_KeepsFirstProvider()
    {
        var first = Substitute.For<IExternalLinkProvider>();
        first.Key.Returns("srd");

        var second = Substitute.For<IExternalLinkProvider>();
        second.Key.Returns("SRD");

        var sut = new ExternalLinkProviderRegistry([first, second]);

        Assert.Same(first, sut.GetProvider("srd"));
        Assert.Single(sut.GetAllProviders());
    }

    [Fact]
    public void GetAllProviders_ReturnsUniqueRegisteredProviders()
    {
        var srdProvider = Substitute.For<IExternalLinkProvider>();
        srdProvider.Key.Returns("srd");

        var rosProvider = Substitute.For<IExternalLinkProvider>();
        rosProvider.Key.Returns("ros");

        var sut = new ExternalLinkProviderRegistry([srdProvider, rosProvider]);

        var providers = sut.GetAllProviders();

        Assert.Equal(2, providers.Count);
        Assert.Contains(srdProvider, providers);
        Assert.Contains(rosProvider, providers);
    }
}
