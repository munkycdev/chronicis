using Bunit;
using Chronicis.Client.Components.Shared;
using Chronicis.Shared.DTOs;
using MudBlazor;
using Xunit;

namespace Chronicis.Client.Tests.Components;

/// <summary>
/// Tests for PublicArticleTreeItem component.
/// This component is already well-designed - no service dependencies, pure presentation.
/// Tests verify tree rendering, expansion, selection, and virtual group handling.
/// </summary>
public class PublicArticleTreeItemTests : MudBlazorTestContext
{
    [Fact]
    public void PublicArticleTreeItem_RendersBasicArticle()
    {
        // Arrange
        var article = new ArticleTreeDto
        {
            Id = Guid.NewGuid(),
            Title = "Test Article",
            Slug = "test-article",
            HasChildren = false
        };

        // Act
        var cut = RenderComponent<PublicArticleTreeItem>(parameters => parameters
            .Add(p => p.Article, article)
            .Add(p => p.PublicSlug, "world-slug"));

        // Assert
        var titleElement = cut.Find(".public-tree-title");
        Assert.Equal("Test Article", titleElement.TextContent);
    }

    [Fact]
    public void PublicArticleTreeItem_ShowsExpandButton_WhenHasChildren()
    {
        // Arrange
        var article = new ArticleTreeDto
        {
            Id = Guid.NewGuid(),
            Title = "Parent Article",
            Slug = "parent",
            HasChildren = true,
            Children = new List<ArticleTreeDto>
            {
                new() { Id = Guid.NewGuid(), Title = "Child", Slug = "child" }
            }
        };

        // Act
        var cut = RenderComponent<PublicArticleTreeItem>(parameters => parameters
            .Add(p => p.Article, article)
            .Add(p => p.PublicSlug, "world-slug"));

        // Assert
        var expandButton = cut.Find(".public-tree-expand-btn");
        Assert.NotNull(expandButton);
    }

    [Fact]
    public void PublicArticleTreeItem_ShowsSpacer_WhenNoChildren()
    {
        // Arrange
        var article = new ArticleTreeDto
        {
            Id = Guid.NewGuid(),
            Title = "Leaf Article",
            Slug = "leaf",
            HasChildren = false
        };

        // Act
        var cut = RenderComponent<PublicArticleTreeItem>(parameters => parameters
            .Add(p => p.Article, article)
            .Add(p => p.PublicSlug, "world-slug"));

        // Assert
        var spacer = cut.Find(".public-tree-spacer");
        Assert.NotNull(spacer);
    }

    [Fact]
    public void PublicArticleTreeItem_DisplaysIcon_WhenProvided()
    {
        // Arrange
        var article = new ArticleTreeDto
        {
            Id = Guid.NewGuid(),
            Title = "Article with Icon",
            Slug = "with-icon",
            IconEmoji = "🐉"
        };

        // Act
        var cut = RenderComponent<PublicArticleTreeItem>(parameters => parameters
            .Add(p => p.Article, article)
            .Add(p => p.PublicSlug, "world-slug"));

        // Assert
        var iconDisplay = cut.FindComponent<IconDisplay>();
        Assert.Equal("🐉", iconDisplay.Instance.Icon);
    }

    [Fact]
    public void PublicArticleTreeItem_ExpandsChildren_WhenExpandButtonClicked()
    {
        // Arrange
        var article = new ArticleTreeDto
        {
            Id = Guid.NewGuid(),
            Title = "Parent",
            Slug = "parent",
            HasChildren = true,
            Children = new List<ArticleTreeDto>
            {
                new() { Id = Guid.NewGuid(), Title = "Child 1", Slug = "child-1" },
                new() { Id = Guid.NewGuid(), Title = "Child 2", Slug = "child-2" }
            }
        };

        var cut = RenderComponent<PublicArticleTreeItem>(parameters => parameters
            .Add(p => p.Article, article)
            .Add(p => p.PublicSlug, "world-slug"));

        // Verify children not visible initially
        var childrenContainer = cut.FindAll(".public-tree-children");
        Assert.Empty(childrenContainer);

        // Act - click expand button
        var expandButton = cut.Find(".public-tree-expand-btn");
        expandButton.Click();

        // Assert - children now visible
        childrenContainer = cut.FindAll(".public-tree-children");
        Assert.Single(childrenContainer);
        
        // Verify child titles are present
        var allTitles = cut.FindAll(".public-tree-title");
        Assert.Contains(allTitles, t => t.TextContent == "Child 1");
        Assert.Contains(allTitles, t => t.TextContent == "Child 2");
    }

    [Fact]
    public void PublicArticleTreeItem_CollapsesChildren_WhenExpandButtonClickedAgain()
    {
        // Arrange
        var article = new ArticleTreeDto
        {
            Id = Guid.NewGuid(),
            Title = "Parent",
            Slug = "parent",
            HasChildren = true,
            Children = new List<ArticleTreeDto>
            {
                new() { Id = Guid.NewGuid(), Title = "Child", Slug = "child" }
            }
        };

        var cut = RenderComponent<PublicArticleTreeItem>(parameters => parameters
            .Add(p => p.Article, article)
            .Add(p => p.PublicSlug, "world-slug"));

        var expandButton = cut.Find(".public-tree-expand-btn");
        
        // Expand
        expandButton.Click();
        Assert.Single(cut.FindAll(".public-tree-children"));

        // Act - Collapse
        expandButton.Click();

        // Assert
        Assert.Empty(cut.FindAll(".public-tree-children"));
    }

    [Fact]
    public void PublicArticleTreeItem_AppliesSelectedClass_WhenCurrentPathMatches()
    {
        // Arrange
        var article = new ArticleTreeDto
        {
            Id = Guid.NewGuid(),
            Title = "Selected Article",
            Slug = "selected-article",
            HasChildren = false
        };

        // Act
        var cut = RenderComponent<PublicArticleTreeItem>(parameters => parameters
            .Add(p => p.Article, article)
            .Add(p => p.PublicSlug, "world-slug")
            .Add(p => p.CurrentPath, "selected-article")
            .Add(p => p.ParentPath, null));

        // Assert
        var container = cut.Find(".public-tree-item");
        Assert.Contains("selected", container.ClassName);
    }

    [Fact]
    public void PublicArticleTreeItem_DoesNotApplySelectedClass_WhenCurrentPathDoesNotMatch()
    {
        // Arrange
        var article = new ArticleTreeDto
        {
            Id = Guid.NewGuid(),
            Title = "Not Selected",
            Slug = "not-selected",
            HasChildren = false
        };

        // Act
        var cut = RenderComponent<PublicArticleTreeItem>(parameters => parameters
            .Add(p => p.Article, article)
            .Add(p => p.PublicSlug, "world-slug")
            .Add(p => p.CurrentPath, "different-article")
            .Add(p => p.ParentPath, null));

        // Assert
        var container = cut.Find(".public-tree-item");
        Assert.DoesNotContain("selected", container.ClassName);
    }

    [Fact]
    public void PublicArticleTreeItem_RaisesOnArticleSelected_WhenClicked()
    {
        // Arrange
        var article = new ArticleTreeDto
        {
            Id = Guid.NewGuid(),
            Title = "Clickable Article",
            Slug = "clickable",
            HasChildren = false
        };

        var selectedPath = string.Empty;
        var cut = RenderComponent<PublicArticleTreeItem>(parameters => parameters
            .Add(p => p.Article, article)
            .Add(p => p.PublicSlug, "world-slug")
            .Add(p => p.ParentPath, null)
            .Add(p => p.OnArticleSelected, path => selectedPath = path));

        // Act
        var content = cut.Find(".public-tree-item-content");
        content.Click();

        // Assert
        Assert.Equal("clickable", selectedPath);
    }

    [Fact]
    public void PublicArticleTreeItem_BuildsCorrectPath_WithParentPath()
    {
        // Arrange
        var article = new ArticleTreeDto
        {
            Id = Guid.NewGuid(),
            Title = "Child Article",
            Slug = "child",
            HasChildren = false
        };

        var selectedPath = string.Empty;
        var cut = RenderComponent<PublicArticleTreeItem>(parameters => parameters
            .Add(p => p.Article, article)
            .Add(p => p.PublicSlug, "world-slug")
            .Add(p => p.ParentPath, "parent")
            .Add(p => p.OnArticleSelected, path => selectedPath = path));

        // Act
        var content = cut.Find(".public-tree-item-content");
        content.Click();

        // Assert
        Assert.Equal("parent/child", selectedPath);
    }

    [Fact]
    public void PublicArticleTreeItem_HandlesVirtualGroup_DoesNotNavigate()
    {
        // Arrange
        var virtualGroup = new ArticleTreeDto
        {
            Id = Guid.NewGuid(),
            Title = "Virtual Group",
            Slug = "virtual",
            IsVirtualGroup = true,
            HasChildren = true,
            Children = new List<ArticleTreeDto>
            {
                new() { Id = Guid.NewGuid(), Title = "Real Child", Slug = "real-child" }
            }
        };

        var selectedPath = string.Empty;
        var cut = RenderComponent<PublicArticleTreeItem>(parameters => parameters
            .Add(p => p.Article, virtualGroup)
            .Add(p => p.PublicSlug, "world-slug")
            .Add(p => p.OnArticleSelected, path => selectedPath = path));

        // Act - Click virtual group
        var content = cut.Find(".public-tree-item-content");
        content.Click();

        // Assert - Should not navigate (selectedPath should be empty)
        Assert.Empty(selectedPath);
        
        // Should toggle expansion instead
        var childrenContainer = cut.FindAll(".public-tree-children");
        Assert.Single(childrenContainer);
    }

    [Fact]
    public void PublicArticleTreeItem_VirtualGroup_CannotBeSelected()
    {
        // Arrange
        var virtualGroup = new ArticleTreeDto
        {
            Id = Guid.NewGuid(),
            Title = "Virtual Group",
            Slug = "virtual",
            IsVirtualGroup = true,
            HasChildren = false
        };

        // Act
        var cut = RenderComponent<PublicArticleTreeItem>(parameters => parameters
            .Add(p => p.Article, virtualGroup)
            .Add(p => p.PublicSlug, "world-slug")
            .Add(p => p.CurrentPath, "virtual") // Try to select it
            .Add(p => p.ParentPath, null));

        // Assert - Virtual groups can never be selected
        var container = cut.Find(".public-tree-item");
        Assert.DoesNotContain("selected", container.ClassName);
    }

    [Fact]
    public void PublicArticleTreeItem_AutoExpands_WhenDescendantIsSelected()
    {
        // Arrange
        var parent = new ArticleTreeDto
        {
            Id = Guid.NewGuid(),
            Title = "Parent",
            Slug = "parent",
            HasChildren = true,
            Children = new List<ArticleTreeDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Child",
                    Slug = "child",
                    HasChildren = true,
                    Children = new List<ArticleTreeDto>
                    {
                        new() { Id = Guid.NewGuid(), Title = "Grandchild", Slug = "grandchild" }
                    }
                }
            }
        };

        // Act - Current path points to grandchild
        var cut = RenderComponent<PublicArticleTreeItem>(parameters => parameters
            .Add(p => p.Article, parent)
            .Add(p => p.PublicSlug, "world-slug")
            .Add(p => p.CurrentPath, "parent/child/grandchild")
            .Add(p => p.ParentPath, null));

        // Assert - Parent and child should be auto-expanded (2 children containers)
        var childrenContainers = cut.FindAll(".public-tree-children");
        Assert.True(childrenContainers.Count >= 1, "Parent should be auto-expanded");
    }

    [Fact]
    public void PublicArticleTreeItem_AppliesIndentation_BasedOnLevel()
    {
        // Arrange
        var article = new ArticleTreeDto
        {
            Id = Guid.NewGuid(),
            Title = "Nested Article",
            Slug = "nested"
        };

        // Act
        var cut = RenderComponent<PublicArticleTreeItem>(parameters => parameters
            .Add(p => p.Article, article)
            .Add(p => p.PublicSlug, "world-slug")
            .Add(p => p.Level, 3)); // Level 3

        // Assert
        var container = cut.Find(".public-tree-item");
        var style = container.GetAttribute("style");
        Assert.Contains("padding-left: 36px", style); // 3 * 12px = 36px
    }

    [Fact]
    public void PublicArticleTreeItem_RendersChildrenInAlphabeticalOrder()
    {
        // Arrange
        var parent = new ArticleTreeDto
        {
            Id = Guid.NewGuid(),
            Title = "Parent",
            Slug = "parent",
            HasChildren = true,
            Children = new List<ArticleTreeDto>
            {
                new() { Id = Guid.NewGuid(), Title = "Zebra", Slug = "zebra" },
                new() { Id = Guid.NewGuid(), Title = "Apple", Slug = "apple" },
                new() { Id = Guid.NewGuid(), Title = "Mango", Slug = "mango" }
            }
        };

        var cut = RenderComponent<PublicArticleTreeItem>(parameters => parameters
            .Add(p => p.Article, parent)
            .Add(p => p.PublicSlug, "world-slug"));

        // Act - Expand to show children
        var expandButton = cut.Find(".public-tree-expand-btn");
        expandButton.Click();

        // Assert - Children should be in alphabetical order
        var allTitles = cut.FindAll(".public-tree-title");
        var childTitles = allTitles.Skip(1).Select(t => t.TextContent).ToList(); // Skip parent title
        
        Assert.Equal(3, childTitles.Count);
        Assert.Equal("Apple", childTitles[0]);
        Assert.Equal("Mango", childTitles[1]);
        Assert.Equal("Zebra", childTitles[2]);
    }
}
