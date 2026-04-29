namespace Chronicis.Client.Services.Routing;

public sealed class AppUrlBuilder : IAppUrlBuilder
{
    public string ForWorld(string worldSlug)
    {
        if (string.IsNullOrEmpty(worldSlug))
            throw new ArgumentException("World slug is required.", nameof(worldSlug));
        return $"/{worldSlug}";
    }

    public string ForCampaign(string worldSlug, string campaignSlug)
    {
        if (string.IsNullOrEmpty(worldSlug))
            throw new ArgumentException("World slug is required.", nameof(worldSlug));
        if (string.IsNullOrEmpty(campaignSlug))
            throw new ArgumentException("Campaign slug is required.", nameof(campaignSlug));
        return $"/{worldSlug}/{campaignSlug}";
    }

    public string ForArc(string worldSlug, string campaignSlug, string arcSlug)
    {
        if (string.IsNullOrEmpty(worldSlug))
            throw new ArgumentException("World slug is required.", nameof(worldSlug));
        if (string.IsNullOrEmpty(campaignSlug))
            throw new ArgumentException("Campaign slug is required.", nameof(campaignSlug));
        if (string.IsNullOrEmpty(arcSlug))
            throw new ArgumentException("Arc slug is required.", nameof(arcSlug));
        return $"/{worldSlug}/{campaignSlug}/{arcSlug}";
    }

    public string ForSession(string worldSlug, string campaignSlug, string arcSlug, string sessionSlug)
    {
        if (string.IsNullOrEmpty(worldSlug))
            throw new ArgumentException("World slug is required.", nameof(worldSlug));
        if (string.IsNullOrEmpty(campaignSlug))
            throw new ArgumentException("Campaign slug is required.", nameof(campaignSlug));
        if (string.IsNullOrEmpty(arcSlug))
            throw new ArgumentException("Arc slug is required.", nameof(arcSlug));
        if (string.IsNullOrEmpty(sessionSlug))
            throw new ArgumentException("Session slug is required.", nameof(sessionSlug));
        return $"/{worldSlug}/{campaignSlug}/{arcSlug}/{sessionSlug}";
    }

    public string ForSessionNote(string worldSlug, string campaignSlug, string arcSlug, string sessionSlug, string noteSlug)
    {
        if (string.IsNullOrEmpty(worldSlug))
            throw new ArgumentException("World slug is required.", nameof(worldSlug));
        if (string.IsNullOrEmpty(campaignSlug))
            throw new ArgumentException("Campaign slug is required.", nameof(campaignSlug));
        if (string.IsNullOrEmpty(arcSlug))
            throw new ArgumentException("Arc slug is required.", nameof(arcSlug));
        if (string.IsNullOrEmpty(sessionSlug))
            throw new ArgumentException("Session slug is required.", nameof(sessionSlug));
        if (string.IsNullOrEmpty(noteSlug))
            throw new ArgumentException("Note slug is required.", nameof(noteSlug));
        return $"/{worldSlug}/{campaignSlug}/{arcSlug}/{sessionSlug}/{noteSlug}";
    }

    public string ForMapListing(string worldSlug)
    {
        if (string.IsNullOrEmpty(worldSlug))
            throw new ArgumentException("World slug is required.", nameof(worldSlug));
        return $"/{worldSlug}/maps";
    }

    public string ForMap(string worldSlug, string mapSlug)
    {
        if (string.IsNullOrEmpty(worldSlug))
            throw new ArgumentException("World slug is required.", nameof(worldSlug));
        if (string.IsNullOrEmpty(mapSlug))
            throw new ArgumentException("Map slug is required.", nameof(mapSlug));
        return $"/{worldSlug}/maps/{mapSlug}";
    }

    public string ForWikiArticle(string worldSlug, IReadOnlyList<string> articleSlugSegments)
    {
        if (string.IsNullOrEmpty(worldSlug))
            throw new ArgumentException("World slug is required.", nameof(worldSlug));
        if (articleSlugSegments == null || articleSlugSegments.Count == 0)
            throw new ArgumentException("At least one article slug segment is required.", nameof(articleSlugSegments));
        return $"/{worldSlug}/wiki/{string.Join("/", articleSlugSegments)}";
    }
}
