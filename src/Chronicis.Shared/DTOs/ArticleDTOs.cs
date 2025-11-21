using System;
using System.Collections.Generic;

namespace Chronicis.Shared.DTOs
{
    public class BaseArticleDto
    {

    }
    public class ArticleDto : ArticleDetailDto
    {
        public bool HasChildren { get; set; } = false;
    }

    public class ArticleCreateDto : BaseArticleDto
    {
        public string Title { get; set; } = string.Empty;
        public int? ParentId { get; set; }
        public string Body { get; set; } = string.Empty;
    }

    public class ArticleUpdateDto : BaseArticleDto
    {
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
    }

    /// <summary>
    /// Lightweight DTO for tree view display - only includes essential fields.
    /// </summary>
    public class ArticleTreeDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int? ParentId { get; set; }
        public bool HasChildren { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    /// <summary>
    /// Full article details with breadcrumb path for navigation.
    /// </summary>
    public class ArticleDetailDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int? ParentId { get; set; }
        public string Body { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        
        /// <summary>
        /// Breadcrumb trail from root to current article.
        /// Example: ["World", "Sword Coast", "Waterdeep"]
        /// </summary>
        public List<BreadcrumbDto> Breadcrumbs { get; set; } = new();
    }

    /// <summary>
    /// Breadcrumb item for navigation path display.
    /// </summary>
    public class BreadcrumbDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request DTO for creating new articles.
    /// </summary>
    public class CreateArticleDto
    {
        public string Title { get; set; } = string.Empty;
        public int? ParentId { get; set; }
        public string Body { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request DTO for updating existing articles.
    /// </summary>
    public class UpdateArticleDto
    {
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
    }
}
