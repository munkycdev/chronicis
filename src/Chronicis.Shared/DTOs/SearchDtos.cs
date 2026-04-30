using System.Diagnostics.CodeAnalysis;
using Chronicis.Shared.Enums;

namespace Chronicis.Shared.DTOs;

[ExcludeFromCodeCoverage]
public class ArticleSearchResultDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string MatchSnippet { get; set; } = string.Empty;
    public string MatchType { get; set; } = string.Empty; // "title", "content", or "hashtag"
    public List<BreadcrumbDto> AncestorPath { get; set; } = new();
    public DateTime LastModified { get; set; }

    // URL context fields for typed navigation
    public string WorldSlug { get; set; } = string.Empty;
    public List<string> ArticleSlugChain { get; set; } = new();
    public ArticleType Type { get; set; }
    public string? CampaignSlug { get; set; }
    public string? ArcSlug { get; set; }
    public string? SessionSlug { get; set; }
}

[ExcludeFromCodeCoverage]
public class GlobalSearchResultsDto
{
    public string Query { get; set; } = string.Empty;
    public List<ArticleSearchResultDto> TitleMatches { get; set; } = new();
    public List<ArticleSearchResultDto> BodyMatches { get; set; } = new();
    public List<ArticleSearchResultDto> HashtagMatches { get; set; } = new();
    public int TotalResults { get; set; }
}
