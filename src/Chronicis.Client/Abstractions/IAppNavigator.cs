using Chronicis.Shared.DTOs;
using Chronicis.Shared.DTOs.Sessions;

namespace Chronicis.Client.Abstractions;

/// <summary>
/// Abstracts navigation so ViewModels can trigger navigation without
/// depending on <see cref="Microsoft.AspNetCore.Components.NavigationManager"/> directly.
/// </summary>
public interface IAppNavigator
{
    /// <summary>Navigates to the specified URL.</summary>
    void NavigateTo(string url, bool replace = false);

    /// <summary>Returns the base URI of the application.</summary>
    string BaseUri { get; }

    /// <summary>Returns the current absolute URI.</summary>
    string Uri { get; }

    // ── Slug-based navigation ────────────────────────────────────────────────

    Task GoToWorldAsync(string worldSlug, bool replace = false);
    Task GoToCampaignAsync(string worldSlug, string campaignSlug, bool replace = false);
    Task GoToCampaignAsync(CampaignDto campaign, bool replace = false);
    Task GoToArcAsync(string worldSlug, string campaignSlug, string arcSlug, bool replace = false);
    Task GoToArcAsync(ArcDto arc, bool replace = false);
    Task GoToSessionAsync(string worldSlug, string campaignSlug, string arcSlug, string sessionSlug, bool replace = false);
    Task GoToSessionAsync(SessionTreeDto session, bool replace = false);
    Task GoToSessionNoteAsync(string worldSlug, string campaignSlug, string arcSlug, string sessionSlug, string noteSlug, bool replace = false);
    Task GoToMapListingAsync(string worldSlug, bool replace = false);
    Task GoToMapAsync(string worldSlug, string mapSlug, bool replace = false);
    Task GoToWikiArticleAsync(string worldSlug, IReadOnlyList<string> articleSlugSegments, bool replace = false);
    Task GoToArticleAsync(ArticleDto article, bool replace = false);
    Task GoToTutorialAsync(string tutorialSlug, bool replace = false);
    Task GoToSearchResultAsync(ArticleSearchResultDto result, bool replace = false);
}
