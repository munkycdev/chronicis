using Blazored.LocalStorage;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using MudBlazor;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Services;

/// <summary>
/// Tests for ArticleDetailFacade - wraps 18 services to simplify ArticleDetail component.
/// Following TDD: Write test first (RED), implement minimal code (GREEN), refactor.
/// 
/// Services wrapped (18 total):
/// 1. IArticleApiService - Article CRUD
/// 2. ILinkApiService - Backlinks, outgoing links
/// 3. IExternalLinkApiService - External link content
/// 4. ITreeStateService - Tree state management
/// 5. IAppContextService - Current world/campaign
/// 6. IBreadcrumbService - Breadcrumb generation
/// 7. ISnackbar - User notifications
/// 8. IJSRuntime - JavaScript interop
/// 9. NavigationManager - Navigation
/// 10. ILocalStorageService - Local storage
/// 11. IArticleCacheService - Article caching
/// 12. IMetadataDrawerService - Metadata drawer state
/// 13. IKeyboardShortcutService - Keyboard shortcuts
/// 14. ILogger - Logging
/// 15. IWikiLinkService - Wiki link operations
/// 16. IAISummaryApiService - AI summaries
/// 17. IMarkdownService - Markdown processing
/// 18. IWorldApiService - World/document operations
/// </summary>
public class ArticleDetailFacadeTests
{
    private readonly IArticleApiService _mockArticleApi;
    private readonly ILinkApiService _mockLinkApi;
    private readonly IExternalLinkApiService _mockExternalLinkApi;
    private readonly IWikiLinkService _mockWikiLinkService;
    private readonly IMarkdownService _mockMarkdownService;
    private readonly NavigationManager _mockNavigationManager;
    private readonly IBreadcrumbService _mockBreadcrumbService;
    private readonly ITreeStateService _mockTreeState;
    private readonly IAppContextService _mockAppContext;
    private readonly ISnackbar _mockSnackbar;
    private readonly IMetadataDrawerService _mockMetadataDrawerService;
    private readonly IKeyboardShortcutService _mockKeyboardShortcutService;
    private readonly IArticleCacheService _mockArticleCache;
    private readonly ILocalStorageService _mockLocalStorage;
    private readonly IJSRuntime _mockJSRuntime;
    private readonly ILogger<ArticleDetailFacade> _mockLogger;
    private readonly ArticleDetailFacade _facade;

    public ArticleDetailFacadeTests()
    {
        _mockArticleApi = Substitute.For<IArticleApiService>();
        _mockLinkApi = Substitute.For<ILinkApiService>();
        _mockExternalLinkApi = Substitute.For<IExternalLinkApiService>();
        _mockWikiLinkService = Substitute.For<IWikiLinkService>();
        _mockMarkdownService = Substitute.For<IMarkdownService>();
        _mockNavigationManager = Substitute.For<NavigationManager>();
        _mockBreadcrumbService = Substitute.For<IBreadcrumbService>();
        _mockTreeState = Substitute.For<ITreeStateService>();
        _mockAppContext = Substitute.For<IAppContextService>();
        _mockSnackbar = Substitute.For<ISnackbar>();
        _mockMetadataDrawerService = Substitute.For<IMetadataDrawerService>();
        _mockKeyboardShortcutService = Substitute.For<IKeyboardShortcutService>();
        _mockArticleCache = Substitute.For<IArticleCacheService>();
        _mockLocalStorage = Substitute.For<ILocalStorageService>();
        _mockJSRuntime = Substitute.For<IJSRuntime>();
        _mockLogger = Substitute.For<ILogger<ArticleDetailFacade>>();
        
        _facade = new ArticleDetailFacade(
            _mockArticleApi, 
            _mockLinkApi, 
            _mockExternalLinkApi,
            _mockWikiLinkService,
            _mockMarkdownService,
            _mockNavigationManager,
            _mockBreadcrumbService,
            _mockTreeState,
            _mockAppContext,
            _mockSnackbar,
            _mockMetadataDrawerService,
            _mockKeyboardShortcutService,
            _mockArticleCache,
            _mockLocalStorage,
            _mockJSRuntime,
            _mockLogger);
    }

    #region GetArticleAsync Tests

    [Fact]
    public async Task GetArticleAsync_WhenArticleExists_ReturnsArticle()
    {
        // Arrange
        var articleId = Guid.NewGuid();
        var expectedArticle = new ArticleDto
        {
            Id = articleId,
            Title = "Test Article",
            Body = "Test content"
        };
        _mockArticleApi.GetArticleAsync(articleId).Returns(expectedArticle);

        // Act
        var result = await _facade.GetArticleAsync(articleId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(articleId, result.Id);
        Assert.Equal("Test Article", result.Title);
        await _mockArticleApi.Received(1).GetArticleAsync(articleId);
    }

    [Fact]
    public async Task GetArticleAsync_WhenArticleNotFound_ReturnsNull()
    {
        // Arrange
        var articleId = Guid.NewGuid();
        _mockArticleApi.GetArticleAsync(articleId).Returns((ArticleDto?)null);

        // Act
        var result = await _facade.GetArticleAsync(articleId);

        // Assert
        Assert.Null(result);
        await _mockArticleApi.Received(1).GetArticleAsync(articleId);
    }

    #endregion

    #region UpdateArticleAsync Tests

    [Fact]
    public async Task UpdateArticleAsync_WithValidData_ReturnsUpdatedArticle()
    {
        // Arrange
        var articleId = Guid.NewGuid();
        var updateDto = new ArticleUpdateDto
        {
            Title = "Updated Title",
            Body = "Updated body",
            EffectiveDate = DateTime.Now
        };
        var expectedArticle = new ArticleDto
        {
            Id = articleId,
            Title = "Updated Title",
            Body = "Updated body"
        };
        _mockArticleApi.UpdateArticleAsync(articleId, updateDto).Returns(expectedArticle);

        // Act
        var result = await _facade.UpdateArticleAsync(articleId, updateDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Title", result.Title);
        await _mockArticleApi.Received(1).UpdateArticleAsync(articleId, updateDto);
    }

    #endregion

    #region DeleteArticleAsync Tests

    [Fact]
    public async Task DeleteArticleAsync_WhenSuccessful_ReturnsTrue()
    {
        // Arrange
        var articleId = Guid.NewGuid();
        _mockArticleApi.DeleteArticleAsync(articleId).Returns(true);

        // Act
        var result = await _facade.DeleteArticleAsync(articleId);

        // Assert
        Assert.True(result);
        await _mockArticleApi.Received(1).DeleteArticleAsync(articleId);
    }

    [Fact]
    public async Task DeleteArticleAsync_WhenFailed_ReturnsFalse()
    {
        // Arrange
        var articleId = Guid.NewGuid();
        _mockArticleApi.DeleteArticleAsync(articleId).Returns(false);

        // Act
        var result = await _facade.DeleteArticleAsync(articleId);

        // Assert
        Assert.False(result);
        await _mockArticleApi.Received(1).DeleteArticleAsync(articleId);
    }

    #endregion

    #region CreateArticleAsync Tests

    [Fact]
    public async Task CreateArticleAsync_WithValidData_ReturnsCreatedArticle()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var createDto = new ArticleCreateDto
        {
            Title = "New Article",
            Body = string.Empty,
            WorldId = worldId,
            EffectiveDate = DateTime.Now
        };
        var expectedArticle = new ArticleDto
        {
            Id = Guid.NewGuid(),
            Title = "New Article",
            WorldId = worldId
        };
        _mockArticleApi.CreateArticleAsync(createDto).Returns(expectedArticle);

        // Act
        var result = await _facade.CreateArticleAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Article", result.Title);
        await _mockArticleApi.Received(1).CreateArticleAsync(createDto);
    }

    #endregion

    #region GetBacklinksAsync Tests

    [Fact]
    public async Task GetBacklinksAsync_ReturnsBacklinks()
    {
        // Arrange
        var articleId = Guid.NewGuid();
        var expectedBacklinks = new List<BacklinkDto>
        {
            new() { ArticleId = Guid.NewGuid(), Title = "Backlink 1" },
            new() { ArticleId = Guid.NewGuid(), Title = "Backlink 2" }
        };
        _mockLinkApi.GetBacklinksAsync(articleId).Returns(expectedBacklinks);

        // Act
        var result = await _facade.GetBacklinksAsync(articleId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("Backlink 1", result[0].Title);
        await _mockLinkApi.Received(1).GetBacklinksAsync(articleId);
    }

    #endregion

    #region GetOutgoingLinksAsync Tests

    [Fact]
    public async Task GetOutgoingLinksAsync_ReturnsOutgoingLinks()
    {
        // Arrange
        var articleId = Guid.NewGuid();
        var expectedLinks = new List<BacklinkDto>
        {
            new() { ArticleId = Guid.NewGuid(), Title = "Link 1" },
            new() { ArticleId = Guid.NewGuid(), Title = "Link 2" }
        };
        _mockLinkApi.GetOutgoingLinksAsync(articleId).Returns(expectedLinks);

        // Act
        var result = await _facade.GetOutgoingLinksAsync(articleId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("Link 1", result[0].Title);
        await _mockLinkApi.Received(1).GetOutgoingLinksAsync(articleId);
    }

    #endregion

    #region AutoLinkAsync Tests

    [Fact]
    public async Task AutoLinkAsync_ReturnsAutoLinkResult()
    {
        // Arrange
        var articleId = Guid.NewGuid();
        var markdown = "This is some #WikiLink content";
        var expectedResult = new AutoLinkResponseDto
        {
            LinksFound = 1,
            Matches = new List<AutoLinkMatchDto>
            {
                new() { ArticleId = Guid.NewGuid(), MatchedText = "WikiLink", ArticleTitle = "Wiki Link" }
            }
        };
        _mockLinkApi.AutoLinkAsync(articleId, markdown).Returns(expectedResult);

        // Act
        var result = await _facade.AutoLinkAsync(articleId, markdown);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.LinksFound);
        await _mockLinkApi.Received(1).AutoLinkAsync(articleId, markdown);
    }

    #endregion

    #region GetExternalLinkContentAsync Tests

    [Fact]
    public async Task GetExternalLinkContentAsync_WithValidSourceAndId_ReturnsContent()
    {
        // Arrange
        var source = "open5e";
        var id = "aboleth";
        var expectedContent = new ExternalLinkContentDto
        {
            Markdown = "# Aboleth\n\nA terrifying creature...",
            Source = source,
            Kind = "Monster"
        };
        _mockExternalLinkApi.GetContentAsync(source, id, Arg.Any<CancellationToken>())
            .Returns(expectedContent);

        // Act
        var result = await _facade.GetExternalLinkContentAsync(source, id, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("# Aboleth\n\nA terrifying creature...", result.Markdown);
        Assert.Equal(source, result.Source);
        await _mockExternalLinkApi.Received(1).GetContentAsync(source, id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetExternalLinkContentAsync_WhenContentNotFound_ReturnsNull()
    {
        // Arrange
        var source = "open5e";
        var id = "nonexistent";
        _mockExternalLinkApi.GetContentAsync(source, id, Arg.Any<CancellationToken>())
            .Returns((ExternalLinkContentDto?)null);

        // Act
        var result = await _facade.GetExternalLinkContentAsync(source, id, CancellationToken.None);

        // Assert
        Assert.Null(result);
        await _mockExternalLinkApi.Received(1).GetContentAsync(source, id, Arg.Any<CancellationToken>());
    }

    #endregion

    #region GetExternalLinkSuggestionsAsync Tests

    [Fact]
    public async Task GetExternalLinkSuggestionsAsync_WithQuery_ReturnsSuggestions()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        var source = "open5e";
        var query = "dragon";
        var expectedSuggestions = new List<ExternalLinkSuggestionDto>
        {
            new() { Source = source, Id = "red-dragon", Title = "Red Dragon" },
            new() { Source = source, Id = "black-dragon", Title = "Black Dragon" }
        };
        _mockExternalLinkApi.GetSuggestionsAsync(worldId, source, query, Arg.Any<CancellationToken>())
            .Returns(expectedSuggestions);

        // Act
        var result = await _facade.GetExternalLinkSuggestionsAsync(worldId, source, query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("Red Dragon", result[0].Title);
        await _mockExternalLinkApi.Received(1).GetSuggestionsAsync(worldId, source, query, Arg.Any<CancellationToken>());
    }

    #endregion

    #region CreateArticleFromWikiLinkAsync Tests

    [Fact]
    public async Task CreateArticleFromWikiLinkAsync_WithValidName_CreatesArticle()
    {
        // Arrange
        var articleName = "New Wiki Article";
        var worldId = Guid.NewGuid();
        var expectedArticle = new ArticleDto
        {
            Id = Guid.NewGuid(),
            Title = articleName,
            WorldId = worldId
        };
        _mockWikiLinkService.CreateArticleFromAutocompleteAsync(articleName, worldId)
            .Returns(expectedArticle);

        // Act
        var result = await _facade.CreateArticleFromWikiLinkAsync(articleName, worldId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(articleName, result.Title);
        Assert.Equal(worldId, result.WorldId);
        await _mockWikiLinkService.Received(1).CreateArticleFromAutocompleteAsync(articleName, worldId);
    }

    [Fact]
    public async Task CreateArticleFromWikiLinkAsync_WhenCreationFails_ReturnsNull()
    {
        // Arrange
        var articleName = "Failed Article";
        var worldId = Guid.NewGuid();
        _mockWikiLinkService.CreateArticleFromAutocompleteAsync(articleName, worldId)
            .Returns((ArticleDto?)null);

        // Act
        var result = await _facade.CreateArticleFromWikiLinkAsync(articleName, worldId);

        // Assert
        Assert.Null(result);
        await _mockWikiLinkService.Received(1).CreateArticleFromAutocompleteAsync(articleName, worldId);
    }

    #endregion

    #region RenderMarkdownToHtml Tests

    [Fact]
    public void RenderMarkdownToHtml_WithMarkdown_ReturnsHtml()
    {
        // Arrange
        var markdown = "# Header\n\nSome **bold** text";
        var expectedHtml = "<h1>Header</h1>\n<p>Some <strong>bold</strong> text</p>";
        _mockMarkdownService.ToHtml(markdown).Returns(expectedHtml);

        // Act
        var result = _facade.RenderMarkdownToHtml(markdown);

        // Assert
        Assert.Equal(expectedHtml, result);
        _mockMarkdownService.Received(1).ToHtml(markdown);
    }

    [Fact]
    public void RenderMarkdownToHtml_WithEmptyString_ReturnsEmpty()
    {
        // Arrange
        var markdown = string.Empty;
        _mockMarkdownService.ToHtml(markdown).Returns(string.Empty);

        // Act
        var result = _facade.RenderMarkdownToHtml(markdown);

        // Assert
        Assert.Empty(result);
        _mockMarkdownService.Received(1).ToHtml(markdown);
    }

    #endregion

    // Note: NavigateToArticle() is not unit tested because NavigationManager is a framework type
    // that requires Blazor runtime initialization. Navigation is tested in component/integration tests.

    #region BuildArticleUrlFromBreadcrumbs Tests

    [Fact]
    public void BuildArticleUrlFromBreadcrumbs_ReturnsBuildUrl()
    {
        // Arrange
        var breadcrumbs = new List<BreadcrumbDto>
        {
            new() { Slug = "test-world" },
            new() { Slug = "test-article" }
        };
        var expectedUrl = "/article/test-world/test-article";
        _mockBreadcrumbService.BuildArticleUrl(breadcrumbs).Returns(expectedUrl);

        // Act
        var result = _facade.BuildArticleUrlFromBreadcrumbs(breadcrumbs);

        // Assert
        Assert.Equal(expectedUrl, result);
        _mockBreadcrumbService.Received(1).BuildArticleUrl(breadcrumbs);
    }

    #endregion

    #region SelectArticleInTreeAsync Tests

    [Fact]
    public async Task SelectArticleInTreeAsync_CallsTreeState()
    {
        // Arrange
        var articleId = Guid.NewGuid();

        // Act
        await _facade.SelectArticleInTreeAsync(articleId);

        // Assert
        _mockTreeState.Received(1).SelectNode(articleId);
    }

    #endregion

    #region RefreshTreeAsync Tests

    [Fact]
    public async Task RefreshTreeAsync_CallsTreeStateRefresh()
    {
        // Arrange & Act
        await _facade.RefreshTreeAsync();

        // Assert
        await _mockTreeState.Received(1).RefreshAsync();
    }

    #endregion

    #region GetCurrentWorldId Tests

    [Fact]
    public void GetCurrentWorldId_ReturnsCurrentWorldId()
    {
        // Arrange
        var worldId = Guid.NewGuid();
        _mockAppContext.CurrentWorldId.Returns(worldId);

        // Act
        var result = _facade.GetCurrentWorldId();

        // Assert
        Assert.Equal(worldId, result);
    }

    [Fact]
    public void GetCurrentWorldId_WhenNoWorld_ReturnsNull()
    {
        // Arrange
        _mockAppContext.CurrentWorldId.Returns((Guid?)null);

        // Act
        var result = _facade.GetCurrentWorldId();

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region ShowSuccessNotification Tests

    [Fact]
    public void ShowSuccessNotification_AddsSuccessSnackbar()
    {
        // Arrange
        var message = "Operation successful";

        // Act
        _facade.ShowSuccessNotification(message);

        // Assert
        _mockSnackbar.Received(1).Add(message, Severity.Success);
    }

    #endregion

    #region ShowErrorNotification Tests

    [Fact]
    public void ShowErrorNotification_AddsErrorSnackbar()
    {
        // Arrange
        var message = "Operation failed";

        // Act
        _facade.ShowErrorNotification(message);

        // Assert
        _mockSnackbar.Received(1).Add(message, Severity.Error);
    }

    #endregion

    #region ToggleMetadataDrawer Tests

    [Fact]
    public void ToggleMetadataDrawer_CallsMetadataDrawerServiceToggle()
    {
        // Act
        _facade.ToggleMetadataDrawer();

        // Assert
        _mockMetadataDrawerService.Received(1).Toggle();
    }

    #endregion

    #region TriggerSaveShortcut Tests

    [Fact]
    public void TriggerSaveShortcut_CallsKeyboardShortcutServiceRequestSave()
    {
        // Act
        _facade.TriggerSaveShortcut();

        // Assert
        _mockKeyboardShortcutService.Received(1).RequestSave();
    }

    #endregion

    #region GetArticleNavigationPathAsync Tests

    [Fact]
    public async Task GetArticleNavigationPathAsync_ReturnsPath()
    {
        // Arrange
        var articleId = Guid.NewGuid();
        var expectedPath = "/article/test-world/test-article";
        _mockArticleCache.GetNavigationPathAsync(articleId).Returns(expectedPath);

        // Act
        var result = await _facade.GetArticleNavigationPathAsync(articleId);

        // Assert
        Assert.Equal(expectedPath, result);
        await _mockArticleCache.Received(1).GetNavigationPathAsync(articleId);
    }

    #endregion

    #region CacheArticle Tests

    [Fact]
    public void CacheArticle_CachesArticle()
    {
        // Arrange
        var article = new ArticleDto
        {
            Id = Guid.NewGuid(),
            Title = "Test Article"
        };

        // Act
        _facade.CacheArticle(article);

        // Assert
        _mockArticleCache.Received(1).CacheArticle(article);
    }

    #endregion

    #region GetFromLocalStorageAsync Tests

    [Fact]
    public async Task GetFromLocalStorageAsync_ReturnsValue()
    {
        // Arrange
        var key = "test-key";
        var expectedValue = "test-value";
        _mockLocalStorage.GetItemAsync<string>(key).Returns(expectedValue);

        // Act
        var result = await _facade.GetFromLocalStorageAsync<string>(key);

        // Assert
        Assert.Equal(expectedValue, result);
        await _mockLocalStorage.Received(1).GetItemAsync<string>(key);
    }

    [Fact]
    public async Task GetFromLocalStorageAsync_WhenKeyNotFound_ReturnsNull()
    {
        // Arrange
        var key = "missing-key";
        _mockLocalStorage.GetItemAsync<string>(key).Returns((string?)null);

        // Act
        var result = await _facade.GetFromLocalStorageAsync<string>(key);

        // Assert
        Assert.Null(result);
        await _mockLocalStorage.Received(1).GetItemAsync<string>(key);
    }

    #endregion

    #region SetInLocalStorageAsync Tests

    [Fact]
    public async Task SetInLocalStorageAsync_SavesValue()
    {
        // Arrange
        var key = "test-key";
        var value = "test-value";

        // Act
        await _facade.SetInLocalStorageAsync(key, value);

        // Assert
        await _mockLocalStorage.Received(1).SetItemAsync(key, value);
    }

    #endregion

    #region ImportJavaScriptModuleAsync Tests

    [Fact]
    public async Task ImportJavaScriptModuleAsync_ReturnsModule()
    {
        // Arrange
        var modulePath = "./scripts/tiptap.js";
        var mockModule = Substitute.For<IJSObjectReference>();
        _mockJSRuntime.InvokeAsync<IJSObjectReference>("import", Arg.Any<object[]>())
            .Returns(mockModule);

        // Act
        var result = await _facade.ImportJavaScriptModuleAsync(modulePath);

        // Assert
        Assert.NotNull(result);
        await _mockJSRuntime.Received(1).InvokeAsync<IJSObjectReference>("import", Arg.Any<object[]>());
    }

    #endregion

    #region LogInformation Tests

    [Fact]
    public void LogInformation_LogsMessage()
    {
        // Arrange
        var message = "Test information message";

        // Act
        _facade.LogInformation(message);

        // Assert
        _mockLogger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(v => v.ToString()!.Contains(message)),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    #endregion

    #region LogError Tests

    [Fact]
    public void LogError_LogsExceptionAndMessage()
    {
        // Arrange
        var exception = new Exception("Test exception");
        var message = "Test error message";

        // Act
        _facade.LogError(exception, message);

        // Assert
        _mockLogger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(v => v.ToString()!.Contains(message)),
            exception,
            Arg.Any<Func<object, Exception?, string>>());
    }

    #endregion
}
