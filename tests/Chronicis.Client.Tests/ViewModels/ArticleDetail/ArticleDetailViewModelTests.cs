using Chronicis.Client.Services;
using Chronicis.Client.ViewModels.ArticleDetail;
using Chronicis.Shared.DTOs;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.ViewModels.ArticleDetail;

public class ArticleDetailViewModelTests
{
    private readonly IArticleDetailFacade _mockFacade;
    private readonly ArticleDetailViewModel _viewModel;

    public ArticleDetailViewModelTests()
    {
        _mockFacade = Substitute.For<IArticleDetailFacade>();
        _viewModel = new ArticleDetailViewModel(_mockFacade);
    }

    [Fact]
    public void Constructor_WithNullFacade_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ArticleDetailViewModel(null!));
    }

    [Fact]
    public async Task LoadArticleAsync_WithValidId_LoadsArticle()
    {
        // Arrange
        var articleId = Guid.NewGuid();
        var article = new ArticleDto
        {
            Id = articleId,
            Title = "Test Article",
            Body = "Content"
        };
        _mockFacade.GetArticleAsync(articleId).Returns(article);

        // Act
        await _viewModel.LoadArticleAsync(articleId);

        // Assert
        Assert.NotNull(_viewModel.Article);
        Assert.Equal("Test Article", _viewModel.Article!.Title);
        Assert.False(_viewModel.IsLoading);
        await _mockFacade.Received(1).GetArticleAsync(articleId);
        _mockFacade.Received(1).CacheArticle(article);
        await _mockFacade.Received(1).SelectArticleInTreeAsync(articleId);
    }

    [Fact]
    public async Task LoadArticleAsync_WhenArticleNotFound_SetsError()
    {
        // Arrange
        var articleId = Guid.NewGuid();
        _mockFacade.GetArticleAsync(articleId).Returns((ArticleDto?)null);

        // Act
        await _viewModel.LoadArticleAsync(articleId);

        // Assert
        Assert.Null(_viewModel.Article);
        Assert.Equal("Article not found", _viewModel.ErrorMessage);
    }

    [Fact]
    public void StartEdit_WithLoadedArticle_EntersEditMode()
    {
        // Arrange
        var articleId = Guid.NewGuid();
        var article = new ArticleDto
        {
            Id = articleId,
            Title = "Test",
            Body = "Body",
            EffectiveDate = DateTime.Now
        };
        _mockFacade.GetArticleAsync(articleId).Returns(article);
        _viewModel.LoadArticleAsync(articleId).Wait();

        // Act
        _viewModel.StartEdit();

        // Assert
        Assert.True(_viewModel.IsEditMode);
        Assert.Equal("Test", _viewModel.EditTitle);
        Assert.Equal("Body", _viewModel.EditBody);
    }

    [Fact]
    public void CancelEdit_ExitsEditMode()
    {
        // Arrange
        var article = new ArticleDto
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            Body = "Body",
            EffectiveDate = DateTime.Now
        };
        _mockFacade.GetArticleAsync(article.Id).Returns(article);
        _viewModel.LoadArticleAsync(article.Id).Wait();
        _viewModel.StartEdit();

        // Act
        _viewModel.CancelEdit();

        // Assert
        Assert.False(_viewModel.IsEditMode);
        Assert.Null(_viewModel.EditTitle);
    }

    [Fact]
    public void UpdateEditTitle_UpdatesTitle()
    {
        // Act
        _viewModel.UpdateEditTitle("New Title");

        // Assert
        Assert.Equal("New Title", _viewModel.EditTitle);
    }

    [Fact]
    public void ToggleMetadataDrawer_TogglesState()
    {
        // Act
        _viewModel.ToggleMetadataDrawer();

        // Assert
        Assert.True(_viewModel.ShowMetadataDrawer);
        _mockFacade.Received(1).ToggleMetadataDrawer();
    }

    [Fact]
    public void OnStateChanged_RaisesWhenStateChanges()
    {
        // Arrange
        var eventRaised = false;
        _viewModel.OnStateChanged += () => eventRaised = true;

        // Act
        _viewModel.UpdateEditTitle("Test");

        // Assert
        Assert.True(eventRaised);
    }

    [Fact]
    public async Task SaveArticleAsync_DelegatesToOperations()
    {
        // Arrange
        var article = new ArticleDto
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            Body = "Body",
            EffectiveDate = DateTime.Now
        };
        _mockFacade.GetArticleAsync(article.Id).Returns(article);
        await _viewModel.LoadArticleAsync(article.Id);
        _viewModel.StartEdit();
        _viewModel.UpdateEditTitle("Updated");
        
        var updatedArticle = new ArticleDto
        {
            Id = article.Id,
            Title = "Updated",
            Body = "Body",
            EffectiveDate = DateTime.Now
        };
        _mockFacade.UpdateArticleAsync(article.Id, Arg.Any<ArticleUpdateDto>())
            .Returns(updatedArticle);

        // Act
        await _viewModel.SaveArticleAsync();

        // Assert
        await _mockFacade.Received(1).UpdateArticleAsync(article.Id, Arg.Any<ArticleUpdateDto>());
    }

    [Fact]
    public async Task DeleteArticleAsync_DelegatesToOperations()
    {
        // Arrange
        var article = new ArticleDto { Id = Guid.NewGuid(), Title = "Test" };
        _mockFacade.GetArticleAsync(article.Id).Returns(article);
        _mockFacade.DeleteArticleAsync(article.Id).Returns(true);
        _mockFacade.GetCurrentWorldId().Returns(Guid.NewGuid());
        
        await _viewModel.LoadArticleAsync(article.Id);

        // Act
        await _viewModel.DeleteArticleAsync();

        // Assert
        await _mockFacade.Received(1).DeleteArticleAsync(article.Id);
    }

    [Fact]
    public async Task CreateChildArticleAsync_DelegatesToOperations()
    {
        // Arrange
        var article = new ArticleDto
        {
            Id = Guid.NewGuid(),
            Title = "Parent",
            WorldId = Guid.NewGuid()
        };
        var childArticle = new ArticleDto
        {
            Id = Guid.NewGuid(),
            Title = "Child",
            Breadcrumbs = new List<BreadcrumbDto> { new() { Slug = "child" } }
        };
        
        _mockFacade.GetArticleAsync(article.Id).Returns(article);
        _mockFacade.CreateArticleAsync(Arg.Any<ArticleCreateDto>()).Returns(childArticle);
        _mockFacade.BuildArticleUrlFromBreadcrumbs(Arg.Any<List<BreadcrumbDto>>())
            .Returns("/article/child");
        
        await _viewModel.LoadArticleAsync(article.Id);

        // Act
        await _viewModel.CreateChildArticleAsync("Child");

        // Assert
        await _mockFacade.Received(1).CreateArticleAsync(Arg.Any<ArticleCreateDto>());
    }
}
