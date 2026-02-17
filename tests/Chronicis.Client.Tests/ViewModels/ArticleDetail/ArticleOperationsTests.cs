using Chronicis.Client.Services;
using Chronicis.Client.ViewModels.ArticleDetail;
using Chronicis.Shared.DTOs;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.ViewModels.ArticleDetail;

public class ArticleOperationsTests
{
    private readonly IArticleDetailFacade _mockFacade;
    private readonly ArticleLoadingState _loadingState;
    private readonly ArticleEditState _editState;
    private readonly ArticleOperations _operations;

    public ArticleOperationsTests()
    {
        _mockFacade = Substitute.For<IArticleDetailFacade>();
        _loadingState = new ArticleLoadingState();
        _editState = new ArticleEditState();
        _operations = new ArticleOperations(_mockFacade, _loadingState, _editState);
    }

    [Fact]
    public async Task SaveArticleAsync_WithValidChanges_SavesAndUpdates()
    {
        // Arrange
        var articleId = Guid.NewGuid();
        var article = new ArticleDto
        {
            Id = articleId,
            Title = "Original",
            Body = "Body",
            EffectiveDate = DateTime.Now
        };
        var updatedArticle = new ArticleDto
        {
            Id = articleId,
            Title = "Updated",
            Body = "Updated Body",
            EffectiveDate = DateTime.Now
        };
        
        _loadingState.SetArticle(article);
        _editState.StartEdit(article);
        _editState.UpdateTitle("Updated");
        
        _mockFacade.UpdateArticleAsync(articleId, Arg.Any<ArticleUpdateDto>())
            .Returns(updatedArticle);

        // Act
        await _operations.SaveArticleAsync();

        // Assert
        Assert.False(_operations.IsSaving);
        Assert.Equal("Updated", _loadingState.Article!.Title);
        Assert.False(_editState.IsEditMode);
        await _mockFacade.Received(1).UpdateArticleAsync(articleId, Arg.Any<ArticleUpdateDto>());
        _mockFacade.Received(1).ShowSuccessNotification("Article saved");
    }

    [Fact]
    public async Task SaveArticleAsync_WhenNotInEditMode_DoesNothing()
    {
        // Arrange
        var article = new ArticleDto { Id = Guid.NewGuid(), Title = "Test" };
        _loadingState.SetArticle(article);
        // Not in edit mode

        // Act
        await _operations.SaveArticleAsync();

        // Assert
        await _mockFacade.DidNotReceive().UpdateArticleAsync(Arg.Any<Guid>(), Arg.Any<ArticleUpdateDto>());
    }

    [Fact]
    public async Task DeleteArticleAsync_WithSuccess_DeletesAndNavigates()
    {
        // Arrange
        var articleId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var article = new ArticleDto
        {
            Id = articleId,
            Title = "Test",
            ParentId = parentId
        };
        
        _loadingState.SetArticle(article);
        _mockFacade.DeleteArticleAsync(articleId).Returns(true);
        _mockFacade.GetArticleNavigationPathAsync(parentId).Returns("/article/parent");

        // Act
        await _operations.DeleteArticleAsync();

        // Assert
        Assert.False(_operations.IsDeleting);
        await _mockFacade.Received(1).DeleteArticleAsync(articleId);
        _mockFacade.Received(1).ShowSuccessNotification("Article deleted");
        await _mockFacade.Received(1).RefreshTreeAsync();
    }

    [Fact]
    public async Task CreateChildArticleAsync_WithValidTitle_CreatesAndNavigates()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var parentArticle = new ArticleDto
        {
            Id = parentId,
            Title = "Parent",
            WorldId = Guid.NewGuid()
        };
        var childArticle = new ArticleDto
        {
            Id = childId,
            Title = "Child",
            ParentId = parentId,
            Breadcrumbs = new List<BreadcrumbDto>
            {
                new() { Slug = "parent" },
                new() { Slug = "child" }
            }
        };
        
        _loadingState.SetArticle(parentArticle);
        _mockFacade.CreateArticleAsync(Arg.Any<ArticleCreateDto>()).Returns(childArticle);
        _mockFacade.BuildArticleUrlFromBreadcrumbs(Arg.Any<List<BreadcrumbDto>>())
            .Returns("/article/parent/child");

        // Act
        await _operations.CreateChildArticleAsync("Child");

        // Assert
        Assert.False(_operations.IsSaving);
        await _mockFacade.Received(1).CreateArticleAsync(Arg.Is<ArticleCreateDto>(dto =>
            dto.Title == "Child" && dto.ParentId == parentId));
        _mockFacade.Received(1).ShowSuccessNotification("Child article created");
        _mockFacade.Received(1).NavigateToArticle("/article/parent/child");
    }

    [Fact]
    public async Task CreateChildArticleAsync_WithEmptyTitle_DoesNothing()
    {
        // Arrange
        var article = new ArticleDto { Id = Guid.NewGuid(), Title = "Test" };
        _loadingState.SetArticle(article);

        // Act
        await _operations.CreateChildArticleAsync("");

        // Assert
        await _mockFacade.DidNotReceive().CreateArticleAsync(Arg.Any<ArticleCreateDto>());
    }
}
