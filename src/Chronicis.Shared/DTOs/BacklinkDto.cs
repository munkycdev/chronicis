namespace Chronicis.Shared.DTOs;

public class BacklinkDto
{
    public int ArticleId { get; set; }
    public string ArticleTitle { get; set; } = string.Empty;
    public string ArticleSlug { get; set; } = string.Empty;
    public List<string> Hashtags { get; set; } = new();
    public int MentionCount { get; set; }
    public DateTime LastModified { get; set; }
}
