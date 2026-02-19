using System.Diagnostics.CodeAnalysis;
using Bunit;
using Chronicis.Client.Components.Search;
using Chronicis.Shared.DTOs;
using MudBlazor;
using Xunit;

namespace Chronicis.Client.Tests.Components;

/// <summary>
/// Tests for the SearchResultCard component.
/// This component displays a search result with highlighting and metadata.
/// </summary>
[ExcludeFromCodeCoverage]
public class SearchResultCardTests : MudBlazorTestContext
{
    private ArticleSearchResultDto CreateTestResult(
        string title = "Test Article",
        string matchType = "title",
        string? snippet = null,
        List<BreadcrumbDto>? ancestors = null)
    {
        return new ArticleSearchResultDto
        {
            Id = Guid.NewGuid(),
            Title = title,
            MatchType = matchType,
            MatchSnippet = snippet ?? string.Empty,
            LastModified = DateTime.UtcNow.AddHours(-2),
            AncestorPath = ancestors ?? new List<BreadcrumbDto>()
        };
    }

    [Fact]
    public void SearchResultCard_WithTitleMatch_ShowsTitleMatchChip()
    {
        var result = CreateTestResult(matchType: "title");

        var cut = RenderComponent<SearchResultCard>(parameters => parameters
            .Add(p => p.Result, result)
            .Add(p => p.Query, "test"));

        Assert.Contains("Title Match", cut.Markup);
    }

    [Fact]
    public void SearchResultCard_WithContentMatch_ShowsContentMatchChip()
    {
        var result = CreateTestResult(matchType: "content");

        var cut = RenderComponent<SearchResultCard>(parameters => parameters
            .Add(p => p.Result, result)
            .Add(p => p.Query, "test"));

        Assert.Contains("Content Match", cut.Markup);
    }

    [Fact]
    public void SearchResultCard_WithHashtagMatch_ShowsHashtagMatchChip()
    {
        var result = CreateTestResult(matchType: "hashtag");

        var cut = RenderComponent<SearchResultCard>(parameters => parameters
            .Add(p => p.Result, result)
            .Add(p => p.Query, "test"));

        Assert.Contains("Hashtag Match", cut.Markup);
    }

    [Fact]
    public void SearchResultCard_WithUnknownMatchType_ShowsFallbackChipText()
    {
        var result = CreateTestResult(matchType: "unknown");

        var cut = RenderComponent<SearchResultCard>(parameters => parameters
            .Add(p => p.Result, result)
            .Add(p => p.Query, "test"));

        Assert.Contains("Match", cut.Markup);
    }

    [Theory]
    [InlineData("title", Color.Primary)]
    [InlineData("content", Color.Info)]
    [InlineData("hashtag", Color.Success)]
    [InlineData("unknown", Color.Default)]
    public void SearchResultCard_MatchTypeChip_HasCorrectColor(string matchType, Color expectedColor)
    {
        var result = CreateTestResult(matchType: matchType);

        var cut = RenderComponent<SearchResultCard>(parameters => parameters
            .Add(p => p.Result, result)
            .Add(p => p.Query, "test"));

        var chip = cut.FindComponent<MudChip<string>>();
        Assert.Equal(expectedColor, chip.Instance.Color);
    }

    [Fact]
    public void SearchResultCard_HighlightsQueryInTitle()
    {
        var result = CreateTestResult(title: "Dragon Quest Adventure");

        var cut = RenderComponent<SearchResultCard>(parameters => parameters
            .Add(p => p.Result, result)
            .Add(p => p.Query, "dragon"));

        Assert.Contains("<mark class=\"search-highlight\">Dragon</mark>", cut.Markup);
    }

    [Fact]
    public void SearchResultCard_WithSnippet_RendersSnippet()
    {
        var snippet = "This is a snippet of matched content";
        var result = CreateTestResult(snippet: snippet);

        var cut = RenderComponent<SearchResultCard>(parameters => parameters
            .Add(p => p.Result, result)
            .Add(p => p.Query, "test"));

        Assert.Contains(snippet, cut.Markup);
    }

    [Fact]
    public void SearchResultCard_HighlightsQueryInSnippet()
    {
        var snippet = "This contains the dragon keyword";
        var result = CreateTestResult(snippet: snippet);

        var cut = RenderComponent<SearchResultCard>(parameters => parameters
            .Add(p => p.Result, result)
            .Add(p => p.Query, "dragon"));

        Assert.Contains("<mark class=\"search-highlight\">dragon</mark>", cut.Markup);
    }

    [Fact]
    public void SearchResultCard_WithAncestors_RendersBreadcrumbs()
    {
        var ancestors = new List<BreadcrumbDto>
        {
            new() { Id = Guid.NewGuid(), Title = "World", Slug = "world" },
            new() { Id = Guid.NewGuid(), Title = "Region", Slug = "region" }
        };
        var result = CreateTestResult(ancestors: ancestors);

        var cut = RenderComponent<SearchResultCard>(parameters => parameters
            .Add(p => p.Result, result)
            .Add(p => p.Query, "test"));

        var breadcrumbs = cut.FindComponent<MudBreadcrumbs>();
        Assert.NotNull(breadcrumbs);
        Assert.Contains("World", cut.Markup);
        Assert.Contains("Region", cut.Markup);
    }

    [Fact]
    public void SearchResultCard_WithoutAncestors_DoesNotRenderBreadcrumbs()
    {
        var result = CreateTestResult(ancestors: new List<BreadcrumbDto>());

        var cut = RenderComponent<SearchResultCard>(parameters => parameters
            .Add(p => p.Result, result)
            .Add(p => p.Query, "test"));

        var breadcrumbs = cut.FindComponents<MudBreadcrumbs>();
        Assert.Empty(breadcrumbs);
    }

    [Fact]
    public void SearchResultCard_OnClick_TriggersCallback()
    {
        var clicked = false;
        var result = CreateTestResult();

        var cut = RenderComponent<SearchResultCard>(parameters => parameters
            .Add(p => p.Result, result)
            .Add(p => p.Query, "test")
            .Add(p => p.OnClick, () => clicked = true));

        var card = cut.FindComponent<MudCard>();
        card.Find(".search-result-card").Click();

        Assert.True(clicked);
    }

    [Fact]
    public void SearchResultCard_CardHasCursorPointer()
    {
        var result = CreateTestResult();

        var cut = RenderComponent<SearchResultCard>(parameters => parameters
            .Add(p => p.Result, result)
            .Add(p => p.Query, "test"));

        var card = cut.FindComponent<MudCard>();
        Assert.Contains("cursor: pointer", card.Markup);
    }

    [Fact]
    public void SearchResultCard_HighlightingIsCaseInsensitive()
    {
        var result = CreateTestResult(title: "Dragon Quest");

        var cut = RenderComponent<SearchResultCard>(parameters => parameters
            .Add(p => p.Result, result)
            .Add(p => p.Query, "DRAGON"));

        Assert.Contains("<mark class=\"search-highlight\">Dragon</mark>", cut.Markup);
    }

    [Fact]
    public void SearchResultCard_WithEmptyQuery_DoesNotHighlight()
    {
        var result = CreateTestResult(title: "Dragon Quest");

        var cut = RenderComponent<SearchResultCard>(parameters => parameters
            .Add(p => p.Result, result)
            .Add(p => p.Query, string.Empty));

        Assert.DoesNotContain("<mark", cut.Markup);
        Assert.Contains("Dragon Quest", cut.Markup);
    }

    [Fact]
    public void SearchResultCard_WhenUpdatedJustNow_ShowsJustNow()
    {
        var result = CreateTestResult();
        result.LastModified = DateTime.UtcNow.AddSeconds(-10);

        var cut = RenderComponent<SearchResultCard>(parameters => parameters
            .Add(p => p.Result, result)
            .Add(p => p.Query, "test"));

        Assert.Contains("Just now", cut.Markup);
    }

    [Fact]
    public void SearchResultCard_WhenOlderThanMonth_ShowsDate()
    {
        var result = CreateTestResult();
        result.LastModified = DateTime.UtcNow.AddDays(-45);

        var cut = RenderComponent<SearchResultCard>(parameters => parameters
            .Add(p => p.Result, result)
            .Add(p => p.Query, "test"));

        Assert.Contains(result.LastModified.ToString("MMM d, yyyy"), cut.Markup);
    }
}
