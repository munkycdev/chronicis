using Chronicis.Client.ViewModels.ArticleDetail;
using Chronicis.Shared.DTOs;
using Xunit;

namespace Chronicis.Client.Tests.ViewModels.ArticleDetail;

public class ArticleLoadingStateTests
{
    [Fact]
    public void SetLoading_UpdatesIsLoadingAndRaisesEvent()
    {
        // Arrange
        var state = new ArticleLoadingState();
        var eventRaised = false;
        state.OnStateChanged += () => eventRaised = true;

        // Act
        state.SetLoading(true);

        // Assert
        Assert.True(state.IsLoading);
        Assert.True(eventRaised);
    }

    [Fact]
    public void SetArticle_UpdatesArticleAndRaisesEvent()
    {
        // Arrange
        var state = new ArticleLoadingState();
        var article = new ArticleDto { Id = Guid.NewGuid(), Title = "Test" };
        var eventRaised = false;
        state.OnStateChanged += () => eventRaised = true;

        // Act
        state.SetArticle(article);

        // Assert
        Assert.NotNull(state.Article);
        Assert.Equal("Test", state.Article.Title);
        Assert.True(eventRaised);
    }

    [Fact]
    public void SetError_UpdatesErrorMessageAndRaisesEvent()
    {
        // Arrange
        var state = new ArticleLoadingState();
        var eventRaised = false;
        state.OnStateChanged += () => eventRaised = true;

        // Act
        state.SetError("Test error");

        // Assert
        Assert.Equal("Test error", state.ErrorMessage);
        Assert.True(eventRaised);
    }

    [Fact]
    public void SetSuccess_UpdatesSuccessMessageAndRaisesEvent()
    {
        // Arrange
        var state = new ArticleLoadingState();
        var eventRaised = false;
        state.OnStateChanged += () => eventRaised = true;

        // Act
        state.SetSuccess("Success!");

        // Assert
        Assert.Equal("Success!", state.SuccessMessage);
        Assert.True(eventRaised);
    }

    [Fact]
    public void ClearMessages_ClearsErrorAndSuccess()
    {
        // Arrange
        var state = new ArticleLoadingState();
        state.SetError("Error");
        state.SetSuccess("Success");

        // Act
        state.ClearMessages();

        // Assert
        Assert.Null(state.ErrorMessage);
        Assert.Null(state.SuccessMessage);
    }
}
