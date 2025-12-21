namespace Chronicis.Shared.DTOs;

public class HashtagPreviewDto
{
    public bool HasArticle { get; set; }
    public string HashtagName { get; set; } = string.Empty;
    public Guid? ArticleId { get; set; }
    public string? ArticleTitle { get; set; }
    public string? ArticleSlug { get; set; }
    public string? PreviewText { get; set; }
    public DateTime? LastModified { get; set; }
}
