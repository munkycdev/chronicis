using System;
using System.Collections.Generic;

namespace Chronicis.Shared.DTOs
{
    /// <summary>
    /// Full article with all details and optional children.
    /// Used for detailed views, editing, and full CRUD operations.
    /// </summary>
    public class ArticleDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int? ParentId { get; set; }
        public string Body { get; set; } = string.Empty;

        // Timestamps
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public DateTime EffectiveDate { get; set; }

        // Tree/hierarchy support
        public bool HasChildren { get; set; } = false;
        public int ChildCount { get; set; } = 0;
        public ICollection<ArticleDto>? Children { get; set; }

        // Navigation
        public List<BreadcrumbDto> Breadcrumbs { get; set; } = new();

        // Phase 11: Custom icons
        public string? IconEmoji { get; set; }
    }

    /// <summary>
    /// Lightweight DTO for tree view display.
    /// Contains only essential fields for efficient navigation rendering.
    /// </summary>
    public class ArticleTreeDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int? ParentId { get; set; }

        // Tree/hierarchy support
        public bool HasChildren { get; set; }
        public int ChildCount { get; set; } = 0;
        public ICollection<ArticleTreeDto>? Children { get; set; }

        // Timestamps
        public DateTime CreatedDate { get; set; }
        public DateTime EffectiveDate { get; set; }

        // Phase 11: Custom icons
        public string? IconEmoji { get; set; }
    }

    /// <summary>
    /// Request DTO for creating new articles.
    /// </summary>
    public class ArticleCreateDto
    {
        public string Title { get; set; } = string.Empty;
        public int? ParentId { get; set; }
        public string Body { get; set; } = string.Empty;

        /// <summary>
        /// Optional effective date. If null, defaults to CreatedDate.
        /// </summary>
        public DateTime? EffectiveDate { get; set; }
    }

    /// <summary>
    /// Request DTO for updating existing articles.
    /// </summary>
    public class ArticleUpdateDto
    {
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public DateTime? EffectiveDate { get; set; }
        public string? IconEmoji { get; set; }
    }

    /// <summary>
    /// Breadcrumb item for navigation path display.
    /// Used to show the path from root to current article.
    /// </summary>
    public class BreadcrumbDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
    }
}