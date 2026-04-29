using Chronicis.Client.Abstractions;
using Chronicis.Client.Services.Routing;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.DTOs.Sessions;
using Microsoft.AspNetCore.Components;

namespace Chronicis.Client.Infrastructure;

/// <summary>
/// Wraps <see cref="NavigationManager"/> to implement <see cref="IAppNavigator"/>.
/// </summary>
public sealed class AppNavigator : IAppNavigator
{
    private readonly NavigationManager _navigation;
    private readonly IAppUrlBuilder _urlBuilder;

    public AppNavigator(NavigationManager navigation, IAppUrlBuilder urlBuilder)
    {
        _navigation = navigation;
        _urlBuilder = urlBuilder;
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
}
