namespace Chronicis.Client.Services;

/// <inheritdoc cref="IWikiLinkCommitService"/>
public class WikiLinkCommitService : IWikiLinkCommitService
{
    private readonly IWikiLinkService _wikiLinkService;

    public WikiLinkCommitService(IWikiLinkService wikiLinkService)
    {
        _wikiLinkService = wikiLinkService;
    }

    /// <inheritdoc/>
    public AutocompleteCommitDecision Decide(
        string query,
        int suggestionsCount,
        int selectedIndex,
        bool isExternalQuery,
        bool isMapQuery)
        => AutocompleteCommitDecision.Decide(query, suggestionsCount, selectedIndex, isExternalQuery, isMapQuery);

    /// <inheritdoc/>
    public async Task<WikiLinkCreateResult> CreateAndLinkAsync(string name, Guid worldId)
    {
        try
        {
            var article = await _wikiLinkService.CreateArticleFromAutocompleteAsync(name, worldId);
            if (article == null)
                return new WikiLinkCreateResult(false, null, "Failed to create article");

            return new WikiLinkCreateResult(true, article, null);
        }
        catch (Exception ex)
        {
            return new WikiLinkCreateResult(false, null, ex.Message);
        }
    }
}
