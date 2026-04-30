using Chronicis.Client.Abstractions;
using Chronicis.Client.Services.Routing;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.DTOs.Sessions;
using Chronicis.Shared.Enums;
using Chronicis.Shared.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace Chronicis.Client.Infrastructure;

/// <summary>
/// Wraps <see cref="NavigationManager"/> to implement <see cref="IAppNavigator"/>.
/// </summary>
public sealed class AppNavigator : IAppNavigator
{
    private readonly NavigationManager _navigation;
    private readonly IAppUrlBuilder _urlBuilder;
    private readonly ILogger<AppNavigator> _logger;

    public AppNavigator(NavigationManager navigation, IAppUrlBuilder urlBuilder, ILogger<AppNavigator> logger)
    {
        _navigation = navigation;
        _urlBuilder = urlBuilder;
        _logger = logger;
    }

    /// <inheritdoc />
    public string BaseUri => _navigation.BaseUri;

    /// <inheritdoc />
    public string Uri => _navigation.Uri;

    /// <inheritdoc />
    public void NavigateTo(string url, bool replace = false) =>
        _navigation.NavigateTo(url, replace);

    public Task GoToWorldAsync(string worldSlug, bool replace = false)
    {
        _navigation.NavigateTo(_urlBuilder.ForWorld(worldSlug), replace);
        return Task.CompletedTask;
    }

    public Task GoToCampaignAsync(string worldSlug, string campaignSlug, bool replace = false)
    {
        _navigation.NavigateTo(_urlBuilder.ForCampaign(worldSlug, campaignSlug), replace);
        return Task.CompletedTask;
    }

    public Task GoToCampaignAsync(CampaignDto campaign, bool replace = false) =>
        GoToCampaignAsync(campaign.WorldSlug, campaign.Slug, replace);

    public Task GoToArcAsync(string worldSlug, string campaignSlug, string arcSlug, bool replace = false)
    {
        _navigation.NavigateTo(_urlBuilder.ForArc(worldSlug, campaignSlug, arcSlug), replace);
        return Task.CompletedTask;
    }

    public Task GoToArcAsync(ArcDto arc, bool replace = false) =>
        GoToArcAsync(arc.WorldSlug, arc.CampaignSlug, arc.Slug, replace);

    public Task GoToSessionAsync(string worldSlug, string campaignSlug, string arcSlug, string sessionSlug, bool replace = false)
    {
        _navigation.NavigateTo(_urlBuilder.ForSession(worldSlug, campaignSlug, arcSlug, sessionSlug), replace);
        return Task.CompletedTask;
    }

    public Task GoToSessionAsync(SessionTreeDto session, bool replace = false) =>
        GoToSessionAsync(session.WorldSlug, session.CampaignSlug, session.ArcSlug, session.Slug, replace);

    public Task GoToSessionNoteAsync(string worldSlug, string campaignSlug, string arcSlug, string sessionSlug, string noteSlug, bool replace = false)
    {
        _navigation.NavigateTo(_urlBuilder.ForSessionNote(worldSlug, campaignSlug, arcSlug, sessionSlug, noteSlug), replace);
        return Task.CompletedTask;
    }

    public Task GoToMapListingAsync(string worldSlug, bool replace = false)
    {
        _navigation.NavigateTo(_urlBuilder.ForMapListing(worldSlug), replace);
        return Task.CompletedTask;
    }

    public Task GoToMapAsync(string worldSlug, string mapSlug, bool replace = false)
    {
        _navigation.NavigateTo(_urlBuilder.ForMap(worldSlug, mapSlug), replace);
        return Task.CompletedTask;
    }

    public Task GoToWikiArticleAsync(string worldSlug, IReadOnlyList<string> articleSlugSegments, bool replace = false)
    {
        _navigation.NavigateTo(_urlBuilder.ForWikiArticle(worldSlug, articleSlugSegments), replace);
        return Task.CompletedTask;
    }

    public Task GoToTutorialAsync(string tutorialSlug, bool replace = false)
    {
        _navigation.NavigateTo(_urlBuilder.ForTutorial(tutorialSlug), replace);
        return Task.CompletedTask;
    }

    public Task GoToArticleAsync(ArticleDto article, bool replace = false)
    {
        var url = article.Type switch
        {
            ArticleType.Tutorial => _urlBuilder.ForTutorial(article.Slug),
            ArticleType.SessionNote => BuildSessionNoteUrl(article),
            ArticleType.Session => LogDeprecatedAndReturnDashboard(article.Id),
            _ => BuildWikiArticleUrl(article)
        };
        _navigation.NavigateTo(url, replace);
        return Task.CompletedTask;
    }

    public Task GoToSearchResultAsync(ArticleSearchResultDto result, bool replace = false)
    {
        string url;
        switch (result.Type)
        {
            case ArticleType.SessionNote:
                if (result.ArticleSlugChain.Count == 0
                    || string.IsNullOrEmpty(result.CampaignSlug)
                    || string.IsNullOrEmpty(result.ArcSlug)
                    || string.IsNullOrEmpty(result.SessionSlug))
                {
                    _logger.LogErrorSanitized("Search result missing session slug context for navigation");
                    url = "/dashboard";
                }
                else
                {
                    url = _urlBuilder.ForSessionNote(
                        result.WorldSlug,
                        result.CampaignSlug,
                        result.ArcSlug,
                        result.SessionSlug,
                        result.ArticleSlugChain[0]);
                }
                break;

            case ArticleType.Tutorial:
                if (result.ArticleSlugChain.Count == 0)
                {
                    _logger.LogErrorSanitized("Search result has empty slug chain for tutorial navigation");
                    url = "/dashboard";
                }
                else
                {
                    url = _urlBuilder.ForTutorial(result.ArticleSlugChain[0]);
                }
                break;

            default:
                if (string.IsNullOrEmpty(result.WorldSlug) || result.ArticleSlugChain.Count == 0)
                {
                    _logger.LogErrorSanitized("Search result missing world slug or slug chain for navigation");
                    url = "/dashboard";
                }
                else
                {
                    url = _urlBuilder.ForWikiArticle(result.WorldSlug, result.ArticleSlugChain);
                }
                break;
        }
        _navigation.NavigateTo(url, replace);
        return Task.CompletedTask;
    }

    private string BuildSessionNoteUrl(ArticleDto article)
    {
        if (article.Breadcrumbs == null || article.Breadcrumbs.Count < 5)
        {
            _logger.LogWarningSanitized("Session note article missing breadcrumbs for slug navigation");
            return "/dashboard";
        }
        return _urlBuilder.ForSessionNote(
            article.Breadcrumbs[0].Slug,
            article.Breadcrumbs[1].Slug,
            article.Breadcrumbs[2].Slug,
            article.Breadcrumbs[3].Slug,
            article.Breadcrumbs[4].Slug);
    }

    private string BuildWikiArticleUrl(ArticleDto article)
    {
        var worldSlug = article.WorldSlug;
        if (string.IsNullOrEmpty(worldSlug) && article.Breadcrumbs != null)
            worldSlug = article.Breadcrumbs.FirstOrDefault(b => b.IsWorld)?.Slug ?? string.Empty;

        var slugChain = article.Breadcrumbs?
            .Where(b => !b.IsWorld && b.Slug != "wiki")
            .Select(b => b.Slug)
            .ToList() ?? new List<string>();

        if (string.IsNullOrEmpty(worldSlug) || slugChain.Count == 0)
        {
            _logger.LogWarningSanitized("Article missing world slug or slug chain for navigation");
            return "/dashboard";
        }
        return _urlBuilder.ForWikiArticle(worldSlug, slugChain);
    }

    private string LogDeprecatedAndReturnDashboard(Guid articleId)
    {
        _logger.LogWarningSanitized("Deprecated Session article type encountered during navigation");
        return "/dashboard";
    }
}
