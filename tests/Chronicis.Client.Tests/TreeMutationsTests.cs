using System.Diagnostics.CodeAnalysis;
using Chronicis.Client.Models;
using Chronicis.Client.Services;
using Chronicis.Client.Services.Tree;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests;

[ExcludeFromCodeCoverage]
public class TreeMutationsTests
{
    private readonly IArticleApiService _articleApi;
    private readonly IAppContextService _appContext;
    private readonly ILogger _logger;
    private readonly TreeMutations _mutations;
    private bool _refreshCallbackInvoked;

    public TreeMutationsTests()
    {
        _articleApi = Substitute.For<IArticleApiService>();
        _appContext = Substitute.For<IAppContextService>();
        _logger = NullLogger.Instance;
        _mutations = new TreeMutations(_articleApi, _appContext, _logger);

        _refreshCallbackInvoked = false;
        _mutations.SetRefreshCallback(() =>
        {
            _refreshCallbackInvoked = true;
            return Task.CompletedTask;
        });
    }

    // ============================================
    // Node Update Behavior Tests
    // ============================================

    [Fact]
    public void UpdateNodeDisplay_ShouldUpdateTitleAndIcon()
    {
        // Arrange
        var nodeIndex = new TreeNodeIndex();
        var node = CreateArticleNode(Guid.NewGuid(), "Original Title", "fa-file");
        nodeIndex.AddNode(node);
        _mutations.SetNodeIndex(nodeIndex);

        // Act
        var result = _mutations.UpdateNodeDisplay(node.Id, "New Title", "fa-star");

        // Assert
        Assert.True(result);
        Assert.Equal("New Title", node.Title);
        Assert.Equal("fa-star", node.IconEmoji);
    }

    [Fact]
    public void UpdateNodeDisplay_OnNonExistentNode_ShouldReturnFalse()
    {
        // Arrange
        _mutations.SetNodeIndex(new TreeNodeIndex());

        // Act
        var result = _mutations.UpdateNodeDisplay(Guid.NewGuid(), "Title", "icon");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void UpdateNodeVisibility_ShouldUpdateVisibilitySetting()
    {
        // Arrange
        var nodeIndex = new TreeNodeIndex();
        var node = CreateArticleNode(Guid.NewGuid(), "Article");
        node.Visibility = ArticleVisibility.Public;
        nodeIndex.AddNode(node);
        _mutations.SetNodeIndex(nodeIndex);

        // Act
        var result = _mutations.UpdateNodeVisibility(node.Id, ArticleVisibility.Private);

        // Assert
        Assert.True(result);
        Assert.Equal(ArticleVisibility.Private, node.Visibility);
    }

    [Fact]
    public void UpdateNodeVisibility_OnNonExistentNode_ShouldReturnFalse()
    {
        // Arrange
        _mutations.SetNodeIndex(new TreeNodeIndex());

        // Act
        var result = _mutations.UpdateNodeVisibility(Guid.NewGuid(), ArticleVisibility.Private);

        // Assert
        Assert.False(result);
    }

    // ============================================
    // Validation Tests
    // ============================================

    [Fact]
    public void IsDescendantOf_ShouldReturnTrueForDirectChild()
    {
        // Arrange
        var nodeIndex = new TreeNodeIndex();
        var parent = CreateArticleNode(Guid.NewGuid(), "Parent");
        var child = CreateArticleNode(Guid.NewGuid(), "Child");
        child.ParentId = parent.Id;

        nodeIndex.AddNode(parent);
        nodeIndex.AddNode(child);
        _mutations.SetNodeIndex(nodeIndex);

        // Act
        var result = _mutations.IsDescendantOf(child.Id, parent.Id);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsDescendantOf_ShouldReturnTrueForDeepDescendant()
    {
        // Arrange
        var nodeIndex = new TreeNodeIndex();
        var grandparent = CreateArticleNode(Guid.NewGuid(), "Grandparent");
        var parent = CreateArticleNode(Guid.NewGuid(), "Parent");
        var child = CreateArticleNode(Guid.NewGuid(), "Child");

        parent.ParentId = grandparent.Id;
        child.ParentId = parent.Id;

        nodeIndex.AddNode(grandparent);
        nodeIndex.AddNode(parent);
        nodeIndex.AddNode(child);
        _mutations.SetNodeIndex(nodeIndex);

        // Act
        var result = _mutations.IsDescendantOf(child.Id, grandparent.Id);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsDescendantOf_ShouldReturnFalseForUnrelatedNodes()
    {
        // Arrange
        var nodeIndex = new TreeNodeIndex();
        var node1 = CreateArticleNode(Guid.NewGuid(), "Node 1");
        var node2 = CreateArticleNode(Guid.NewGuid(), "Node 2");

        nodeIndex.AddNode(node1);
        nodeIndex.AddNode(node2);
        _mutations.SetNodeIndex(nodeIndex);

        // Act
        var result = _mutations.IsDescendantOf(node1.Id, node2.Id);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsDescendantOf_ShouldReturnFalseForSameNode()
    {
        // Arrange
        var nodeIndex = new TreeNodeIndex();
        var node = CreateArticleNode(Guid.NewGuid(), "Node");
        nodeIndex.AddNode(node);
        _mutations.SetNodeIndex(nodeIndex);

        // Act
        var result = _mutations.IsDescendantOf(node.Id, node.Id);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidArticle_ShouldReturnTrueForArticleNode()
    {
        // Arrange
        var nodeIndex = new TreeNodeIndex();
        var article = CreateArticleNode(Guid.NewGuid(), "Article");
        nodeIndex.AddNode(article);
        _mutations.SetNodeIndex(nodeIndex);

        // Act
        var result = _mutations.IsValidArticle(article.Id);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidArticle_ShouldReturnFalseForNonArticleNode()
    {
        // Arrange
        var nodeIndex = new TreeNodeIndex();
        var world = new TreeNode
        {
            Id = Guid.NewGuid(),
            NodeType = TreeNodeType.World,
            Title = "World"
        };
        nodeIndex.AddNode(world);
        _mutations.SetNodeIndex(nodeIndex);

        // Act
        var result = _mutations.IsValidArticle(world.Id);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidArticle_ShouldReturnFalseForNonExistentNode()
    {
        // Arrange
        _mutations.SetNodeIndex(new TreeNodeIndex());

        // Act
        var result = _mutations.IsValidArticle(Guid.NewGuid());

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanAcceptChildren_ShouldReturnTrueForArticle()
    {
        // Arrange
        var nodeIndex = new TreeNodeIndex();
        var article = CreateArticleNode(Guid.NewGuid(), "Article");
        nodeIndex.AddNode(article);
        _mutations.SetNodeIndex(nodeIndex);

        // Act
        var result = _mutations.CanAcceptChildren(article.Id);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanAcceptChildren_ShouldReturnFalseForCampaignsGroup()
    {
        // Arrange
        var nodeIndex = new TreeNodeIndex();
        var campaignsGroup = new TreeNode
        {
            Id = Guid.NewGuid(),
            NodeType = TreeNodeType.VirtualGroup,
            VirtualGroupType = VirtualGroupType.Campaigns,
            Title = "Campaigns"
        };
        nodeIndex.AddNode(campaignsGroup);
        _mutations.SetNodeIndex(nodeIndex);

        // Act
        var result = _mutations.CanAcceptChildren(campaignsGroup.Id);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidDropTarget_ShouldReturnTrueForArticle()
    {
        // Arrange
        var nodeIndex = new TreeNodeIndex();
        var article = CreateArticleNode(Guid.NewGuid(), "Article");
        nodeIndex.AddNode(article);
        _mutations.SetNodeIndex(nodeIndex);

        // Act
        var result = _mutations.IsValidDropTarget(article.Id);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidDropTarget_ShouldReturnFalseForLinksGroup()
    {
        // Arrange
        var nodeIndex = new TreeNodeIndex();
        var linksGroup = new TreeNode
        {
            Id = Guid.NewGuid(),
            NodeType = TreeNodeType.VirtualGroup,
            VirtualGroupType = VirtualGroupType.Links,
            Title = "External Resources"
        };
        nodeIndex.AddNode(linksGroup);
        _mutations.SetNodeIndex(nodeIndex);

        // Act
        var result = _mutations.IsValidDropTarget(linksGroup.Id);

        // Assert
        Assert.False(result);
    }

    // ============================================
    // Create Operation Tests
    // ============================================

    [Fact]
    public async Task CreateRootArticleAsync_WithNoWorld_ShouldReturnNull()
    {
        // Arrange
        _appContext.CurrentWorldId.Returns((Guid?)null);
        _mutations.SetNodeIndex(new TreeNodeIndex());

        // Act
        var result = await _mutations.CreateRootArticleAsync();

        // Assert
        Assert.Null(result);
        await _articleApi.DidNotReceive().CreateArticleAsync(Arg.Any<ArticleCreateDto>());
    }

    [Fact]
    public async Task CreateRootArticleAsync_ShouldCallApiAndRefresh()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var newArticleId = Guid.NewGuid();
        _appContext.CurrentWorldId.Returns(worldId);
        _articleApi.CreateArticleAsync(Arg.Any<ArticleCreateDto>())
            .Returns(new ArticleDto { Id = newArticleId, Title = "" });
        _mutations.SetNodeIndex(new TreeNodeIndex());

        // Act
        var result = await _mutations.CreateRootArticleAsync();

        // Assert
        Assert.Equal(newArticleId, result);
        Assert.True(_refreshCallbackInvoked);
        await _articleApi.Received(1).CreateArticleAsync(Arg.Is<ArticleCreateDto>(dto =>
            dto.WorldId == worldId &&
            dto.ParentId == null &&
            dto.Type == ArticleType.WikiArticle));
    }

    [Fact]
    public async Task CreateChildArticleAsync_WithNonExistentParent_ShouldReturnNull()
    {
        // Arrange
        _mutations.SetNodeIndex(new TreeNodeIndex());

        // Act
        var result = await _mutations.CreateChildArticleAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
        await _articleApi.DidNotReceive().CreateArticleAsync(Arg.Any<ArticleCreateDto>());
    }

    [Fact]
    public async Task CreateChildArticleAsync_UnderArc_ShouldCreateSessionArticle()
    {
        // Arrange
        var nodeIndex = new TreeNodeIndex();
        var arcId = Guid.NewGuid();
        var arcNode = new TreeNode
        {
            Id = arcId,
            NodeType = TreeNodeType.Arc,
            Title = "Test Arc",
            WorldId = Guid.NewGuid(),
            CampaignId = Guid.NewGuid()
        };
        nodeIndex.AddNode(arcNode);
        _mutations.SetNodeIndex(nodeIndex);

        var newArticleId = Guid.NewGuid();
        _articleApi.CreateArticleAsync(Arg.Any<ArticleCreateDto>())
            .Returns(new ArticleDto { Id = newArticleId });

        // Act
        var result = await _mutations.CreateChildArticleAsync(arcId);

        // Assert
        Assert.Equal(newArticleId, result);
        await _articleApi.Received(1).CreateArticleAsync(Arg.Is<ArticleCreateDto>(dto =>
            dto.Type == ArticleType.Session &&
            dto.ArcId == arcId));
    }

    // ============================================
    // Delete Operation Tests
    // ============================================

    [Fact]
    public async Task DeleteArticleAsync_WithNonExistentNode_ShouldReturnFalse()
    {
        // Arrange
        _mutations.SetNodeIndex(new TreeNodeIndex());

        // Act
        var result = await _mutations.DeleteArticleAsync(Guid.NewGuid());

        // Assert
        Assert.False(result);
        await _articleApi.DidNotReceive().DeleteArticleAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task DeleteArticleAsync_WithNonArticleNode_ShouldReturnFalse()
    {
        // Arrange
        var nodeIndex = new TreeNodeIndex();
        var world = new TreeNode
        {
            Id = Guid.NewGuid(),
            NodeType = TreeNodeType.World,
            Title = "World"
        };
        nodeIndex.AddNode(world);
        _mutations.SetNodeIndex(nodeIndex);

        // Act
        var result = await _mutations.DeleteArticleAsync(world.Id);

        // Assert
        Assert.False(result);
        await _articleApi.DidNotReceive().DeleteArticleAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task DeleteArticleAsync_WithValidArticle_ShouldCallApiAndRefresh()
    {
        // Arrange
        var nodeIndex = new TreeNodeIndex();
        var article = CreateArticleNode(Guid.NewGuid(), "Test Article");
        nodeIndex.AddNode(article);
        _mutations.SetNodeIndex(nodeIndex);

        // Act
        var result = await _mutations.DeleteArticleAsync(article.Id);

        // Assert
        Assert.True(result);
        Assert.True(_refreshCallbackInvoked);
        await _articleApi.Received(1).DeleteArticleAsync(article.Id);
    }

    // ============================================
    // Move Operation Tests
    // ============================================

    [Fact]
    public async Task MoveArticleAsync_ToItself_ShouldReturnFalse()
    {
        // Arrange
        var nodeIndex = new TreeNodeIndex();
        var article = CreateArticleNode(Guid.NewGuid(), "Article");
        nodeIndex.AddNode(article);
        _mutations.SetNodeIndex(nodeIndex);

        // Act
        var result = await _mutations.MoveArticleAsync(article.Id, article.Id);

        // Assert
        Assert.False(result);
        await _articleApi.DidNotReceive().MoveArticleAsync(Arg.Any<Guid>(), Arg.Any<Guid?>());
    }

    [Fact]
    public async Task MoveArticleAsync_ToDescendant_ShouldReturnFalse()
    {
        // Arrange
        var nodeIndex = new TreeNodeIndex();
        var parent = CreateArticleNode(Guid.NewGuid(), "Parent");
        var child = CreateArticleNode(Guid.NewGuid(), "Child");
        child.ParentId = parent.Id;

        nodeIndex.AddNode(parent);
        nodeIndex.AddNode(child);
        _mutations.SetNodeIndex(nodeIndex);

        // Act
        var result = await _mutations.MoveArticleAsync(parent.Id, child.Id);

        // Assert
        Assert.False(result);
        await _articleApi.DidNotReceive().MoveArticleAsync(Arg.Any<Guid>(), Arg.Any<Guid?>());
    }

    [Fact]
    public async Task MoveArticleAsync_ToCampaignsGroup_ShouldReturnFalse()
    {
        // Arrange
        var nodeIndex = new TreeNodeIndex();
        var article = CreateArticleNode(Guid.NewGuid(), "Article");
        var campaignsGroup = new TreeNode
        {
            Id = Guid.NewGuid(),
            NodeType = TreeNodeType.VirtualGroup,
            VirtualGroupType = VirtualGroupType.Campaigns,
            Title = "Campaigns"
        };

        nodeIndex.AddNode(article);
        nodeIndex.AddNode(campaignsGroup);
        _mutations.SetNodeIndex(nodeIndex);

        // Act
        var result = await _mutations.MoveArticleAsync(article.Id, campaignsGroup.Id);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task MoveArticleAsync_ToWikiGroup_ShouldMoveAndChangeType()
    {
        // Arrange
        var nodeIndex = new TreeNodeIndex();
        var article = CreateArticleNode(Guid.NewGuid(), "Article");
        article.ArticleType = ArticleType.Character;

        var wikiGroup = new TreeNode
        {
            Id = Guid.NewGuid(),
            NodeType = TreeNodeType.VirtualGroup,
            VirtualGroupType = VirtualGroupType.Wiki,
            Title = "Wiki"
        };

        nodeIndex.AddNode(article);
        nodeIndex.AddNode(wikiGroup);
        _mutations.SetNodeIndex(nodeIndex);

        _articleApi.MoveArticleAsync(article.Id, null).Returns(true);
        _articleApi.GetArticleDetailAsync(article.Id).Returns(new ArticleDto
        {
            Id = article.Id,
            Title = "Article",
            Body = "Content"
        });
        _articleApi.UpdateArticleAsync(article.Id, Arg.Any<ArticleUpdateDto>())
            .Returns(new ArticleDto { Id = article.Id });

        // Act
        var result = await _mutations.MoveArticleAsync(article.Id, wikiGroup.Id);

        // Assert
        Assert.True(result);
        Assert.True(_refreshCallbackInvoked);
        await _articleApi.Received(1).MoveArticleAsync(article.Id, null);
        await _articleApi.Received(1).UpdateArticleAsync(article.Id,
            Arg.Is<ArticleUpdateDto>(dto => dto.Type == ArticleType.WikiArticle));
    }

    [Fact]
    public async Task MoveArticleAsync_ToValidArticle_ShouldSucceed()
    {
        // Arrange
        var nodeIndex = new TreeNodeIndex();
        var source = CreateArticleNode(Guid.NewGuid(), "Source");
        var target = CreateArticleNode(Guid.NewGuid(), "Target");

        nodeIndex.AddNode(source);
        nodeIndex.AddNode(target);
        _mutations.SetNodeIndex(nodeIndex);

        _articleApi.MoveArticleAsync(source.Id, target.Id).Returns(true);

        // Act
        var result = await _mutations.MoveArticleAsync(source.Id, target.Id);

        // Assert
        Assert.True(result);
        Assert.True(_refreshCallbackInvoked);
        await _articleApi.Received(1).MoveArticleAsync(source.Id, target.Id);
    }

    // ============================================
    // Helper Methods
    // ============================================

    private static TreeNode CreateArticleNode(Guid id, string title, string? icon = null)
    {
        return new TreeNode
        {
            Id = id,
            NodeType = TreeNodeType.Article,
            ArticleType = ArticleType.WikiArticle,
            Title = title,
            IconEmoji = icon
        };
    }
}
