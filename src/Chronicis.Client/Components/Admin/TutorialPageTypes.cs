using Chronicis.Shared.Enums;

namespace Chronicis.Client.Components.Admin;

/// <summary>
/// Curated tutorial page-type options for the SysAdmin tutorial mapping UI.
/// Page entries are derived from the current Pages/*.razor routes and aligned to TutorialPageTypeResolver output.
/// </summary>
public static class TutorialPageTypes
{
    private static readonly IReadOnlyList<TutorialPageTypeOption> _all = BuildAll();

    public static IReadOnlyList<TutorialPageTypeOption> All => _all;

    public static TutorialPageTypeOption? Find(string? pageType)
    {
        if (string.IsNullOrWhiteSpace(pageType))
        {
            return null;
        }

        return _all.FirstOrDefault(option =>
            string.Equals(option.PageType, pageType.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<TutorialPageTypeOption> BuildAll()
    {
        var pageOptions = new List<TutorialPageTypeOption>
        {
            new("Page:Default", "Default Tutorial"),
            new("Page:Dashboard", "Dashboard"),
            new("Page:Settings", "Settings"),
            new("Page:WorldDetail", "World Detail"),
            new("Page:CampaignDetail", "Campaign Detail"),
            new("Page:ArcDetail", "Arc Detail"),
            new("Page:SessionDetail", "Session Detail"),
            new("Page:Search", "Search"),
            new("Page:AdminUtilities", "Admin Utilities"),
            new("Page:AdminStatus", "Admin Status"),
            new("Page:Cosmos", "Cosmos"),
            new("Page:About", "About"),
            new("Page:GettingStarted", "Getting Started"),
            new("Page:ChangeLog", "Change Log"),
            new("Page:Privacy", "Privacy"),
            new("Page:TermsOfService", "Terms of Service"),
            new("Page:Licenses", "Licenses")
        };

        var articleOptions = new List<TutorialPageTypeOption>
        {
            new("ArticleType:Any", "Any Article")
        };

        articleOptions.AddRange(
            Enum.GetValues<ArticleType>()
                .Where(articleType => articleType != ArticleType.Tutorial)
                .Select(articleType => new TutorialPageTypeOption(
                    $"ArticleType:{articleType}",
                    GetArticleTypeDisplayName(articleType)))
                .OrderBy(option => option.PageType, StringComparer.Ordinal));

        return pageOptions.Concat(articleOptions).ToList();
    }

    private static string GetArticleTypeDisplayName(ArticleType articleType) => articleType switch
    {
        ArticleType.WikiArticle => "Wiki Articles",
        ArticleType.Character => "Character Articles",
        ArticleType.CharacterNote => "Character Notes",
        ArticleType.Session => "Session Articles",
        ArticleType.SessionNote => "Session Notes",
        ArticleType.Legacy => "Legacy Articles",
        _ => articleType.ToString()
    };
}

public sealed record TutorialPageTypeOption(string PageType, string DefaultName)
{
    public string DisplayLabel => $"{PageType} ({DefaultName})";
}
