using Chronicis.Client.Components.Admin;
using Chronicis.Shared.Enums;
using System.Reflection;
using Xunit;

namespace Chronicis.Client.Tests.Components.Admin;

public class TutorialPageTypesTests
{
    [Fact]
    public void All_BuildsCuratedOptionsIncludingArticleTypes()
    {
        var all = TutorialPageTypes.All;

        Assert.NotEmpty(all);
        Assert.Contains(all, o => o.PageType == "Page:Default" && o.DefaultName == "Default Tutorial");
        Assert.Contains(all, o => o.PageType == "Page:AdminUtilities");
        Assert.Contains(all, o => o.PageType == "Page:AdminStatus");
        Assert.Contains(all, o => o.PageType == "ArticleType:Any" && o.DefaultName == "Any Article");
        Assert.DoesNotContain(all, o => o.PageType == $"ArticleType:{ArticleType.Tutorial}");
        Assert.Contains(all, o => o.PageType == $"ArticleType:{ArticleType.WikiArticle}" && o.DefaultName == "Wiki Articles");
        Assert.Contains(all, o => o.PageType == $"ArticleType:{ArticleType.Character}" && o.DefaultName == "Character Articles");
        Assert.Contains(all, o => o.PageType == $"ArticleType:{ArticleType.CharacterNote}" && o.DefaultName == "Character Notes");
        Assert.Contains(all, o => o.PageType == $"ArticleType:{ArticleType.Session}" && o.DefaultName == "Session Articles");
        Assert.Contains(all, o => o.PageType == $"ArticleType:{ArticleType.SessionNote}" && o.DefaultName == "Session Notes");
        Assert.Contains(all, o => o.PageType == $"ArticleType:{ArticleType.Legacy}" && o.DefaultName == "Legacy Articles");
    }

    [Fact]
    public void GetArticleTypeDisplayName_UsesFallbackForUnknownEnumValue()
    {
        var method = typeof(TutorialPageTypes).GetMethod("GetArticleTypeDisplayName", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var result = (string?)method!.Invoke(null, [(ArticleType)123]);

        Assert.Equal("123", result);
    }

    [Fact]
    public void Find_WhenNullOrWhitespace_ReturnsNull()
    {
        Assert.Null(TutorialPageTypes.Find(null));
        Assert.Null(TutorialPageTypes.Find("  "));
    }

    [Fact]
    public void Find_WhenMatch_UsesTrimAndCaseInsensitiveComparison()
    {
        var option = TutorialPageTypes.Find("  page:dashboard ");

        Assert.NotNull(option);
        Assert.Equal("Page:Dashboard", option!.PageType);
    }

    [Fact]
    public void TutorialPageTypeOption_DisplayLabel_FormatsPageTypeAndName()
    {
        var option = new TutorialPageTypeOption("Page:Dashboard", "Dashboard");

        Assert.Equal("Page:Dashboard (Dashboard)", option.DisplayLabel);
    }
}
