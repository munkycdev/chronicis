using System.Diagnostics.CodeAnalysis;
using Bunit;
using Chronicis.Client.Components.Articles;
using Chronicis.Client.Components.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using TestContext = Bunit.TestContext;

namespace Chronicis.Client.Tests.Components;

[ExcludeFromCodeCoverage]
public class ArticleDetailWikiLinkAutocompleteTests : TestContext
{
    public ArticleDetailWikiLinkAutocompleteTests()
    {
        Services.AddSingleton<ILogger<WikiLinkAutocomplete>>(NullLogger<WikiLinkAutocomplete>.Instance);
    }

    [Fact]
    public void Autocomplete_ShowsLoadingState()
    {
        var cut = RenderComponent<ArticleDetailWikiLinkAutocomplete>(parameters => parameters
            .Add(p => p.Loading, true));

        Assert.Contains("Searching...", cut.Markup);
    }

    [Fact]
    public void Autocomplete_ShowsMinimumCharactersMessage_ForInternalShortQuery()
    {
        var cut = RenderComponent<ArticleDetailWikiLinkAutocomplete>(parameters => parameters
            .Add(p => p.Query, "ab")
            .Add(p => p.IsExternalQuery, false));

        Assert.Contains("Type at least 3 characters to search", cut.Markup);
    }

    [Fact]
    public void Autocomplete_ShowsCreateOption_ForInternalNoResults()
    {
        var cut = RenderComponent<ArticleDetailWikiLinkAutocomplete>(parameters => parameters
            .Add(p => p.Query, "my/new article")
            .Add(p => p.IsExternalQuery, false));

        Assert.Contains("No articles found", cut.Markup);
        Assert.Contains("Create \"New article\" in Wiki root", cut.Markup);
    }

    [Fact]
    public void Autocomplete_ShowsNoResults_ForExternalNoResults()
    {
        var cut = RenderComponent<ArticleDetailWikiLinkAutocomplete>(parameters => parameters
            .Add(p => p.Query, "acid")
            .Add(p => p.IsExternalQuery, true));

        Assert.Contains("No results found", cut.Markup);
        Assert.DoesNotContain("Create \"", cut.Markup);
    }

    [Fact]
    public async Task Autocomplete_ClickingSuggestion_InvokesOnSelect()
    {
        var suggestion = WikiLinkAutocompleteItem.FromInternal(new Chronicis.Shared.DTOs.LinkSuggestionDto
        {
            ArticleId = Guid.NewGuid(),
            Title = "Acid Arrow",
            DisplayPath = "Spells"
        });
        WikiLinkAutocompleteItem? selected = null;

        var cut = RenderComponent<ArticleDetailWikiLinkAutocomplete>(parameters => parameters
            .Add(p => p.Suggestions, new List<WikiLinkAutocompleteItem> { suggestion })
            .Add(p => p.OnSelect, (WikiLinkAutocompleteItem item) => selected = item));

        await cut.Find(".wiki-link-suggestion").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        Assert.Same(suggestion, selected);
    }

    [Fact]
    public async Task Autocomplete_HoveringSuggestion_InvokesSelectedIndexChanged()
    {
        var suggestion = WikiLinkAutocompleteItem.FromInternal(new Chronicis.Shared.DTOs.LinkSuggestionDto
        {
            ArticleId = Guid.NewGuid(),
            Title = "Acid Arrow",
            DisplayPath = "Spells"
        });
        var hoveredIndex = -1;

        var cut = RenderComponent<ArticleDetailWikiLinkAutocomplete>(parameters => parameters
            .Add(p => p.Suggestions, new List<WikiLinkAutocompleteItem> { suggestion })
            .Add(p => p.SelectedIndex, 1)
            .Add(p => p.SelectedIndexChanged, (int i) => hoveredIndex = i));

        await cut.Find(".wiki-link-suggestion").TriggerEventAsync("onmouseenter", new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        Assert.Equal(0, hoveredIndex);
    }

    [Fact]
    public async Task Autocomplete_CreateOption_InvokesOnCreateWithLastPathSegment()
    {
        var createdName = string.Empty;
        var cut = RenderComponent<ArticleDetailWikiLinkAutocomplete>(parameters => parameters
            .Add(p => p.Query, "world/new article")
            .Add(p => p.IsExternalQuery, false)
            .Add(p => p.OnCreate, (string name) => createdName = name));

        await cut.Find(".wiki-link-create-option").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        Assert.Equal("New article", createdName);
    }
}
