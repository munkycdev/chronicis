namespace Chronicis.Client.Services.Routing;

public interface IAppUrlBuilder
{
    string ForWorld(string worldSlug);
    string ForCampaign(string worldSlug, string campaignSlug);
    string ForArc(string worldSlug, string campaignSlug, string arcSlug);
    string ForSession(string worldSlug, string campaignSlug, string arcSlug, string sessionSlug);
    string ForSessionNote(string worldSlug, string campaignSlug, string arcSlug, string sessionSlug, string noteSlug);
    string ForMapListing(string worldSlug);
    string ForMap(string worldSlug, string mapSlug);
    string ForWikiArticle(string worldSlug, IReadOnlyList<string> articleSlugSegments);
    string ForTutorial(string tutorialSlug);
}
