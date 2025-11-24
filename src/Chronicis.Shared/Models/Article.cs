using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Chronicis.Shared.Models
{
    /// <summary>
    /// Core entity representing a hierarchical article/note in Chronicis.
    /// Supports infinite nesting through self-referencing ParentId.
    /// </summary>
    public class Article
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int? ParentId { get; set; }
        public string? Body { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public DateTime EffectiveDate { get; set; }  // NEW - user-editable campaign date

        public string? IconEmoji { get; set; }  // NEW

        // Navigation properties
        public Article? Parent { get; set; }
        public ICollection<Article>? Children { get; set; }

        // Computed
        [NotMapped]
        public int ChildCount => Children?.Count ?? 0;
    }
}
