using System.Diagnostics.CodeAnalysis;
using Chronicis.Client.Models;
using Chronicis.Client.Services.Tree;
using Xunit;

namespace Chronicis.Client.Tests;

[ExcludeFromCodeCoverage]
public class TreeNodeIndexTests
{
    [Fact]
    public void AddNode_ShouldMakeNodeRetrievable()
    {
        // Arrange
        var index = new TreeNodeIndex();
        var node = CreateArticleNode(Guid.NewGuid(), "Test Article");

        // Act
        index.AddNode(node);

        // Assert
        Assert.True(index.TryGetNode(node.Id, out var retrieved));
        Assert.Same(node, retrieved);
    }

    [Fact]
    public void AddRootNode_ShouldAddToBothIndexAndRootNodes()
    {
        // Arrange
        var index = new TreeNodeIndex();
        var node = CreateWorldNode(Guid.NewGuid(), "Test World");

        // Act
        index.AddRootNode(node);

        // Assert
        Assert.True(index.ContainsNode(node.Id));
        Assert.Contains(node, index.RootNodes);
        Assert.Single(index.RootNodes);
    }

    [Fact]
    public void TryGetNode_ShouldReturnFalseForNonExistentNode()
    {
        // Arrange
        var index = new TreeNodeIndex();
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = index.TryGetNode(nonExistentId, out var node);

        // Assert
        Assert.False(result);
        Assert.Null(node);
    }

    [Fact]
    public void GetNode_ShouldReturnNullForNonExistentNode()
    {
        // Arrange
        var index = new TreeNodeIndex();

        // Act
        var node = index.GetNode(Guid.NewGuid());

        // Assert
        Assert.Null(node);
    }

    [Fact]
    public void ContainsNode_ShouldReturnTrueForExistingNode()
    {
        // Arrange
        var index = new TreeNodeIndex();
        var node = CreateArticleNode(Guid.NewGuid(), "Test");
        index.AddNode(node);

        // Act & Assert
        Assert.True(index.ContainsNode(node.Id));
    }

    [Fact]
    public void ContainsNode_ShouldReturnFalseForNonExistingNode()
    {
        // Arrange
        var index = new TreeNodeIndex();

        // Act & Assert
        Assert.False(index.ContainsNode(Guid.NewGuid()));
    }

    [Fact]
    public void Clear_ShouldRemoveAllNodesAndRootNodes()
    {
        // Arrange
        var index = new TreeNodeIndex();
        var rootNode = CreateWorldNode(Guid.NewGuid(), "World");
        var childNode = CreateArticleNode(Guid.NewGuid(), "Article");

        index.AddRootNode(rootNode);
        index.AddNode(childNode);

        // Act
        index.Clear();

        // Assert
        Assert.Empty(index.RootNodes);
        Assert.Equal(0, index.Count);
        Assert.False(index.ContainsNode(rootNode.Id));
        Assert.False(index.ContainsNode(childNode.Id));
    }

    [Fact]
    public void Count_ShouldReflectNumberOfIndexedNodes()
    {
        // Arrange
        var index = new TreeNodeIndex();

        // Act & Assert
        Assert.Equal(0, index.Count);

        index.AddNode(CreateArticleNode(Guid.NewGuid(), "One"));
        Assert.Equal(1, index.Count);

        index.AddNode(CreateArticleNode(Guid.NewGuid(), "Two"));
        Assert.Equal(2, index.Count);
    }

    [Fact]
    public void AllNodes_ShouldEnumerateAllIndexedNodes()
    {
        // Arrange
        var index = new TreeNodeIndex();
        var node1 = CreateArticleNode(Guid.NewGuid(), "One");
        var node2 = CreateArticleNode(Guid.NewGuid(), "Two");
        var node3 = CreateArticleNode(Guid.NewGuid(), "Three");

        index.AddNode(node1);
        index.AddNode(node2);
        index.AddNode(node3);

        // Act
        var allNodes = index.AllNodes.ToList();

        // Assert
        Assert.Equal(3, allNodes.Count);
        Assert.Contains(node1, allNodes);
        Assert.Contains(node2, allNodes);
        Assert.Contains(node3, allNodes);
    }

    [Fact]
    public void FindParentNode_ShouldFindNodeContainingChild()
    {
        // Arrange
        var index = new TreeNodeIndex();
        var parent = CreateArticleNode(Guid.NewGuid(), "Parent");
        var child = CreateArticleNode(Guid.NewGuid(), "Child");

        parent.Children.Add(child);
        index.AddNode(parent);
        index.AddNode(child);

        // Act
        var foundParent = index.FindParentNode(child);

        // Assert
        Assert.Same(parent, foundParent);
    }

    [Fact]
    public void FindParentNode_ShouldReturnNullForRootNode()
    {
        // Arrange
        var index = new TreeNodeIndex();
        var rootNode = CreateWorldNode(Guid.NewGuid(), "World");
        index.AddRootNode(rootNode);

        // Act
        var parent = index.FindParentNode(rootNode);

        // Assert
        Assert.Null(parent);
    }

    [Fact]
    public void SetRootNodes_ShouldReplaceRootNodesCollection()
    {
        // Arrange
        var index = new TreeNodeIndex();
        var oldRoot = CreateWorldNode(Guid.NewGuid(), "Old World");
        index.AddRootNode(oldRoot);

        var newRoot1 = CreateWorldNode(Guid.NewGuid(), "New World 1");
        var newRoot2 = CreateWorldNode(Guid.NewGuid(), "New World 2");
        var newRootNodes = new List<TreeNode> { newRoot1, newRoot2 };

        // Act
        index.SetRootNodes(newRootNodes);

        // Assert
        Assert.Equal(2, index.RootNodes.Count);
        Assert.Contains(newRoot1, index.RootNodes);
        Assert.Contains(newRoot2, index.RootNodes);
        Assert.DoesNotContain(oldRoot, index.RootNodes);

        // Note: SetRootNodes doesn't add to index, just replaces the collection
        // The old root is still in the index if it was added via AddNode/AddRootNode
        Assert.True(index.ContainsNode(oldRoot.Id));
    }

    [Fact]
    public void AddNode_WithSameId_ShouldOverwriteExistingNode()
    {
        // Arrange
        var index = new TreeNodeIndex();
        var id = Guid.NewGuid();
        var originalNode = CreateArticleNode(id, "Original");
        var updatedNode = CreateArticleNode(id, "Updated");

        index.AddNode(originalNode);

        // Act
        index.AddNode(updatedNode);

        // Assert
        Assert.True(index.TryGetNode(id, out var retrieved));
        Assert.Equal("Updated", retrieved!.Title);
        Assert.Same(updatedNode, retrieved);
    }

    // ============================================
    // Helper Methods
    // ============================================

    private static TreeNode CreateArticleNode(Guid id, string title)
    {
        return new TreeNode
        {
            Id = id,
            NodeType = TreeNodeType.Article,
            Title = title
        };
    }

    private static TreeNode CreateWorldNode(Guid id, string title)
    {
        return new TreeNode
        {
            Id = id,
            NodeType = TreeNodeType.World,
            Title = title,
            WorldId = id
        };
    }
}
