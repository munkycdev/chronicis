using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Components.Articles;

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

    public string SourceBadge => string.IsNullOrWhiteSpace(Source)
        ? string.Empty
        : Source.ToUpperInvariant();

    public static WikiLinkAutocompleteItem FromInternal(LinkSuggestionDto suggestion)
    {
        return new WikiLinkAutocompleteItem
        {
            IsExternal = false,
            IsCategory = false,
            Title = suggestion.Title,
            SecondaryText = suggestion.DisplayPath,
            ArticleId = suggestion.ArticleId
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
            Icon = suggestion.Icon
        };
    }
}
