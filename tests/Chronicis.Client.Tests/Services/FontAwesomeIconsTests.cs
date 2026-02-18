using Chronicis.Client.Services;
using Xunit;

namespace Chronicis.Client.Tests.Services;

public class FontAwesomeIconsTests
{
    [Fact]
    public void Categories_ArePopulated()
    {
        Assert.NotEmpty(FontAwesomeIcons.Categories);
        Assert.All(FontAwesomeIcons.Categories, c =>
        {
            Assert.False(string.IsNullOrWhiteSpace(c.Name));
            Assert.False(string.IsNullOrWhiteSpace(c.Icon));
            Assert.NotEmpty(c.Icons);
        });
    }

    [Fact]
    public void GetAllIcons_ReturnsDistinctSortedValues()
    {
        var all = FontAwesomeIcons.GetAllIcons();

        Assert.NotEmpty(all);
        Assert.Equal(all.OrderBy(x => x).ToList(), all);
        Assert.Equal(all.Distinct().Count(), all.Count);
    }

    [Fact]
    public void SearchIcons_ReturnsAll_WhenQueryBlank()
    {
        var search = FontAwesomeIcons.SearchIcons("   ");

        Assert.Equal(FontAwesomeIcons.GetAllIcons(), search);
    }

    [Fact]
    public void SearchIcons_FiltersByTerms()
    {
        var search = FontAwesomeIcons.SearchIcons("file pdf");

        Assert.Contains("fa-solid fa-file-pdf", search);
        Assert.DoesNotContain("fa-solid fa-dragon", search);
    }

    [Fact]
    public void IconCategory_Ctor_SetsProperties()
    {
        var sut = new IconCategory("Name", "Icon", new[] { "a", "b" });

        Assert.Equal("Name", sut.Name);
        Assert.Equal("Icon", sut.Icon);
        Assert.Equal(new[] { "a", "b" }, sut.Icons);
    }
}

