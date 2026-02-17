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
        // Arrange
        var result = CreateTestResult(matchType: "title");

        // Act
        var cut = RenderComponent<SearchResultCard>(parameters => parameters
            .Add(p => p.Result, result)
            .Add(p => p.Query, "test"));

        // Assert
        Assert.Contains("Title Match", cut.Markup);
    }

    [Fact]
    public void SearchResultCard_WithContentMatch_ShowsContentMatchChip()
    {
        // Arrange
        var result = CreateTestResult(matchType: "content");

        // Act
        var cut = RenderComponent<SearchResultCard>(parameters => parameters
            .Add(p => p.Result, result)
            .Add(p => p.Query, "test"));

        // Assert
        Assert.Contains("Content Match", cut.Markup);
    }

    [Fact]
    public void SearchResultCard_WithHashtagMatch_ShowsHashtagMatchChip()
    {
        // Arrange
        var result = CreateTestResult(matchType: "hashtag");

        // Act
        var cut = RenderComponent<SearchResultCard>(parameters => parameters
            .Add(p => p.Result, result)
            .Add(p => p.Query, "test"));

        // Assert
        Assert.Contains("Hashtag Match", cut.Markup);
    }

    [Theory]
    [InlineData("title", Color.Primary)]
    [InlineData("content", Color.Info)]
    [InlineData("hashtag", Color.Success)]
    public void SearchResultCard_MatchTypeChip_HasCorrectColor(string matchType, Color expectedColor)
    {
        // Arrange
        var result = CreateTestResult(matchType: matchType);

        // Act
        var cut = RenderComponent<SearchResultCard>(parameters => parameters
            .Add(p => p.Result, result)
            .Add(p => p.Query, "test"));

        // Assert
        var chip = cut.FindComponent<MudChip<string>>();
        Assert.Equal(expectedColor, chip.Instance.Color);
    }

    [Fact]
    public void SearchResultCard_HighlightsQueryInTitle()
    {
        // Arrange
        var result = CreateTestResult(title: "Dragon Quest Adventure");

        // Act
        var cut = RenderComponent<SearchResultCard>(parameters => parameters
            .Add(p => p.Result, result)
            .Add(p => p.Query, "dragon"));

        // Assert
        Assert.Contains("<mark class=\"search-highlight\">Dragon</mark>", cut.Markup);
    }

    [Fact]
    public void SearchResultCard_WithSnippet_RendersSnippet()
    {
        // Arrange
        var snippet = "This is a snippet of matched content";
        var result = CreateTestResult(snippet: snippet);

        // Act
        var cut = RenderComponent<SearchResultCard>(parameters => parameters
            .Add(p => p.Result, result)
            .Add(p => p.Query, "test"));

        // Assert
        Assert.Contains(snippet, cut.Markup);
    }

    [Fact]
    public void SearchResultCard_HighlightsQueryInSnippet()
    {
        // Arrange
        var snippet = "This contains the dragon keyword";
        var result = CreateTestResult(snippet: snippet);

        // Act
        var cut = RenderComponent<SearchResultCard>(parameters => parameters
            .Add(p => p.Result, result)
            .Add(p => p.Query, "dragon"));

        // Assert
        Assert.Contains("<mark class=\"search-highlight\">dragon</mark>", cut.Markup);
    }

    [Fact]
    public void SearchResultCard_WithAncestors_RendersBreadcrumbs()
    {
        // Arrange
        var ancestors = new List<BreadcrumbDto>
        {
            new() { Id = Guid.NewGuid(), Title = "World", Slug = "world" },
            new() { Id = Guid.NewGuid(), Title = "Region", Slug = "region" }
        };
        var result = CreateTestResult(ancestors: ancestors);

        // Act
        var cut = RenderComponent<SearchResultCard>(parameters => parameters
            .Add(p => p.Result, result)
            .Add(p => p.Query, "test"));

        // Assert
        var breadcrumbs = cut.FindComponent<MudBreadcrumbs>();
        Assert.NotNull(breadcrumbs);
        Assert.Contains("World", cut.Markup);
        Assert.Contains("Region", cut.Markup);
    }

    [Fact]
    public void SearchResultCard_WithoutAncestors_DoesNotRenderBreadcrumbs()
    {
        // Arrange
        var result = CreateTestResult(ancestors: new List<BreadcrumbDto>());

        // Act
        var cut = RenderComponent<SearchResultCard>(parameters => parameters
            .Add(p => p.Result, result)
            .Add(p => p.Query, "test"));

        // Assert
        var breadcrumbs = cut.FindComponents<MudBreadcrumbs>();
        Assert.Empty(breadcrumbs);
    }

    [Fact]
    public void SearchResultCard_OnClick_TriggersCallback()
    {
        // Arrange
        var clicked = false;
        var result = CreateTestResult();

        // Act
        var cut = RenderComponent<SearchResultCard>(parameters => parameters
            .Add(p => p.Result, result)
            .Add(p => p.Query, "test")
            .Add(p => p.OnClick, () => clicked = true));

        var card = cut.FindComponent<MudCard>();
        card.Find(".search-result-card").Click();

        // Assert
        Assert.True(clicked);
    }

    [Fact]
    public void SearchResultCard_CardHasCursorPointer()
    {
        // Arrange
        var result = CreateTestResult();

        // Act
        var cut = RenderComponent<SearchResultCard>(parameters => parameters
            .Add(p => p.Result, result)
            .Add(p => p.Query, "test"));

        // Assert
        var card = cut.FindComponent<MudCard>();
        Assert.Contains("cursor: pointer", card.Markup);
    }

    [Fact]
    public void SearchResultCard_HighlightingIsCaseInsensitive()
    {
        // Arrange
        var result = CreateTestResult(title: "Dragon Quest");

        // Act
        var cut = RenderComponent<SearchResultCard>(parameters => parameters
            .Add(p => p.Result, result)
            .Add(p => p.Query, "DRAGON"));

        // Assert
        Assert.Contains("<mark class=\"search-highlight\">Dragon</mark>", cut.Markup);
    }

    [Fact]
    public void SearchResultCard_WithEmptyQuery_DoesNotHighlight()
    {
        // Arrange
        var result = CreateTestResult(title: "Dragon Quest");

        // Act
        var cut = RenderComponent<SearchResultCard>(parameters => parameters
            .Add(p => p.Result, result)
            .Add(p => p.Query, string.Empty));

        // Assert
        Assert.DoesNotContain("<mark", cut.Markup);
        Assert.Contains("Dragon Quest", cut.Markup);
    }
}
