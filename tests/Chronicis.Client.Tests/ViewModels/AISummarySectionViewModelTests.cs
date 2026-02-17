using Chronicis.Client.Services;
using Chronicis.Client.ViewModels;
using Chronicis.Shared.DTOs;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.ViewModels;

/// <summary>
/// Tests for AISummarySectionViewModel.
/// Tests all business logic for AI summary generation without UI framework dependencies.
/// </summary>
public class AISummarySectionViewModelTests
{
    private readonly IAISummaryFacade _mockFacade;

    public AISummarySectionViewModelTests()
    {
        _mockFacade = Substitute.For<IAISummaryFacade>();
    }

    #region Initialization Tests

    [Fact]
    public async Task Initialize_LoadsTemplates()
    {
        // Arrange
        var templates = new List<SummaryTemplateDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Default" },
            new() { Id = Guid.NewGuid(), Name = "Detailed" }
        };
        _mockFacade.GetTemplatesAsync().Returns(templates);
        _mockFacade.GetSummaryAsync(Arg.Any<Guid>()).Returns((ArticleSummaryDto?)null);

        var vm = new AISummarySectionViewModel(_mockFacade);

        // Act
        await vm.InitializeAsync(Guid.NewGuid(), "Article");

        // Assert
        Assert.Equal(2, vm.Templates.Count);
        Assert.Equal("Default", vm.Templates[0].Name);
    }

    [Fact]
    public async Task Initialize_SelectsDefaultTemplate_WhenNoneSelected()
    {
        // Arrange
        var defaultTemplateId = Guid.NewGuid();
        var templates = new List<SummaryTemplateDto>
        {
            new() { Id = defaultTemplateId, Name = "Default" },
            new() { Id = Guid.NewGuid(), Name = "Detailed" }
        };
        _mockFacade.GetTemplatesAsync().Returns(templates);
        _mockFacade.GetSummaryAsync(Arg.Any<Guid>()).Returns((ArticleSummaryDto?)null);

        var vm = new AISummarySectionViewModel(_mockFacade);

        // Act
        await vm.InitializeAsync(Guid.NewGuid(), "Article");

        // Assert
        Assert.Equal(defaultTemplateId, vm.SelectedTemplateId);
    }

    [Fact]
    public async Task Initialize_SelectsFirstTemplate_WhenNoDefaultExists()
    {
        // Arrange
        var firstTemplateId = Guid.NewGuid();
        var templates = new List<SummaryTemplateDto>
        {
            new() { Id = firstTemplateId, Name = "Custom" },
            new() { Id = Guid.NewGuid(), Name = "Detailed" }
        };
        _mockFacade.GetTemplatesAsync().Returns(templates);
        _mockFacade.GetSummaryAsync(Arg.Any<Guid>()).Returns((ArticleSummaryDto?)null);

        var vm = new AISummarySectionViewModel(_mockFacade);

        // Act
        await vm.InitializeAsync(Guid.NewGuid(), "Article");

        // Assert
        Assert.Equal(firstTemplateId, vm.SelectedTemplateId);
    }

    [Fact]
    public async Task Initialize_LoadsArticleSummary_WhenEntityTypeIsArticle()
    {
        // Arrange
        var articleId = Guid.NewGuid();
        var summary = new ArticleSummaryDto
        {
            ArticleId = articleId,
            Summary = "Test summary",
            GeneratedAt = DateTime.UtcNow
        };
        _mockFacade.GetTemplatesAsync().Returns(new List<SummaryTemplateDto>());
        _mockFacade.GetSummaryAsync(articleId).Returns(summary);

        var vm = new AISummarySectionViewModel(_mockFacade);

        // Act
        await vm.InitializeAsync(articleId, "Article");

        // Assert
        Assert.True(vm.HasSummary);
        Assert.Equal("Test summary", vm.Summary);
    }

    [Fact]
    public async Task Initialize_LoadsEntitySummary_WhenEntityTypeIsCampaign()
    {
        // Arrange
        var campaignId = Guid.NewGuid();
        var summary = new EntitySummaryDto
        {
            EntityId = campaignId,
            EntityType = "Campaign",
            Summary = "Campaign summary",
            GeneratedAt = DateTime.UtcNow
        };
        _mockFacade.GetTemplatesAsync().Returns(new List<SummaryTemplateDto>());
        _mockFacade.GetEntitySummaryAsync("Campaign", campaignId).Returns(summary);

        var vm = new AISummarySectionViewModel(_mockFacade);

        // Act
        await vm.InitializeAsync(campaignId, "Campaign");

        // Assert
        Assert.True(vm.HasSummary);
        Assert.Equal("Campaign summary", vm.Summary);
    }

    [Fact]
    public async Task Initialize_LoadsExistingSettings_WhenSummaryExists()
    {
        // Arrange
        var articleId = Guid.NewGuid();
        var templateId = Guid.NewGuid();
        var summary = new ArticleSummaryDto
        {
            ArticleId = articleId,
            Summary = "Test",
            TemplateId = templateId,
            CustomPrompt = "Custom instructions"
        };
        _mockFacade.GetTemplatesAsync().Returns(new List<SummaryTemplateDto>());
        _mockFacade.GetSummaryAsync(articleId).Returns(summary);

        var vm = new AISummarySectionViewModel(_mockFacade);

        // Act
        await vm.InitializeAsync(articleId, "Article");

        // Assert
        Assert.Equal(templateId, vm.SelectedTemplateId);
        Assert.Equal("Custom instructions", vm.CustomPrompt);
    }

    #endregion

    #region State Management Tests

    [Fact]
    public void SetSelectedTemplateId_UpdatesProperty()
    {
        // Arrange
        var vm = new AISummarySectionViewModel(_mockFacade);
        var templateId = Guid.NewGuid();

        // Act
        vm.SetSelectedTemplateId(templateId);

        // Assert
        Assert.Equal(templateId, vm.SelectedTemplateId);
    }

    [Fact]
    public void SetCustomPrompt_UpdatesProperty()
    {
        // Arrange
        var vm = new AISummarySectionViewModel(_mockFacade);

        // Act
        vm.SetCustomPrompt("Custom prompt");

        // Assert
        Assert.Equal("Custom prompt", vm.CustomPrompt);
    }

    [Fact]
    public void SetSaveConfiguration_UpdatesProperty()
    {
        // Arrange
        var vm = new AISummarySectionViewModel(_mockFacade);

        // Act
        vm.SetSaveConfiguration(true);

        // Assert
        Assert.True(vm.SaveConfiguration);
    }

    [Fact]
    public void SetAdvancedExpanded_UpdatesProperty()
    {
        // Arrange
        var vm = new AISummarySectionViewModel(_mockFacade);

        // Act
        vm.SetAdvancedExpanded(true);

        // Assert
        Assert.True(vm.AdvancedExpanded);
    }

    [Fact]
    public void StateChanged_RaisedWhenStateUpdates()
    {
        // Arrange
        var vm = new AISummarySectionViewModel(_mockFacade);
        var stateChangedRaised = false;
        vm.StateChanged += () => stateChangedRaised = true;

        // Act
        vm.SetCustomPrompt("Test");

        // Assert
        Assert.True(stateChangedRaised);
    }

    #endregion

    #region Generate Summary Tests

    [Fact]
    public async Task GenerateSummary_ForArticle_CallsCorrectApi()
    {
        // Arrange
        var articleId = Guid.NewGuid();
        var result = new SummaryGenerationDto
        {
            Success = true,
            Summary = "Generated summary",
            GeneratedDate = DateTime.UtcNow
        };
        _mockFacade.GetTemplatesAsync().Returns(new List<SummaryTemplateDto>());
        _mockFacade.GetSummaryAsync(articleId).Returns((ArticleSummaryDto?)null);
        _mockFacade.GenerateSummaryAsync(articleId, Arg.Any<GenerateSummaryRequestDto>()).Returns(result);

        var vm = new AISummarySectionViewModel(_mockFacade);
        await vm.InitializeAsync(articleId, "Article");

        // Act
        await vm.GenerateSummaryAsync();

        // Assert
        await _mockFacade.Received(1).GenerateSummaryAsync(articleId, Arg.Any<GenerateSummaryRequestDto>());
    }

    [Fact]
    public async Task GenerateSummary_ForCampaign_CallsCorrectApi()
    {
        // Arrange
        var campaignId = Guid.NewGuid();
        var result = new SummaryGenerationDto
        {
            Success = true,
            Summary = "Campaign summary",
            GeneratedDate = DateTime.UtcNow
        };
        _mockFacade.GetTemplatesAsync().Returns(new List<SummaryTemplateDto>());
        _mockFacade.GetEntitySummaryAsync("Campaign", campaignId).Returns((EntitySummaryDto?)null);
        _mockFacade.GenerateEntitySummaryAsync("Campaign", campaignId, Arg.Any<GenerateSummaryRequestDto>()).Returns(result);

        var vm = new AISummarySectionViewModel(_mockFacade);
        await vm.InitializeAsync(campaignId, "Campaign");

        // Act
        await vm.GenerateSummaryAsync();

        // Assert
        await _mockFacade.Received(1).GenerateEntitySummaryAsync("Campaign", campaignId, Arg.Any<GenerateSummaryRequestDto>());
    }

    [Fact]
    public async Task GenerateSummary_Success_UpdatesSummaryData()
    {
        // Arrange
        var articleId = Guid.NewGuid();
        var result = new SummaryGenerationDto
        {
            Success = true,
            Summary = "New summary",
            GeneratedDate = DateTime.UtcNow
        };
        _mockFacade.GetTemplatesAsync().Returns(new List<SummaryTemplateDto>());
        _mockFacade.GetSummaryAsync(articleId).Returns((ArticleSummaryDto?)null);
        _mockFacade.GenerateSummaryAsync(articleId, Arg.Any<GenerateSummaryRequestDto>()).Returns(result);

        var vm = new AISummarySectionViewModel(_mockFacade);
        await vm.InitializeAsync(articleId, "Article");

        // Act
        await vm.GenerateSummaryAsync();

        // Assert
        Assert.True(vm.HasSummary);
        Assert.Equal("New summary", vm.Summary);
    }

    [Fact]
    public async Task GenerateSummary_Success_ShowsSuccessMessage()
    {
        // Arrange
        var articleId = Guid.NewGuid();
        var result = new SummaryGenerationDto
        {
            Success = true,
            Summary = "Summary",
            GeneratedDate = DateTime.UtcNow
        };
        _mockFacade.GetTemplatesAsync().Returns(new List<SummaryTemplateDto>());
        _mockFacade.GetSummaryAsync(articleId).Returns((ArticleSummaryDto?)null);
        _mockFacade.GenerateSummaryAsync(articleId, Arg.Any<GenerateSummaryRequestDto>()).Returns(result);

        var vm = new AISummarySectionViewModel(_mockFacade);
        await vm.InitializeAsync(articleId, "Article");

        // Act
        await vm.GenerateSummaryAsync();

        // Assert
        _mockFacade.Received(1).ShowSuccess("Summary generated!");
    }

    [Fact]
    public async Task GenerateSummary_Failure_ShowsErrorMessage()
    {
        // Arrange
        var articleId = Guid.NewGuid();
        var result = new SummaryGenerationDto
        {
            Success = false,
            ErrorMessage = "Generation failed"
        };
        _mockFacade.GetTemplatesAsync().Returns(new List<SummaryTemplateDto>());
        _mockFacade.GetSummaryAsync(articleId).Returns((ArticleSummaryDto?)null);
        _mockFacade.GenerateSummaryAsync(articleId, Arg.Any<GenerateSummaryRequestDto>()).Returns(result);

        var vm = new AISummarySectionViewModel(_mockFacade);
        await vm.InitializeAsync(articleId, "Article");

        // Act
        await vm.GenerateSummaryAsync();

        // Assert
        _mockFacade.Received(1).ShowError("Generation failed");
        Assert.Equal("Generation failed", vm.ErrorMessage);
    }

    [Fact]
    public async Task GenerateSummary_IncludesCustomPrompt_WhenProvided()
    {
        // Arrange
        var articleId = Guid.NewGuid();
        var result = new SummaryGenerationDto { Success = true, Summary = "Summary", GeneratedDate = DateTime.UtcNow };
        _mockFacade.GetTemplatesAsync().Returns(new List<SummaryTemplateDto>());
        _mockFacade.GetSummaryAsync(articleId).Returns((ArticleSummaryDto?)null);
        _mockFacade.GenerateSummaryAsync(articleId, Arg.Any<GenerateSummaryRequestDto>()).Returns(result);

        var vm = new AISummarySectionViewModel(_mockFacade);
        await vm.InitializeAsync(articleId, "Article");
        vm.SetCustomPrompt("Focus on characters");

        // Act
        await vm.GenerateSummaryAsync();

        // Assert
        await _mockFacade.Received(1).GenerateSummaryAsync(
            articleId,
            Arg.Is<GenerateSummaryRequestDto>(r => r.CustomPrompt == "Focus on characters")
        );
    }

    [Fact]
    public async Task GenerateSummary_IncludesSaveConfiguration_WhenEnabled()
    {
        // Arrange
        var articleId = Guid.NewGuid();
        var result = new SummaryGenerationDto { Success = true, Summary = "Summary", GeneratedDate = DateTime.UtcNow };
        _mockFacade.GetTemplatesAsync().Returns(new List<SummaryTemplateDto>());
        _mockFacade.GetSummaryAsync(articleId).Returns((ArticleSummaryDto?)null);
        _mockFacade.GenerateSummaryAsync(articleId, Arg.Any<GenerateSummaryRequestDto>()).Returns(result);

        var vm = new AISummarySectionViewModel(_mockFacade);
        await vm.InitializeAsync(articleId, "Article");
        vm.SetSaveConfiguration(true);

        // Act
        await vm.GenerateSummaryAsync();

        // Assert
        await _mockFacade.Received(1).GenerateSummaryAsync(
            articleId,
            Arg.Is<GenerateSummaryRequestDto>(r => r.SaveConfiguration == true)
        );
    }

    #endregion

    #region Clear Summary Tests

    [Fact]
    public async Task ClearSummary_ForArticle_CallsCorrectApi()
    {
        // Arrange
        var articleId = Guid.NewGuid();
        _mockFacade.GetTemplatesAsync().Returns(new List<SummaryTemplateDto>());
        _mockFacade.GetSummaryAsync(articleId).Returns((ArticleSummaryDto?)null);
        _mockFacade.ClearSummaryAsync(articleId).Returns(true);

        var vm = new AISummarySectionViewModel(_mockFacade);
        await vm.InitializeAsync(articleId, "Article");

        // Act
        await vm.ClearSummaryAsync();

        // Assert
        await _mockFacade.Received(1).ClearSummaryAsync(articleId);
    }

    [Fact]
    public async Task ClearSummary_ForCampaign_CallsCorrectApi()
    {
        // Arrange
        var campaignId = Guid.NewGuid();
        _mockFacade.GetTemplatesAsync().Returns(new List<SummaryTemplateDto>());
        _mockFacade.GetEntitySummaryAsync("Campaign", campaignId).Returns((EntitySummaryDto?)null);
        _mockFacade.ClearEntitySummaryAsync("Campaign", campaignId).Returns(true);

        var vm = new AISummarySectionViewModel(_mockFacade);
        await vm.InitializeAsync(campaignId, "Campaign");

        // Act
        await vm.ClearSummaryAsync();

        // Assert
        await _mockFacade.Received(1).ClearEntitySummaryAsync("Campaign", campaignId);
    }

    [Fact]
    public async Task ClearSummary_Success_ShowsInfoMessage()
    {
        // Arrange
        var articleId = Guid.NewGuid();
        _mockFacade.GetTemplatesAsync().Returns(new List<SummaryTemplateDto>());
        _mockFacade.GetSummaryAsync(articleId).Returns((ArticleSummaryDto?)null);
        _mockFacade.ClearSummaryAsync(articleId).Returns(true);

        var vm = new AISummarySectionViewModel(_mockFacade);
        await vm.InitializeAsync(articleId, "Article");

        // Act
        await vm.ClearSummaryAsync();

        // Assert
        _mockFacade.Received(1).ShowInfo("Summary cleared");
    }

    [Fact]
    public async Task ClearSummary_Failure_ShowsErrorMessage()
    {
        // Arrange
        var articleId = Guid.NewGuid();
        _mockFacade.GetTemplatesAsync().Returns(new List<SummaryTemplateDto>());
        _mockFacade.GetSummaryAsync(articleId).Returns((ArticleSummaryDto?)null);
        _mockFacade.ClearSummaryAsync(articleId).Returns(false);

        var vm = new AISummarySectionViewModel(_mockFacade);
        await vm.InitializeAsync(articleId, "Article");

        // Act
        await vm.ClearSummaryAsync();

        // Assert
        _mockFacade.Received(1).ShowError("Failed to clear summary");
    }

    #endregion

    #region Copy Summary Tests

    [Fact]
    public async Task CopySummary_CallsClipboardApi()
    {
        // Arrange
        var articleId = Guid.NewGuid();
        var summary = new ArticleSummaryDto
        {
            ArticleId = articleId,
            Summary = "Summary to copy"
        };
        _mockFacade.GetTemplatesAsync().Returns(new List<SummaryTemplateDto>());
        _mockFacade.GetSummaryAsync(articleId).Returns(summary);

        var vm = new AISummarySectionViewModel(_mockFacade);
        await vm.InitializeAsync(articleId, "Article");

        // Act
        await vm.CopySummaryAsync();

        // Assert
        await _mockFacade.Received(1).CopyToClipboardAsync("Summary to copy");
    }

    [Fact]
    public async Task CopySummary_ShowsSuccessMessage()
    {
        // Arrange
        var articleId = Guid.NewGuid();
        var summary = new ArticleSummaryDto
        {
            ArticleId = articleId,
            Summary = "Summary"
        };
        _mockFacade.GetTemplatesAsync().Returns(new List<SummaryTemplateDto>());
        _mockFacade.GetSummaryAsync(articleId).Returns(summary);

        var vm = new AISummarySectionViewModel(_mockFacade);
        await vm.InitializeAsync(articleId, "Article");

        // Act
        await vm.CopySummaryAsync();

        // Assert
        _mockFacade.Received(1).ShowSuccess("Copied to clipboard");
    }

    #endregion

    #region Toggle Expanded Tests

    [Fact]
    public async Task ToggleExpanded_ChangesIsExpandedState()
    {
        // Arrange
        var vm = new AISummarySectionViewModel(_mockFacade);
        _mockFacade.GetTemplatesAsync().Returns(new List<SummaryTemplateDto>());
        _mockFacade.GetSummaryAsync(Arg.Any<Guid>()).Returns((ArticleSummaryDto?)null);
        await vm.InitializeAsync(Guid.NewGuid(), "Article", false);

        Assert.False(vm.IsExpanded);

        // Act
        await vm.ToggleExpandedAsync();

        // Assert
        Assert.True(vm.IsExpanded);
    }

    #endregion

    #region Helper Method Tests

    [Fact]
    public void GetRelativeTime_ReturnsJustNow_ForRecentTime()
    {
        // Arrange
        var vm = new AISummarySectionViewModel(_mockFacade);
        var date = DateTime.UtcNow.AddSeconds(-30);

        // Act
        var result = vm.GetRelativeTime(date);

        // Assert
        Assert.Equal("just now", result);
    }

    [Fact]
    public void GetRelativeTime_ReturnsMinutesAgo_ForMinutes()
    {
        // Arrange
        var vm = new AISummarySectionViewModel(_mockFacade);
        var date = DateTime.UtcNow.AddMinutes(-5);

        // Act
        var result = vm.GetRelativeTime(date);

        // Assert
        Assert.Equal("5m ago", result);
    }

    [Fact]
    public void GetRelativeTime_ReturnsHoursAgo_ForHours()
    {
        // Arrange
        var vm = new AISummarySectionViewModel(_mockFacade);
        var date = DateTime.UtcNow.AddHours(-3);

        // Act
        var result = vm.GetRelativeTime(date);

        // Assert
        Assert.Equal("3h ago", result);
    }

    [Fact]
    public void GetRelativeTime_ReturnsDaysAgo_ForDays()
    {
        // Arrange
        var vm = new AISummarySectionViewModel(_mockFacade);
        var date = DateTime.UtcNow.AddDays(-2);

        // Act
        var result = vm.GetRelativeTime(date);

        // Assert
        Assert.Equal("2d ago", result);
    }

    #endregion

    #region Entity Type Specific Tests

    [Fact]
    public void NoSourcesMessage_ReturnsArticleMessage_WhenEntityTypeIsArticle()
    {
        // Arrange
        var vm = new AISummarySectionViewModel(_mockFacade);

        // Act - Initialize will set entity type
        var task = vm.InitializeAsync(Guid.NewGuid(), "Article");

        // Assert
        Assert.Contains("wiki links", vm.NoSourcesMessage);
    }

    [Fact]
    public void NoSourcesMessage_ReturnsCampaignMessage_WhenEntityTypeIsCampaign()
    {
        // Arrange
        var vm = new AISummarySectionViewModel(_mockFacade);

        // Act
        var task = vm.InitializeAsync(Guid.NewGuid(), "Campaign");

        // Assert
        Assert.Contains("session notes", vm.NoSourcesMessage);
    }

    [Fact]
    public void SourceLabel_ReturnsSource_ForArticle()
    {
        // Arrange
        var vm = new AISummarySectionViewModel(_mockFacade);
        var task = vm.InitializeAsync(Guid.NewGuid(), "Article");

        // Assert
        Assert.Equal("source", vm.SourceLabel);
    }

    [Fact]
    public void SourceLabel_ReturnsSession_ForCampaign()
    {
        // Arrange
        var vm = new AISummarySectionViewModel(_mockFacade);
        var task = vm.InitializeAsync(Guid.NewGuid(), "Campaign");

        // Assert
        Assert.Equal("session", vm.SourceLabel);
    }

    #endregion
}
