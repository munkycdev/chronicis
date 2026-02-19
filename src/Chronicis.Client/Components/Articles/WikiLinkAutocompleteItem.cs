using Chronicis.Shared.DTOs;
using System.Diagnostics.CodeAnalysis;

namespace Chronicis.Client.Components.Articles;

[ExcludeFromCodeCoverage]
public sealed class WikiLinkAutocompleteItem
{
    private WikiLinkAutocompleteItem()
    {
    }

    public bool IsExternal { get; private init; }
    public bool IsCategory { get; private init; }
    public string Title { get; private init; } = string.Empty;
    public string? SecondaryText { get; private init; }
    public string? Source { get; private init; }
    public Guid? ArticleId { get; private init; }
    public string? ExternalId { get; private init; }
    public string? CategoryKey { get; private init; }
    public string? Icon { get; private init; }

    /// <summary>
    /// If the suggestion matched via an alias, this contains the matched alias.
    /// Used for display: "MatchedAlias (Title)"
    /// </summary>
    public string? MatchedAlias { get; private init; }

    public string SourceBadge => string.IsNullOrWhiteSpace(Source)
        ? string.Empty
        : Source.ToUpperInvariant();

    /// <summary>
    /// Gets the display title. If matched via alias, shows "Alias (Title)".
    /// </summary>
    public string DisplayTitle => !string.IsNullOrWhiteSpace(MatchedAlias)
        ? $"{MatchedAlias} ({Title})"
        : Title;

    public static WikiLinkAutocompleteItem FromInternal(LinkSuggestionDto suggestion)
    {
        return new WikiLinkAutocompleteItem
        {
            IsExternal = false,
            IsCategory = false,
            Title = suggestion.Title,
            SecondaryText = suggestion.DisplayPath,
            ArticleId = suggestion.ArticleId,
            MatchedAlias = suggestion.MatchedAlias
        };
    }

    public static WikiLinkAutocompleteItem FromExternal(ExternalLinkSuggestionDto suggestion)
    {
        var isCategory = suggestion.Category == "_category";

        return new WikiLinkAutocompleteItem
        {
            IsExternal = true,
            IsCategory = isCategory,
            Title = isCategory ? $"{suggestion.Icon} {suggestion.Title}" : suggestion.Title,
            SecondaryText = suggestion.Subtitle,
            Source = suggestion.Source,
            ExternalId = isCategory ? null : suggestion.Id,
            CategoryKey = isCategory ? suggestion.Id?.Replace("_category/", "") : null,
            Icon = suggestion.Icon,
            MatchedAlias = null // External links don't have aliases
        };
    }
}
