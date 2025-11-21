namespace Chronicis.Shared.Models;

public class ArticleSearchResultDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public List<AncestorDto> AncestorPath { get; set; } = new();
}

public class AncestorDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
}
