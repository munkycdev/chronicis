using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Blazored.LocalStorage;
using Bunit;
using Chronicis.Client.Abstractions;
using Chronicis.Client.Components.Articles;
using Chronicis.Client.Components.Characters;
using Chronicis.Client.Components.Drawers;
using Chronicis.Client.Components.Maps;
using Chronicis.Client.Components.Shared;
using Chronicis.Client.Services;
using Chronicis.Client.Utilities;
using Chronicis.Client.ViewModels;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MudBlazor;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Chronicis.Client.Tests.Components.Articles;

/// <summary>
/// Unit tests for session note page handwritten note integration.
/// Tests button vs Tab_UI state transitions, save/transcribe flows,
/// overwrite confirmation dialog, and error display on load failure.
/// </summary>
[ExcludeFromCodeCoverage]
public class ArticleDetailHandwrittenNoteIntegrationTests : MudBlazorTestContext
{
    private sealed record Deps(
        ArticleDetailViewModel ViewModel,
        IHandwrittenNoteApiService HandwrittenNoteApi,
        IDialogService DialogService,
        ISnackbar Snackbar,
        IAppContextService AppContext);

    private Deps CreateDeps()
    {
        var articleApi = Substitute.For<IArticleApiService>();
        var linkApi = Substitute.For<ILinkApiService>();
        var mapApi = Substitute.For<IMapApiService>();
        var wikiLinkService = Substitute.For<IWikiLinkService>();
        var summaryApi = Substitute.For<IAISummaryApiService>();
        var markdown = Substitute.For<IMarkdownService>();
        var treeState = Substitute.For<ITreeStateService>();
        var appContext = Substitute.For<IAppContextService>();
        var breadcrumbs = Substitute.For<IBreadcrumbService>();
        var articleCache = Substitute.For<IArticleCacheService>();
        var navigator = Substitute.For<IAppNavigator>();
        var notifier = Substitute.For<IUserNotifier>();
        var titleService = Substitute.For<IPageTitleService>();
        var externalLinkApi = Substitute.For<IExternalLinkApiService>();
        var drawerCoordinator = new DrawerCoordinator();
        var handwrittenNoteApi = Substitute.For<IHandwrittenNoteApiService>();
        var dialogService = Substitute.For<IDialogService>();
        var snackbar = Substitute.For<ISnackbar>();

        var viewModel = new ArticleDetailViewModel(
            articleApi,
            linkApi,
            treeState,
            breadcrumbs,
            appContext,
            articleCache,
            navigator,
            notifier,
            titleService,
            Substitute.For<ILogger<ArticleDetailViewModel>>());

        Services.AddSingleton(articleApi);
        Services.AddSingleton(linkApi);
        Services.AddSingleton(externalLinkApi);
        Services.AddSingleton(mapApi);
        Services.AddSingleton(wikiLinkService);
        Services.AddSingleton(Substitute.For<IWikiLinkCommitService>());
        Services.AddSingleton(summaryApi);
        Services.AddSingleton(markdown);
        Services.AddSingleton(treeState);
        Services.AddSingleton(appContext);
        Services.AddSingleton(breadcrumbs);
        Services.AddSingleton(Substitute.For<ILocalStorageService>());
        Services.AddSingleton(Substitute.For<IWorldApiService>());
        Services.AddSingleton(articleCache);
        Services.AddSingleton(navigator);
        Services.AddSingleton<IDrawerCoordinator>(drawerCoordinator);
        Services.AddSingleton(Substitute.For<IKeyboardShortcutService>());
        Services.AddSingleton(handwrittenNoteApi);
        Services.AddSingleton(dialogService);
        Services.AddSingleton(snackbar);
        Services.AddSingleton(Substitute.For<ILogger<ArticleDetail>>());
        Services.AddSingleton(viewModel);

        ComponentFactories.AddStub<LoadingSkeleton>();
        ComponentFactories.AddStub<EmptyState>();
        ComponentFactories.AddStub<ArticleHeader>();
        ComponentFactories.AddStub<CharacterClaimButton>();
        ComponentFactories.AddStub<AISummarySection>();
        ComponentFactories.AddStub<ArticleActionBar>();
        ComponentFactories.AddStub<DrawerHost>();
        ComponentFactories.AddStub<ExternalLinkDetailPanel>();
        ComponentFactories.AddStub<ArticleDetailWikiLinkAutocomplete>();
        ComponentFactories.AddStub<SessionMapViewerModal>();
        ComponentFactories.AddStub<DrawingCanvas>();
        ComponentFactories.AddStub<HandwrittenNoteTabView>();

        return new Deps(viewModel, handwrittenNoteApi, dialogService, snackbar, appContext);
    }

    private IRenderedComponent<ArticleDetail> RenderWithArticle(Deps deps, ArticleDto article)
    {
        deps.ViewModel.HydrateArticleAsync(article).GetAwaiter().GetResult();
        var cut = RenderComponent<ArticleDetail>();
        SetField(cut.Instance, "_article", article);
        SetField(cut.Instance, "_editTitle", article.Title);
        SetField(cut.Instance, "_editBody", article.Body ?? string.Empty);
        return cut;
    }

    private static ArticleDto CreateSessionNote(Guid? handwrittenNoteImageId = null, string? body = null)
        => new()
        {
            Id = Guid.NewGuid(),
            WorldId = Guid.NewGuid(),
            Title = "Session 1",
            Body = body ?? string.Empty,
            Type = ArticleType.SessionNote,
            HandwrittenNoteImageId = handwrittenNoteImageId
        };

    #region Button vs Tab_UI State Transitions (Requirements 1.1, 1.2, 1.3)

    [Fact]
    public void WhenHandwrittenNoteImageIdNull_ShowsAddButton_NotTabView()
    {
        // Validates the view state logic: null ImageId → add button, not tab view
        var deps = CreateDeps();
        var article = CreateSessionNote(handwrittenNoteImageId: null);
        var cut = RenderWithArticle(deps, article);

        var showCanvas = (bool)GetField(cut.Instance, "_showDrawingCanvas")!;
        Assert.False(showCanvas);
        Assert.False(HandwrittenNoteViewState.ShouldShowTabUi(article.HandwrittenNoteImageId));
        Assert.True(HandwrittenNoteViewState.ShouldShowAddButton(article.HandwrittenNoteImageId));
    }

    [Fact]
    public async Task WhenHandwrittenNoteImageIdNonNull_LoadsImageUrl()
    {
        var deps = CreateDeps();
        var imageId = Guid.NewGuid();
        var article = CreateSessionNote(handwrittenNoteImageId: imageId);
        deps.HandwrittenNoteApi.GetHandwrittenNoteUrlAsync(article.Id)
            .Returns("https://example.com/image.png");

        var cut = RenderWithArticle(deps, article);
        await cut.InvokeAsync(() => InvokePrivateTask(cut.Instance, "LoadHandwrittenNoteImageUrlAsync"));

        Assert.Equal("https://example.com/image.png", GetField(cut.Instance, "_handwrittenNoteImageUrl"));
        Assert.True(HandwrittenNoteViewState.ShouldShowTabUi(article.HandwrittenNoteImageId));
        Assert.False(HandwrittenNoteViewState.ShouldShowAddButton(article.HandwrittenNoteImageId));
    }

    [Fact]
    public async Task WhenAddButtonClicked_ShowsDrawingCanvas()
    {
        var deps = CreateDeps();
        var article = CreateSessionNote(handwrittenNoteImageId: null);
        var cut = RenderWithArticle(deps, article);

        await cut.InvokeAsync(() => InvokePrivateMethod(cut.Instance, "ShowDrawingCanvas"));

        var showCanvas = (bool)GetField(cut.Instance, "_showDrawingCanvas")!;
        Assert.True(showCanvas);
    }

    #endregion

    #region Save Flow (Requirements 3.4, 3.5)

    [Fact]
    public async Task HandleSave_Success_TransitionsToTabView()
    {
        var deps = CreateDeps();
        var article = CreateSessionNote(handwrittenNoteImageId: null);
        var cut = RenderWithArticle(deps, article);
        SetField(cut.Instance, "_showDrawingCanvas", true);

        var docId = Guid.NewGuid();
        deps.HandwrittenNoteApi.SaveHandwrittenNoteAsync(article.Id, Arg.Any<byte[]>())
            .Returns(new HandwrittenNoteSaveResultDto
            {
                DocumentId = docId,
                DownloadUrl = "https://example.com/saved.png"
            });

        var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        await cut.InvokeAsync(() => InvokePrivateTask(cut.Instance, "HandleHandwrittenNoteSave", pngBytes));

        Assert.False((bool)GetField(cut.Instance, "_showDrawingCanvas")!);
        Assert.Equal(docId, article.HandwrittenNoteImageId);
        Assert.Equal("https://example.com/saved.png", GetField(cut.Instance, "_handwrittenNoteImageUrl"));
        Assert.False((bool)GetField(cut.Instance, "_isHandwrittenNoteSaving")!);
    }

    [Fact]
    public async Task HandleSave_ReturnsNull_ShowsError_PreservesCanvas()
    {
        var deps = CreateDeps();
        var article = CreateSessionNote(handwrittenNoteImageId: null);
        var cut = RenderWithArticle(deps, article);
        SetField(cut.Instance, "_showDrawingCanvas", true);

        deps.HandwrittenNoteApi.SaveHandwrittenNoteAsync(article.Id, Arg.Any<byte[]>())
            .Returns((HandwrittenNoteSaveResultDto?)null);

        var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        await cut.InvokeAsync(() => InvokePrivateTask(cut.Instance, "HandleHandwrittenNoteSave", pngBytes));

        // Canvas preserved, error shown
        Assert.True((bool)GetField(cut.Instance, "_showDrawingCanvas")!);
        Assert.False((bool)GetField(cut.Instance, "_isHandwrittenNoteSaving")!);
        deps.Snackbar.Received(1).Add("Failed to save handwritten note", Severity.Error);
    }

    [Fact]
    public async Task HandleSave_Throws_ShowsError_PreservesCanvas_ReEnablesButton()
    {
        var deps = CreateDeps();
        var article = CreateSessionNote(handwrittenNoteImageId: null);
        var cut = RenderWithArticle(deps, article);
        SetField(cut.Instance, "_showDrawingCanvas", true);

        deps.HandwrittenNoteApi.SaveHandwrittenNoteAsync(article.Id, Arg.Any<byte[]>())
            .Throws(new Exception("Network error"));

        var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        await cut.InvokeAsync(() => InvokePrivateTask(cut.Instance, "HandleHandwrittenNoteSave", pngBytes));

        Assert.True((bool)GetField(cut.Instance, "_showDrawingCanvas")!);
        Assert.False((bool)GetField(cut.Instance, "_isHandwrittenNoteSaving")!);
        deps.Snackbar.Received(1).Add(
            Arg.Is<string>(s => s.Contains("Network error")),
            Severity.Error);
    }

    #endregion

    #region Transcribe Flow (Requirements 4.1, 4.2, 4.6, 4.7)

    [Fact]
    public async Task HandleTranscribe_Success_TransitionsToTabViewWithTranscribedTabActive()
    {
        var deps = CreateDeps();
        var article = CreateSessionNote(handwrittenNoteImageId: null);
        var cut = RenderWithArticle(deps, article);
        SetField(cut.Instance, "_showDrawingCanvas", true);

        var docId = Guid.NewGuid();
        deps.HandwrittenNoteApi.TranscribeHandwrittenNoteAsync(article.Id, Arg.Any<byte[]>())
            .Returns(new HandwrittenNoteTranscribeResultDto
            {
                DocumentId = docId,
                DownloadUrl = "https://example.com/transcribed.png",
                TranscribedText = "Hello world"
            });

        var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        await cut.InvokeAsync(() => InvokePrivateTask(cut.Instance, "HandleHandwrittenNoteTranscribe", pngBytes));

        Assert.False((bool)GetField(cut.Instance, "_showDrawingCanvas")!);
        Assert.True((bool)GetField(cut.Instance, "_showTranscribedTabActive")!);
        Assert.Equal(docId, article.HandwrittenNoteImageId);
        Assert.Equal("Hello world", article.Body);
        Assert.Equal("Hello world", GetField(cut.Instance, "_editBody"));
    }

    [Fact]
    public async Task HandleTranscribe_ReturnsNull_ShowsError()
    {
        var deps = CreateDeps();
        var article = CreateSessionNote(handwrittenNoteImageId: null);
        var cut = RenderWithArticle(deps, article);
        SetField(cut.Instance, "_showDrawingCanvas", true);

        deps.HandwrittenNoteApi.TranscribeHandwrittenNoteAsync(article.Id, Arg.Any<byte[]>())
            .Returns((HandwrittenNoteTranscribeResultDto?)null);

        var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        await cut.InvokeAsync(() => InvokePrivateTask(cut.Instance, "HandleHandwrittenNoteTranscribe", pngBytes));

        Assert.True((bool)GetField(cut.Instance, "_showDrawingCanvas")!);
        Assert.False((bool)GetField(cut.Instance, "_isHandwrittenNoteSaving")!);
        deps.Snackbar.Received(1).Add(
            "Failed to transcribe handwritten note",
            Severity.Error);
    }

    [Fact]
    public async Task HandleTranscribe_Throws_ShowsError_PreservesCanvas()
    {
        var deps = CreateDeps();
        var article = CreateSessionNote(handwrittenNoteImageId: null);
        var cut = RenderWithArticle(deps, article);
        SetField(cut.Instance, "_showDrawingCanvas", true);

        deps.HandwrittenNoteApi.TranscribeHandwrittenNoteAsync(article.Id, Arg.Any<byte[]>())
            .Throws(new Exception("Timeout"));

        var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        await cut.InvokeAsync(() => InvokePrivateTask(cut.Instance, "HandleHandwrittenNoteTranscribe", pngBytes));

        Assert.True((bool)GetField(cut.Instance, "_showDrawingCanvas")!);
        Assert.False((bool)GetField(cut.Instance, "_isHandwrittenNoteSaving")!);
        deps.Snackbar.Received(1).Add(
            Arg.Is<string>(s => s.Contains("Timeout")),
            Severity.Error);
    }

    #endregion

    #region Overwrite Confirmation Dialog (Requirement 4.7)

    [Fact]
    public async Task HandleTranscribe_WithExistingBody_ShowsConfirmation_Confirmed_Proceeds()
    {
        var deps = CreateDeps();
        var article = CreateSessionNote(handwrittenNoteImageId: null, body: "Existing content");
        var cut = RenderWithArticle(deps, article);
        SetField(cut.Instance, "_showDrawingCanvas", true);

        deps.DialogService.ShowMessageBox(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DialogOptions>())
            .Returns(Task.FromResult<bool?>(true));

        var docId = Guid.NewGuid();
        deps.HandwrittenNoteApi.TranscribeHandwrittenNoteAsync(article.Id, Arg.Any<byte[]>())
            .Returns(new HandwrittenNoteTranscribeResultDto
            {
                DocumentId = docId,
                DownloadUrl = "https://example.com/img.png",
                TranscribedText = "New text"
            });

        var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        await cut.InvokeAsync(() => InvokePrivateTask(cut.Instance, "HandleHandwrittenNoteTranscribe", pngBytes));

        // Confirmation was shown
        await deps.DialogService.Received(1).ShowMessageBox(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DialogOptions>());
        // Transcription proceeded
        Assert.Equal("New text", article.Body);
        Assert.False((bool)GetField(cut.Instance, "_showDrawingCanvas")!);
    }

    [Fact]
    public async Task HandleTranscribe_WithExistingBody_ShowsConfirmation_Cancelled_DoesNotProceed()
    {
        var deps = CreateDeps();
        var article = CreateSessionNote(handwrittenNoteImageId: null, body: "Keep this");
        var cut = RenderWithArticle(deps, article);
        SetField(cut.Instance, "_showDrawingCanvas", true);

        deps.DialogService.ShowMessageBox(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DialogOptions>())
            .Returns(Task.FromResult<bool?>(null));

        var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        await cut.InvokeAsync(() => InvokePrivateTask(cut.Instance, "HandleHandwrittenNoteTranscribe", pngBytes));

        // API not called, body preserved
        await deps.HandwrittenNoteApi.DidNotReceive()
            .TranscribeHandwrittenNoteAsync(Arg.Any<Guid>(), Arg.Any<byte[]>());
        Assert.Equal("Keep this", article.Body);
        Assert.True((bool)GetField(cut.Instance, "_showDrawingCanvas")!);
    }

    [Fact]
    public async Task HandleTranscribe_WithEmptyBody_NoConfirmation()
    {
        var deps = CreateDeps();
        var article = CreateSessionNote(handwrittenNoteImageId: null, body: null);
        var cut = RenderWithArticle(deps, article);
        SetField(cut.Instance, "_showDrawingCanvas", true);

        var docId = Guid.NewGuid();
        deps.HandwrittenNoteApi.TranscribeHandwrittenNoteAsync(article.Id, Arg.Any<byte[]>())
            .Returns(new HandwrittenNoteTranscribeResultDto
            {
                DocumentId = docId,
                DownloadUrl = "https://example.com/img.png",
                TranscribedText = "Transcribed"
            });

        var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        await cut.InvokeAsync(() => InvokePrivateTask(cut.Instance, "HandleHandwrittenNoteTranscribe", pngBytes));

        // No dialog shown
        await deps.DialogService.DidNotReceive().ShowMessageBox(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DialogOptions>());
        Assert.Equal("Transcribed", article.Body);
    }

    #endregion

    #region Error Display on Load Failure (Requirement 1.4)

    [Fact]
    public async Task LoadHandwrittenNoteImageUrl_Throws_SetsLoadError()
    {
        var deps = CreateDeps();
        var imageId = Guid.NewGuid();
        var article = CreateSessionNote(handwrittenNoteImageId: imageId);

        deps.HandwrittenNoteApi.GetHandwrittenNoteUrlAsync(article.Id)
            .Throws(new Exception("Storage unavailable"));

        var cut = RenderWithArticle(deps, article);
        await cut.InvokeAsync(() => InvokePrivateTask(cut.Instance, "LoadHandwrittenNoteImageUrlAsync"));

        Assert.True((bool)GetField(cut.Instance, "_handwrittenNoteLoadError")!);
    }

    [Fact]
    public async Task LoadHandwrittenNoteImageUrl_Success_NoError()
    {
        var deps = CreateDeps();
        var imageId = Guid.NewGuid();
        var article = CreateSessionNote(handwrittenNoteImageId: imageId);

        deps.HandwrittenNoteApi.GetHandwrittenNoteUrlAsync(article.Id)
            .Returns("https://example.com/note.png");

        var cut = RenderWithArticle(deps, article);
        await cut.InvokeAsync(() => InvokePrivateTask(cut.Instance, "LoadHandwrittenNoteImageUrlAsync"));

        Assert.False((bool)GetField(cut.Instance, "_handwrittenNoteLoadError")!);
        Assert.Equal("https://example.com/note.png", GetField(cut.Instance, "_handwrittenNoteImageUrl"));
    }

    [Fact]
    public async Task LoadHandwrittenNoteImageUrl_WhenNoImageId_DoesNotCallApi()
    {
        var deps = CreateDeps();
        var article = CreateSessionNote(handwrittenNoteImageId: null);

        var cut = RenderWithArticle(deps, article);
        await cut.InvokeAsync(() => InvokePrivateTask(cut.Instance, "LoadHandwrittenNoteImageUrlAsync"));

        await deps.HandwrittenNoteApi.DidNotReceive().GetHandwrittenNoteUrlAsync(Arg.Any<Guid>());
    }

    #endregion

    #region HandleHandwrittenNoteBodyChanged

    [Fact]
    public async Task HandleBodyChanged_UpdatesArticleAndEditBody()
    {
        var deps = CreateDeps();
        var article = CreateSessionNote(handwrittenNoteImageId: Guid.NewGuid(), body: "old");
        var cut = RenderWithArticle(deps, article);

        await cut.InvokeAsync(() => InvokePrivateTask(cut.Instance, "HandleHandwrittenNoteBodyChanged", "new content"));

        Assert.Equal("new content", article.Body);
        Assert.Equal("new content", GetField(cut.Instance, "_editBody"));
        Assert.True((bool)GetField(cut.Instance, "_hasUnsavedChanges")!);
    }

    #endregion

    #region HandleSave with null article

    [Fact]
    public async Task HandleSave_WhenArticleNull_DoesNothing()
    {
        var deps = CreateDeps();
        var article = CreateSessionNote();
        var cut = RenderWithArticle(deps, article);
        SetField(cut.Instance, "_article", null);

        var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        await cut.InvokeAsync(() => InvokePrivateTask(cut.Instance, "HandleHandwrittenNoteSave", pngBytes));

        await deps.HandwrittenNoteApi.DidNotReceive()
            .SaveHandwrittenNoteAsync(Arg.Any<Guid>(), Arg.Any<byte[]>());
    }

    [Fact]
    public async Task HandleTranscribe_WhenArticleNull_DoesNothing()
    {
        var deps = CreateDeps();
        var article = CreateSessionNote();
        var cut = RenderWithArticle(deps, article);
        SetField(cut.Instance, "_article", null);

        var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        await cut.InvokeAsync(() => InvokePrivateTask(cut.Instance, "HandleHandwrittenNoteTranscribe", pngBytes));

        await deps.HandwrittenNoteApi.DidNotReceive()
            .TranscribeHandwrittenNoteAsync(Arg.Any<Guid>(), Arg.Any<byte[]>());
    }

    #endregion

    #region Helpers

    private static object? GetField(object instance, string name)
        => instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(instance);

    private static void SetField(object instance, string name, object? value)
        => instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(instance, value);

    private static async Task InvokePrivateTask(object instance, string methodName, params object?[] args)
    {
        var result = instance.GetType()
            .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(instance, args);
        if (result is Task task)
            await task;
    }

    private static void InvokePrivateMethod(object instance, string methodName, params object?[] args)
    {
        instance.GetType()
            .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(instance, args);
    }

    #endregion
}
