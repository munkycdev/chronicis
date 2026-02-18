using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Xunit;

namespace Chronicis.Client.Tests.Services;

public class IWikiLinkAutocompleteServiceTests
{
    [Fact]
    public void FromInternal_MapsFields()
    {
        var id = Guid.NewGuid();
        var input = new LinkSuggestionDto
        {
            ArticleId = id,
            Title = "Wizard",
            DisplayPath = "World / Wizard"
        };

        var item = WikiLinkAutocompleteItem.FromInternal(input);

        Assert.Equal("Wizard", item.DisplayText);
        Assert.Equal(id.ToString(), item.ArticleId);
        Assert.Equal("World / Wizard", item.Tooltip);
        Assert.False(item.IsExternal);
        Assert.False(item.IsCategory);
    }

    [Fact]
    public void FromExternal_MapsFields_AndSetsCategoryFlag()
    {
        var input = new ExternalLinkSuggestionDto
        {
            Source = "srd",
            Id = "/api/2014/spells/acid-arrow",
            Title = "Acid Arrow",
            Category = "Spells"
        };

        var item = WikiLinkAutocompleteItem.FromExternal(input);

        Assert.Equal("Acid Arrow", item.DisplayText);
        Assert.Equal("/api/2014/spells/acid-arrow", item.ExternalKey);
        Assert.Equal("srd", item.Tooltip);
        Assert.True(item.IsExternal);
        Assert.True(item.IsCategory);
    }
}

