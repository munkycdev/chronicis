using Chronicis.Shared.Enums;

namespace Chronicis.Client.ViewModels;

public class ArticleTreeItemViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public string? Body { get; set; }
    public bool HasChildren { get; set; }
    public bool IsExpanded { get; set; }
    public bool IsSelected { get; set; }
    public ArticleType Type { get; set; }
    public ArticleVisibility Visibility { get; set; }
    public List<ArticleTreeItemViewModel> Children { get; set; } = new();
}
