using Chronicis.Shared.DTOs;

namespace Chronicis.Api.Models;

public class ArticleDto : ArticleDetailDto
{
    public bool HasChildren { get; set; } = false;
}

public class ArticleCreateDto
{
    public string Title { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public string Body { get; set; } = string.Empty;
}

public class ArticleUpdateDto
{
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}
