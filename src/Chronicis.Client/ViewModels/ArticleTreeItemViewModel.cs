namespace Chronicis.Client.ViewModels;

public class ArticleTreeItemViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public string? Body { get; set; }
    public bool HasChildren { get; set; }
    public bool IsExpanded { get; set; }
    public bool IsSelected { get; set; }
    public List<ArticleTreeItemViewModel> Children { get; set; } = new();
}
