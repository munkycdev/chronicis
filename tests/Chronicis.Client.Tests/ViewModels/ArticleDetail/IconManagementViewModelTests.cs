using Chronicis.Client.Services;
using Chronicis.Client.ViewModels.ArticleDetail;
using Chronicis.Shared.DTOs;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Chronicis.Client.Tests.ViewModels.ArticleDetail;

public class IconManagementViewModelTests
{
    private readonly IArticleDetailFacade _facade;
    private readonly IconManagementViewModel _viewModel;

    public IconManagementViewModelTests()
    {
        _facade = Substitute.For<IArticleDetailFacade>();
        _viewModel = new IconManagementViewModel(_facade);
    }

    [Fact]
    public void Constructor_InitializesWithNullIcon()
    {
        // Assert
        Assert.Null(_viewModel.CurrentIcon);
        Assert.False(_viewModel.IsSaving);
        Assert.Null(_viewModel.ErrorMessage);
    }

    [Fact]
    public async Task UpdateIconAsync_WithValidIcon_UpdatesIconAndNotifies()
    {
        // Arrange
        var articleId = Guid.NewGuid();
        var currentTitle = "Test Article";
        var currentBody = "Test Body";
        var currentDate = DateTime.Now;
        var newIcon = "🎭";
        var stateChanged = false;
        _viewModel.OnStateChanged += () => stateChanged = true;

        var updatedArticle = new ArticleDto { Id = articleId, IconEmoji = newIcon };
        _facade.UpdateArticleAsync(articleId, Arg.Any<ArticleUpdateDto>()).Returns(updatedArticle);

        // Act
        await _viewModel.UpdateIconAsync(articleId, currentTitle, currentBody, currentDate, newIcon);

        // Assert
        Assert.Equal(newIcon, _viewModel.CurrentIcon);
        Assert.False(_viewModel.IsSaving);
        Assert.Null(_viewModel.ErrorMessage);
        Assert.True(stateChanged);
    }

    [Fact]
    public async Task UpdateIconAsync_WithNullIcon_ClearsIcon()
    {
        // Arrange
        var articleId = Guid.NewGuid();
        var currentTitle = "Test";
        var currentBody = "Body";
        var currentDate = DateTime.Now;
        var updatedArticle = new ArticleDto { Id = articleId, IconEmoji = null };
        _facade.UpdateArticleAsync(articleId, Arg.Any<ArticleUpdateDto>()).Returns(updatedArticle);

        // Act
        await _viewModel.UpdateIconAsync(articleId, currentTitle, currentBody, currentDate, null);

        // Assert
        Assert.Null(_viewModel.CurrentIcon);
        await _facade.Received(1).UpdateArticleAsync(articleId, Arg.Any<ArticleUpdateDto>());
    }

    [Fact]
    public async Task UpdateIconAsync_SetsIsSavingDuringOperation()
    {
        // Arrange
        var articleId = Guid.NewGuid();
        var wasSaving = false;
        
        _facade.UpdateArticleAsync(articleId, Arg.Any<ArticleUpdateDto>())
            .Returns(async callInfo =>
            {
                wasSaving = _viewModel.IsSaving;
                await Task.Delay(10);
                return new ArticleDto { Id = articleId };
            });

        // Act
        await _viewModel.UpdateIconAsync(articleId, "Title", "Body", DateTime.Now, "🎭");

        // Assert
        Assert.True(wasSaving, "IsSaving should be true during the operation");
        Assert.False(_viewModel.IsSaving, "IsSaving should be false after completion");
    }

    [Fact]
    public async Task UpdateIconAsync_OnError_SetsErrorMessage()
    {
        // Arrange
        var articleId = Guid.NewGuid();
        var errorMessage = "Network error";
        _facade.UpdateArticleAsync(articleId, Arg.Any<ArticleUpdateDto>())
            .Throws(new Exception(errorMessage));

        // Act
        await _viewModel.UpdateIconAsync(articleId, "Title", "Body", DateTime.Now, "🎭");

        // Assert
        Assert.Equal(errorMessage, _viewModel.ErrorMessage);
        Assert.False(_viewModel.IsSaving);
    }

    [Fact]
    public async Task UpdateIconAsync_CallsFacadeWithCorrectDto()
    {
        // Arrange
        var articleId = Guid.NewGuid();
        var title = "My Article";
        var body = "Article content";
        var effectiveDate = new DateTime(2024, 1, 15);
        var icon = "📝";

        _facade.UpdateArticleAsync(articleId, Arg.Any<ArticleUpdateDto>())
            .Returns(new ArticleDto { Id = articleId });

        // Act
        await _viewModel.UpdateIconAsync(articleId, title, body, effectiveDate, icon);

        // Assert
        await _facade.Received(1).UpdateArticleAsync(articleId, Arg.Is<ArticleUpdateDto>(dto =>
            dto.Title == title &&
            dto.Body == body &&
            dto.EffectiveDate == effectiveDate &&
            dto.IconEmoji == icon
        ));
    }

    [Fact]
    public async Task UpdateIconAsync_RaisesOnIconUpdatedEvent()
    {
        // Arrange
        var articleId = Guid.NewGuid();
        var newIcon = "🎨";
        var eventRaised = false;
        string? receivedIcon = null;

        _viewModel.OnIconUpdated += icon =>
        {
            eventRaised = true;
            receivedIcon = icon;
        };

        _facade.UpdateArticleAsync(articleId, Arg.Any<ArticleUpdateDto>())
            .Returns(new ArticleDto { Id = articleId, IconEmoji = newIcon });

        // Act
        await _viewModel.UpdateIconAsync(articleId, "Title", "Body", DateTime.Now, newIcon);

        // Assert
        Assert.True(eventRaised);
        Assert.Equal(newIcon, receivedIcon);
    }

    [Fact]
    public async Task UpdateIconAsync_OnError_DoesNotRaiseIconUpdatedEvent()
    {
        // Arrange
        var articleId = Guid.NewGuid();
        var eventRaised = false;
        _viewModel.OnIconUpdated += _ => eventRaised = true;
        
        _facade.UpdateArticleAsync(articleId, Arg.Any<ArticleUpdateDto>())
            .Throws(new Exception("Error"));

        // Act
        await _viewModel.UpdateIconAsync(articleId, "Title", "Body", DateTime.Now, "🎭");

        // Assert
        Assert.False(eventRaised);
    }

    [Fact]
    public void ClearError_ResetsErrorMessage()
    {
        // Arrange - use reflection to set the private ErrorMessage property
        var errorProperty = typeof(IconManagementViewModel).GetProperty("ErrorMessage");
        errorProperty!.SetValue(_viewModel, "Some error");

        // Act
        _viewModel.ClearError();

        // Assert
        Assert.Null(_viewModel.ErrorMessage);
    }
}
