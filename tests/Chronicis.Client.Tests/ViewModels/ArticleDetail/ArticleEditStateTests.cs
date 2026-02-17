using Chronicis.Client.ViewModels.ArticleDetail;
using Chronicis.Shared.DTOs;
using Xunit;

namespace Chronicis.Client.Tests.ViewModels.ArticleDetail;

public class ArticleEditStateTests
{
    [Fact]
    public void StartEdit_EntersEditModeWithArticleData()
    {
        // Arrange
        var state = new ArticleEditState();
        var article = new ArticleDto
        {
            Id = Guid.NewGuid(),
            Title = "Original Title",
            Body = "Original Body",
            EffectiveDate = new DateTime(2025, 1, 15, 14, 30, 0)
        };
        var eventRaised = false;
        state.OnStateChanged += () => eventRaised = true;

        // Act
        state.StartEdit(article);

        // Assert
        Assert.True(state.IsEditMode);
        Assert.Equal("Original Title", state.EditTitle);
        Assert.Equal("Original Body", state.EditBody);
        Assert.Equal(new DateTime(2025, 1, 15, 14, 30, 0), state.EditEffectiveDate);
        Assert.Equal(new TimeSpan(14, 30, 0), state.EditEffectiveTime);
        Assert.True(eventRaised);
    }

    [Fact]
    public void CancelEdit_ExitsEditModeAndClearsFields()
    {
        // Arrange
        var state = new ArticleEditState();
        var article = new ArticleDto { Title = "Test", Body = "Body", EffectiveDate = DateTime.Now };
        state.StartEdit(article);

        // Act
        state.CancelEdit();

        // Assert
        Assert.False(state.IsEditMode);
        Assert.Null(state.EditTitle);
        Assert.Null(state.EditBody);
        Assert.Null(state.EditEffectiveDate);
        Assert.Null(state.EditEffectiveTime);
    }

    [Fact]
    public void UpdateTitle_UpdatesTitleAndRaisesEvent()
    {
        // Arrange
        var state = new ArticleEditState();
        var eventRaised = false;
        state.OnStateChanged += () => eventRaised = true;

        // Act
        state.UpdateTitle("New Title");

        // Assert
        Assert.Equal("New Title", state.EditTitle);
        Assert.True(eventRaised);
    }

    [Fact]
    public void UpdateBody_UpdatesBodyAndRaisesEvent()
    {
        // Arrange
        var state = new ArticleEditState();
        var eventRaised = false;
        state.OnStateChanged += () => eventRaised = true;

        // Act
        state.UpdateBody("New Body");

        // Assert
        Assert.Equal("New Body", state.EditBody);
        Assert.True(eventRaised);
    }

    [Fact]
    public void CreateUpdateDto_CombinesEditStateWithArticle()
    {
        // Arrange
        var state = new ArticleEditState();
        var article = new ArticleDto
        {
            Id = Guid.NewGuid(),
            Title = "Original",
            Body = "Original Body",
            EffectiveDate = new DateTime(2025, 1, 15)
        };
        state.StartEdit(article);
        state.UpdateTitle("Updated Title");
        state.UpdateBody("Updated Body");
        state.UpdateEffectiveDate(new DateTime(2025, 2, 1));
        state.UpdateEffectiveTime(new TimeSpan(10, 0, 0));

        // Act
        var updateDto = state.CreateUpdateDto(article);

        // Assert
        Assert.Equal("Updated Title", updateDto.Title);
        Assert.Equal("Updated Body", updateDto.Body);
        Assert.Equal(new DateTime(2025, 2, 1, 10, 0, 0), updateDto.EffectiveDate);
    }

    [Fact]
    public void CreateUpdateDto_UsesFallbackValuesFromArticle()
    {
        // Arrange
        var state = new ArticleEditState();
        var article = new ArticleDto
        {
            Id = Guid.NewGuid(),
            Title = "Original",
            Body = "Original Body",
            EffectiveDate = new DateTime(2025, 1, 15, 12, 0, 0)
        };

        // Act (no edit state set, should use article values)
        var updateDto = state.CreateUpdateDto(article);

        // Assert
        Assert.Equal("Original", updateDto.Title);
        Assert.Equal("Original Body", updateDto.Body);
        Assert.Equal(new DateTime(2025, 1, 15, 12, 0, 0), updateDto.EffectiveDate);
    }
}
