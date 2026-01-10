using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Components.Articles;

public sealed class WikiLinkAutocompleteItem
{
    private WikiLinkAutocompleteItem()
    {
    }

    public bool IsExternal { get; private init; }
    public string Title { get; private init; } = string.Empty;
    public string? SecondaryText { get; private init; }
    public string? Source { get; private init; }
    public Guid? ArticleId { get; private init; }
    public string? ExternalId { get; private init; }

    public string SourceBadge => string.IsNullOrWhiteSpace(Source)
        ? string.Empty
        : Source.ToUpperInvariant();

    public static WikiLinkAutocompleteItem FromInternal(LinkSuggestionDto suggestion)
    {
        return new WikiLinkAutocompleteItem
        {
            IsExternal = false,
            Title = suggestion.Title,
            SecondaryText = suggestion.DisplayPath,
            ArticleId = suggestion.ArticleId
        };
    }

    public static WikiLinkAutocompleteItem FromExternal(ExternalLinkSuggestionDto suggestion)
    {
        return new WikiLinkAutocompleteItem
        {
            IsExternal = true,
            Title = suggestion.Title,
            SecondaryText = suggestion.Subtitle,
            Source = suggestion.Source,
            ExternalId = suggestion.Id
        };
    }
}
