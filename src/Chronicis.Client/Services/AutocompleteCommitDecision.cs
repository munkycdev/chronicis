namespace Chronicis.Client.Services;

/// <summary>
/// Pure discriminated union returned by <see cref="AutocompleteCommitDecision.Decide"/>.
/// Hosts pattern-match on this to choose the correct commit path.
/// </summary>
public abstract record AutocompleteCommitDecision
{
    /// <summary>Select the existing suggestion at <see cref="Index"/>.</summary>
    public sealed record SelectExisting(int Index) : AutocompleteCommitDecision;

    /// <summary>Create a new article with the resolved <see cref="Name"/>.</summary>
    public sealed record CreateNew(string Name) : AutocompleteCommitDecision;

    /// <summary>No action — guard conditions were not met.</summary>
    public sealed record DoNothing() : AutocompleteCommitDecision;

    /// <summary>
    /// Determines what the host should do when Enter is pressed in the autocomplete.
    /// </summary>
    /// <param name="query">Current autocomplete query (internal remainder for external queries).</param>
    /// <param name="suggestionsCount">Number of suggestions currently visible.</param>
    /// <param name="selectedIndex">Currently highlighted suggestion index.</param>
    /// <param name="isExternalQuery">True when the query targets an external provider (e.g. srd/).</param>
    /// <param name="isMapQuery">True when the query targets the maps namespace.</param>
    public static AutocompleteCommitDecision Decide(
        string query,
        int suggestionsCount,
        int selectedIndex,
        bool isExternalQuery,
        bool isMapQuery)
    {
        if (suggestionsCount > 0 && selectedIndex >= 0 && selectedIndex < suggestionsCount)
            return new SelectExisting(selectedIndex);

        if (isExternalQuery || isMapQuery)
            return new DoNothing();

        var trimmed = (query ?? string.Empty).Trim();
        if (trimmed.Length < 3)
            return new DoNothing();

        var name = ExtractArticleName(trimmed);
        return string.IsNullOrEmpty(name) ? new DoNothing() : new CreateNew(name);
    }

    /// <summary>
    /// Extracts a display name from the query, taking the last path segment and
    /// capitalising the first letter — parity with ArticleDetailWikiLinkAutocomplete.GetArticleName.
    /// </summary>
    private static string ExtractArticleName(string query)
    {
        var segments = query.Split('/');
        var name = segments[^1].Trim();
        if (string.IsNullOrEmpty(name))
            return string.Empty;
        return char.ToUpper(name[0]) + name[1..];
    }
}
