using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Services;

public class WikiLinkAutocompleteServiceTests
{
    [Fact]
    public async Task ShowAsync_InternalQuery_UsesInternalApiAndRaisesEvents()
    {
        var linkApi = Substitute.For<ILinkApiService>();
        linkApi.GetSuggestionsAsync(Arg.Any<Guid>(), "wizard")
            .Returns(new List<LinkSuggestionDto> { new() { ArticleId = Guid.NewGuid(), Title = "Wizard" } });

        var externalApi = Substitute.For<IExternalLinkApiService>();
        var sut = CreateService(linkApi, externalApi);

        var showCount = 0;
        var updateCount = 0;
        sut.OnShow += () => showCount++;
        sut.OnSuggestionsUpdated += () => updateCount++;

        await sut.ShowAsync("wizard", 1, 2, Guid.NewGuid());

        Assert.True(sut.IsVisible);
        Assert.False(sut.IsExternalQuery);
        Assert.Single(sut.Suggestions);
        Assert.Equal(1, showCount);
        Assert.True(updateCount >= 2);
    }

    [Fact]
    public async Task ShowAsync_ExternalQuery_UsesExternalApi()
    {
        var linkApi = Substitute.For<ILinkApiService>();
        var externalApi = Substitute.For<IExternalLinkApiService>();
        externalApi.GetSuggestionsAsync(Arg.Any<Guid?>(), "srd", "acid", Arg.Any<CancellationToken>())
            .Returns(new List<ExternalLinkSuggestionDto> { new() { Source = "srd", Id = "/api/x", Title = "Acid" } });

        var sut = CreateService(linkApi, externalApi);

        await sut.ShowAsync("srd/acid", 0, 0, null);

        Assert.True(sut.IsExternalQuery);
        Assert.Equal("srd", sut.ExternalSourceKey);
        Assert.Equal("acid", sut.Query);
        Assert.Single(sut.Suggestions);
    }

    [Fact]
    public async Task ShowAsync_ExternalQuery_WithWorldId_UsesProvidedWorldId()
    {
        var worldId = Guid.NewGuid();
        var linkApi = Substitute.For<ILinkApiService>();
        var externalApi = Substitute.For<IExternalLinkApiService>();
        externalApi.GetSuggestionsAsync(worldId, "srd", "acid", Arg.Any<CancellationToken>())
            .Returns(new List<ExternalLinkSuggestionDto>());
        var sut = CreateService(linkApi, externalApi);

        await sut.ShowAsync("srd/acid", 0, 0, worldId);

        await externalApi.Received(1).GetSuggestionsAsync(worldId, "srd", "acid", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShowAsync_ShortInternalQuery_ClearsSuggestionsWithoutLoading()
    {
        var sut = CreateService();

        await sut.ShowAsync("ab", 0, 0, Guid.NewGuid());

        Assert.Empty(sut.Suggestions);
        Assert.False(sut.IsLoading);
    }

    [Fact]
    public async Task ShowAsync_HandlesException()
    {
        var linkApi = Substitute.For<ILinkApiService>();
        linkApi.GetSuggestionsAsync(Arg.Any<Guid>(), Arg.Any<string>())
            .Returns(_ => Task.FromException<List<LinkSuggestionDto>>(new InvalidOperationException("boom")));
        var sut = CreateService(linkApi, Substitute.For<IExternalLinkApiService>());

        await sut.ShowAsync("wizard", 0, 0, Guid.NewGuid());

        Assert.Empty(sut.Suggestions);
        Assert.False(sut.IsLoading);
    }

    [Fact]
    public async Task SelectionMethods_WorkAcrossBoundaries()
    {
        var linkApi = Substitute.For<ILinkApiService>();
        linkApi.GetSuggestionsAsync(Arg.Any<Guid>(), Arg.Any<string>())
            .Returns(new List<LinkSuggestionDto>
            {
                new() { ArticleId = Guid.NewGuid(), Title = "A" },
                new() { ArticleId = Guid.NewGuid(), Title = "B" }
            });

        var sut = CreateService(linkApi, Substitute.For<IExternalLinkApiService>());
        await sut.ShowAsync("query", 0, 0, Guid.NewGuid());

        sut.SelectNext();
        Assert.Equal(1, sut.SelectedIndex);

        sut.SelectNext();
        Assert.Equal(0, sut.SelectedIndex);

        sut.SelectPrevious();
        Assert.Equal(1, sut.SelectedIndex);

        sut.SetSelectedIndex(0);
        Assert.Equal(0, sut.SelectedIndex);

        sut.SetSelectedIndex(99);
        Assert.Equal(0, sut.SelectedIndex);

        Assert.NotNull(sut.GetSelectedSuggestion());

        sut.Hide();
        Assert.False(sut.IsVisible);
        Assert.Empty(sut.Suggestions);
        Assert.Null(sut.GetSelectedSuggestion());
    }

    [Fact]
    public async Task ShowAsync_ParsesKnownPrefixWithoutSlash_AsExternal()
    {
        var worldId = Guid.NewGuid();
        var externalApi = Substitute.For<IExternalLinkApiService>();
        externalApi.GetSuggestionsAsync(Arg.Any<Guid?>(), "srd", "", Arg.Any<CancellationToken>())
            .Returns(new List<ExternalLinkSuggestionDto>());

        var providerApi = Substitute.For<IResourceProviderApiService>();
        providerApi.GetWorldProvidersAsync(worldId).Returns(new List<WorldResourceProviderDto>
        {
            new()
            {
                IsEnabled = true,
                LookupKey = "srd",
                Provider = new ResourceProviderDto { Code = "srd", Name = "SRD", Description = "", DocumentationLink = "", License = "" }
            }
        });
        var sut = CreateService(Substitute.For<ILinkApiService>(), externalApi, providerApi);

        await sut.ShowAsync("srd", 0, 0, worldId);

        Assert.True(sut.IsExternalQuery);
        Assert.Equal("srd", sut.ExternalSourceKey);
        Assert.Equal(string.Empty, sut.Query);
    }

    [Fact]
    public async Task ShowAsync_TreatsUnknownPrefixWithoutSlash_AsInternal()
    {
        var linkApi = Substitute.For<ILinkApiService>();
        linkApi.GetSuggestionsAsync(Arg.Any<Guid>(), "abc").Returns(new List<LinkSuggestionDto>());
        var sut = CreateService(linkApi, Substitute.For<IExternalLinkApiService>());

        await sut.ShowAsync("abc", 0, 0, Guid.NewGuid());

        Assert.False(sut.IsExternalQuery);
        Assert.Null(sut.ExternalSourceKey);
    }

    [Fact]
    public async Task ShowAsync_WhitespaceQuery_IsTreatedAsInternalAndClearsSuggestions()
    {
        var sut = CreateService();

        await sut.ShowAsync("   ", 0, 0, Guid.NewGuid());

        Assert.False(sut.IsExternalQuery);
        Assert.Null(sut.ExternalSourceKey);
        Assert.Empty(sut.Suggestions);
    }

    [Fact]
    public async Task ShowAsync_Open5ePrefixWithoutSlash_IsExternal()
    {
        var worldId = Guid.NewGuid();
        var externalApi = Substitute.For<IExternalLinkApiService>();
        externalApi.GetSuggestionsAsync(Arg.Any<Guid?>(), "open5e", "", Arg.Any<CancellationToken>())
            .Returns(new List<ExternalLinkSuggestionDto>());
        var providerApi = Substitute.For<IResourceProviderApiService>();
        providerApi.GetWorldProvidersAsync(worldId).Returns(new List<WorldResourceProviderDto>
        {
            new()
            {
                IsEnabled = true,
                LookupKey = "open5e",
                Provider = new ResourceProviderDto { Code = "open5e", Name = "Open5e", Description = "", DocumentationLink = "", License = "" }
            }
        });
        var sut = CreateService(Substitute.For<ILinkApiService>(), externalApi, providerApi);

        await sut.ShowAsync("open5e", 0, 0, worldId);

        Assert.True(sut.IsExternalQuery);
        Assert.Equal("open5e", sut.ExternalSourceKey);
    }

    [Fact]
    public void SelectionMethods_NoOp_WhenNoSuggestions()
    {
        var sut = CreateService();

        sut.SelectNext();
        sut.SelectPrevious();
        sut.SetSelectedIndex(0);

        Assert.Equal(0, sut.SelectedIndex);
        Assert.Null(sut.GetSelectedSuggestion());
    }

    [Fact]
    public async Task SetSelectedIndex_WithNegativeIndex_DoesNotChangeSelection()
    {
        var linkApi = Substitute.For<ILinkApiService>();
        linkApi.GetSuggestionsAsync(Arg.Any<Guid>(), Arg.Any<string>())
            .Returns(new List<LinkSuggestionDto>
            {
                new() { ArticleId = Guid.NewGuid(), Title = "A" }
            });
        var sut = CreateService(linkApi, Substitute.For<IExternalLinkApiService>());
        await sut.ShowAsync("query", 0, 0, Guid.NewGuid());

        sut.SetSelectedIndex(-1);

        Assert.Equal(0, sut.SelectedIndex);
    }

    [Fact]
    public async Task ShowAsync_ShortQuery_WithSubscriber_RaisesSuggestionsUpdated()
    {
        var sut = CreateService();
        var updates = 0;
        sut.OnSuggestionsUpdated += () => updates++;

        await sut.ShowAsync("ab", 0, 0, Guid.NewGuid());

        Assert.Equal(1, updates);
    }

    [Fact]
    public async Task ShowAsync_InternalQuery_WithNullWorld_UsesGuidEmpty()
    {
        var linkApi = Substitute.For<ILinkApiService>();
        linkApi.GetSuggestionsAsync(Guid.Empty, "spell").Returns(new List<LinkSuggestionDto>());
        var sut = CreateService(linkApi, Substitute.For<IExternalLinkApiService>());

        await sut.ShowAsync("spell", 0, 0, null);

        await linkApi.Received(1).GetSuggestionsAsync(Guid.Empty, "spell");
    }

    [Fact]
    public async Task SelectionAndHide_RaiseEvents_WhenSubscribersRegistered()
    {
        var linkApi = Substitute.For<ILinkApiService>();
        linkApi.GetSuggestionsAsync(Arg.Any<Guid>(), Arg.Any<string>())
            .Returns(new List<LinkSuggestionDto>
            {
                new() { ArticleId = Guid.NewGuid(), Title = "A" },
                new() { ArticleId = Guid.NewGuid(), Title = "B" }
            });
        var sut = CreateService(linkApi, Substitute.For<IExternalLinkApiService>());
        var hideCount = 0;
        var updateCount = 0;
        sut.OnHide += () => hideCount++;
        sut.OnSuggestionsUpdated += () => updateCount++;

        await sut.ShowAsync("query", 0, 0, Guid.NewGuid());
        sut.SelectNext();
        sut.SelectPrevious();
        sut.SetSelectedIndex(1);
        sut.Hide();

        Assert.Equal(1, hideCount);
        Assert.True(updateCount >= 4);
    }

    [Fact]
    public async Task ShowAsync_ExternalQuery_WithLookupKeyAlias_MapsToProviderCode()
    {
        var worldId = Guid.NewGuid();
        var externalApi = Substitute.For<IExternalLinkApiService>();
        externalApi.GetSuggestionsAsync(worldId, "srd14", "acid", Arg.Any<CancellationToken>())
            .Returns(new List<ExternalLinkSuggestionDto>());

        var providerApi = Substitute.For<IResourceProviderApiService>();
        providerApi.GetWorldProvidersAsync(worldId).Returns(new List<WorldResourceProviderDto>
        {
            new()
            {
                IsEnabled = true,
                LookupKey = "rules",
                Provider = new ResourceProviderDto { Code = "srd14", Name = "SRD 2014", Description = "", DocumentationLink = "", License = "" }
            }
        });

        var sut = CreateService(Substitute.For<ILinkApiService>(), externalApi, providerApi);
        await sut.ShowAsync("rules/acid", 0, 0, worldId);

        await externalApi.Received(1).GetSuggestionsAsync(worldId, "srd14", "acid", Arg.Any<CancellationToken>());
    }

    private static WikiLinkAutocompleteService CreateService(
        ILinkApiService? linkApi = null,
        IExternalLinkApiService? externalApi = null,
        IResourceProviderApiService? providerApi = null)
    {
        linkApi ??= Substitute.For<ILinkApiService>();
        externalApi ??= Substitute.For<IExternalLinkApiService>();
        if (providerApi == null)
        {
            providerApi = Substitute.For<IResourceProviderApiService>();
            providerApi.GetWorldProvidersAsync(Arg.Any<Guid>())
                .Returns(new List<WorldResourceProviderDto>());
        }

        return new WikiLinkAutocompleteService(
            linkApi,
            externalApi,
            providerApi,
            NullLogger<WikiLinkAutocompleteService>.Instance);
    }
}

