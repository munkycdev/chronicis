using System.Diagnostics.CodeAnalysis;
using Chronicis.Client.Components.Articles;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Xunit;

namespace Chronicis.Client.Tests.Components;

[ExcludeFromCodeCoverage]
public class WikiLinkAutocompleteItemTests
{
    [Fact]
    public void FromInternal_MapsFields()
    {
        var articleId = Guid.NewGuid();
        var dto = new LinkSuggestionDto
        {
            ArticleId = articleId,
            Title = "Fireball",
            DisplayPath = "Magic/Spells",
            MatchedAlias = "Ball of Fire",
            ArticleType = ArticleType.WikiArticle
        };

        var item = WikiLinkAutocompleteItem.FromInternal(dto);

        Assert.False(item.IsExternal);
        Assert.False(item.IsCategory);
        Assert.Equal(articleId, item.ArticleId);
        Assert.Equal("Magic/Spells", item.SecondaryText);
        Assert.Equal("Ball of Fire (Fireball)", item.DisplayTitle);
    }

    [Fact]
    public void FromExternal_MapsNonCategoryFields()
    {
        var dto = new ExternalLinkSuggestionDto
        {
            Source = "srd",
            Id = "/api/spells/acid-arrow",
            Title = "Acid Arrow",
            Subtitle = "2nd-level evocation",
            Icon = "spell"
        };

        var item = WikiLinkAutocompleteItem.FromExternal(dto);

        Assert.True(item.IsExternal);
        Assert.False(item.IsCategory);
        Assert.Equal("/api/spells/acid-arrow", item.ExternalId);
        Assert.Equal("SRD", item.SourceBadge);
        Assert.Equal("Acid Arrow", item.DisplayTitle);
    }

    [Fact]
    public void FromExternal_MapsCategoryFields()
    {
        var dto = new ExternalLinkSuggestionDto
        {
            Source = "srd",
            Category = "_category",
            Id = "_category/spells",
            Title = "Spells",
            Icon = "book"
        };

        var item = WikiLinkAutocompleteItem.FromExternal(dto);

        Assert.True(item.IsExternal);
        Assert.True(item.IsCategory);
        Assert.Null(item.ExternalId);
        Assert.Equal("spells", item.CategoryKey);
        Assert.Equal("book Spells", item.Title);
    }

    [Fact]
    public void SourceBadge_EmptyWhenSourceMissing()
    {
        var dto = new ExternalLinkSuggestionDto
        {
            Source = "",
            Title = "Item"
        };

        var item = WikiLinkAutocompleteItem.FromExternal(dto);

        Assert.Equal(string.Empty, item.SourceBadge);
    }
}
