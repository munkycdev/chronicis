using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Chronicis.Shared.Models
{
    /// <summary>
    /// Core entity representing a hierarchical article/note in Chronicis.
    /// Supports infinite nesting through self-referencing ParentId.
    /// </summary>
    public class Article
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(500)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Parent article ID. Null indicates root-level article.
        /// </summary>
        public int? ParentId { get; set; }

        /// <summary>
        /// Article content in plain text (Phase 1) or Markdown (Phase 4+).
        /// </summary>
        public string Body { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? ModifiedDate { get; set; }

        // Navigation properties for EF Core hierarchy
        public Article? Parent { get; set; }
        public ICollection<Article> Children { get; set; } = new List<Article>();
    }
}
