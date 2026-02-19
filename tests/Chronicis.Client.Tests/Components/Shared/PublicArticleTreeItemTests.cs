using Bunit;
using Chronicis.Client.Components.Shared;
using Chronicis.Shared.DTOs;
using Xunit;

namespace Chronicis.Client.Tests.Components.Shared;

public class PublicArticleTreeItemTests : MudBlazorTestContext
{
    [Fact]
    public void PublicArticleTreeItem_Leaf_RendersSpacer()
    {
        var article = new ArticleTreeDto
        {
            Id = Guid.NewGuid(),
            Title = "Leaf",
            Slug = "leaf",
            HasChildren = false,
            Children = []
        };

        var cut = RenderComponent<PublicArticleTreeItem>(p => p
            .Add(x => x.Article, article)
            .Add(x => x.PublicSlug, "world"));

        Assert.NotEmpty(cut.FindAll(".public-tree-spacer"));
    }

    [Fact]
    public async Task PublicArticleTreeItem_RealArticle_Click_InvokesSelectionWithBuiltPath()
    {
        var article = new ArticleTreeDto
        {
            Id = Guid.NewGuid(),
            Title = "Chapter",
            Slug = "chapter",
            HasChildren = false,
            IsVirtualGroup = false
        };
        string? selected = null;

        var cut = RenderComponent<PublicArticleTreeItem>(p => p
            .Add(x => x.Article, article)
            .Add(x => x.ParentPath, "root")
            .Add(x => x.PublicSlug, "world")
            .Add(x => x.OnArticleSelected, path => selected = path));

        await cut.Find(".public-tree-item-content").ClickAsync(new());

        Assert.Equal("root/chapter", selected);
    }

    [Fact]
    public async Task PublicArticleTreeItem_VirtualGroup_Click_TogglesChildrenAndDoesNotNavigate()
    {
        var child = new ArticleTreeDto
        {
            Id = Guid.NewGuid(),
            Title = "Child",
            Slug = "child",
            IsVirtualGroup = false,
            HasChildren = false
        };
        var article = new ArticleTreeDto
        {
            Id = Guid.NewGuid(),
            Title = "Group",
            Slug = "group",
            IsVirtualGroup = true,
            HasChildren = true,
            Children = [child]
        };

        var callbackCalled = false;
        var cut = RenderComponent<PublicArticleTreeItem>(p => p
            .Add(x => x.Article, article)
            .Add(x => x.PublicSlug, "world")
            .Add(x => x.OnArticleSelected, _ => callbackCalled = true));

        await cut.Find(".public-tree-item-content").ClickAsync(new());

        Assert.NotEmpty(cut.FindAll(".public-tree-children"));
        Assert.False(callbackCalled);
    }

    [Fact]
    public void PublicArticleTreeItem_CurrentPathMatch_AddsSelectedClass()
    {
        var article = new ArticleTreeDto
        {
            Id = Guid.NewGuid(),
            Title = "Leaf",
            Slug = "leaf",
            IsVirtualGroup = false,
            HasChildren = false
        };

        var cut = RenderComponent<PublicArticleTreeItem>(p => p
            .Add(x => x.Article, article)
            .Add(x => x.PublicSlug, "world")
            .Add(x => x.CurrentPath, "leaf"));

        var container = cut.Find(".public-tree-item");
        Assert.Contains("selected", container.ClassName);
    }

    [Fact]
    public void PublicArticleTreeItem_PathMatchInDescendants_AutoExpands()
    {
        var child = new ArticleTreeDto
        {
            Id = Guid.NewGuid(),
            Title = "Child",
            Slug = "child",
            IsVirtualGroup = false,
            HasChildren = false
        };
        var article = new ArticleTreeDto
        {
            Id = Guid.NewGuid(),
            Title = "Parent",
            Slug = "parent",
            IsVirtualGroup = false,
            HasChildren = true,
            Children = [child]
        };

        var cut = RenderComponent<PublicArticleTreeItem>(p => p
            .Add(x => x.Article, article)
            .Add(x => x.PublicSlug, "world")
            .Add(x => x.CurrentPath, "parent/child"));

        Assert.NotEmpty(cut.FindAll(".public-tree-children"));
    }
}
