using System.ComponentModel;
using System.Reflection;
using Bunit;
using Bunit.TestDoubles;
using Chronicis.Client.Abstractions;
using Chronicis.Client.Pages;
using Chronicis.Client.Services;
using Chronicis.Client.Tests.Components;
using Chronicis.Client.ViewModels;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.DTOs.Maps;
using Chronicis.Shared.DTOs.Sessions;
using Chronicis.Shared.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MudBlazor;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Pages;

public class SessionDetailTests : MudBlazorTestContext
{
    private bool _providersRendered;

    private sealed record PageDeps(
        SessionDetailViewModel ViewModel,
        ISessionApiService SessionApi,
        IArticleApiService ArticleApi,
        IArcApiService ArcApi,
        ICampaignApiService CampaignApi,
        IWorldApiService WorldApi,
        IAuthService AuthService,
        ITreeStateService TreeState,
        IBreadcrumbService Breadcrumbs,
        IAppNavigator Navigator,
        IUserNotifier Notifier,
        IPageTitleService TitleService,
        IDialogService DialogService,
        IKeyboardShortcutService KeyboardShortcuts,
        ILinkApiService LinkApi,
        IExternalLinkApiService ExternalLinkApi,
        IMapApiService MapApi,
        IWikiLinkService WikiLinkService,
        IAppContextService AppContext,
        IArticleCacheService ArticleCache,
        IAISummaryApiService SummaryApi,
        IDrawerCoordinator DrawerCoordinator);

    private static PageDeps CreatePageDeps()
    {
        var sessionApi = Substitute.For<ISessionApiService>();
        var articleApi = Substitute.For<IArticleApiService>();
        var arcApi = Substitute.For<IArcApiService>();
        var campaignApi = Substitute.For<ICampaignApiService>();
        var worldApi = Substitute.For<IWorldApiService>();
        var authService = Substitute.For<IAuthService>();
        var treeState = Substitute.For<ITreeStateService>();
        var breadcrumbs = Substitute.For<IBreadcrumbService>();
        var navigator = Substitute.For<IAppNavigator>();
        var notifier = Substitute.For<IUserNotifier>();
        var titleService = Substitute.For<IPageTitleService>();
        var dialogService = Substitute.For<IDialogService>();
        var keyboardShortcuts = new KeyboardShortcutService();
        var linkApi = Substitute.For<ILinkApiService>();
        var externalLinkApi = Substitute.For<IExternalLinkApiService>();
        var mapApi = Substitute.For<IMapApiService>();
        var wikiLinkService = Substitute.For<IWikiLinkService>();
        var appContext = Substitute.For<IAppContextService>();
        var articleCache = Substitute.For<IArticleCacheService>();
        var summaryApi = Substitute.For<IAISummaryApiService>();
        var drawerCoordinator = new DrawerCoordinator();
        var vmLogger = Substitute.For<ILogger<SessionDetailViewModel>>();

        var vm = new SessionDetailViewModel(
            sessionApi,
            articleApi,
            arcApi,
            campaignApi,
            worldApi,
            authService,
            treeState,
            breadcrumbs,
            navigator,
            notifier,
            titleService,
            vmLogger);

        return new PageDeps(
            vm,
            sessionApi,
            articleApi,
            arcApi,
            campaignApi,
            worldApi,
            authService,
            treeState,
            breadcrumbs,
            navigator,
            notifier,
            titleService,
            dialogService,
            keyboardShortcuts,
            linkApi,
            externalLinkApi,
            mapApi,
            wikiLinkService,
            appContext,
            articleCache,
            summaryApi,
            drawerCoordinator);
    }

    private IRenderedComponent<SessionDetail> RenderPage(PageDeps d, Guid sessionId)
    {
        Services.AddSingleton(d.ViewModel);
        Services.AddSingleton(d.DialogService);
        Services.AddSingleton(d.KeyboardShortcuts);
        Services.AddSingleton(Substitute.For<ILogger<SessionDetail>>());
        Services.AddSingleton(d.WorldApi);
        Services.AddSingleton(d.LinkApi);
        Services.AddSingleton(d.ExternalLinkApi);
        Services.AddSingleton(d.MapApi);
        Services.AddSingleton(d.WikiLinkService);
        Services.AddSingleton(d.AppContext);
        Services.AddSingleton(d.ArticleCache);
        Services.AddSingleton(d.SummaryApi);
        Services.AddSingleton<IDrawerCoordinator>(d.DrawerCoordinator);

        Services.GetRequiredService<FakeNavigationManager>().NavigateTo("http://localhost/session/test");

        if (!_providersRendered)
        {
            RenderComponent<MudPopoverProvider>();
            RenderComponent<MudSnackbarProvider>();
            RenderComponent<MudDialogProvider>();
            _providersRendered = true;
        }

        ComponentFactories.AddStub<Chronicis.Client.Components.Articles.ArticleDetailWikiLinkAutocomplete>();
        ComponentFactories.AddStub<Chronicis.Client.Components.Shared.ExternalLinkDetailPanel>();
        ComponentFactories.AddStub<Chronicis.Client.Components.Shared.ChroniclsBreadcrumbs>();
        ComponentFactories.AddStub<Chronicis.Client.Components.Shared.SaveStatusIndicator>();
        ComponentFactories.AddStub<Chronicis.Client.Components.Shared.EntityListItem>();

        return RenderComponent<SessionDetail>(p => p.Add(x => x.SessionId, sessionId));
    }

    [Fact]
    public void SessionDetail_WhenLoadInProgress_RendersLoadingSkeleton()
    {
        var d = CreatePageDeps();
        var sessionId = Guid.NewGuid();
        var tcs = new TaskCompletionSource<SessionDto?>();
        d.SessionApi.GetSessionAsync(sessionId).Returns(tcs.Task);

        var cut = RenderPage(d, sessionId);

        Assert.Contains("chronicis-loading-skeleton", cut.Markup, StringComparison.OrdinalIgnoreCase);
        tcs.SetResult(null);
    }

    [Fact]
    public void SessionDetail_WhenSessionMissing_RendersNotFound()
    {
        var d = CreatePageDeps();
        var sessionId = Guid.NewGuid();
        d.SessionApi.GetSessionAsync(sessionId).Returns((SessionDto?)null);

        var cut = RenderPage(d, sessionId);

        cut.WaitForAssertion(() =>
            Assert.Contains("not found", cut.Markup, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void SessionDetail_RendersGmEditorContainers()
    {
        var gmSessionId = Guid.NewGuid();
        var gmDeps = CreatePageDeps();
        ConfigureLoadedSession(gmDeps, gmSessionId, isGm: true, publicNotes: "<p>Public</p>");
        var gmCut = RenderPage(gmDeps, gmSessionId);

        gmCut.WaitForAssertion(() =>
        {
            Assert.Contains($"session-public-editor-{gmSessionId}", gmCut.Markup, StringComparison.Ordinal);
            Assert.Contains($"session-private-editor-{gmSessionId}", gmCut.Markup, StringComparison.Ordinal);
            Assert.Contains("Delete", gmCut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void SessionDetail_RendersNonGmReadonlyState()
    {
        var playerSessionId = Guid.NewGuid();
        var playerDeps = CreatePageDeps();
        ConfigureLoadedSession(playerDeps, playerSessionId, isGm: false, publicNotes: "", privateNotes: "<p>secret</p>");
        var playerCut = RenderPage(playerDeps, playerSessionId);

        playerCut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("Private Notes", playerCut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("No public notes yet", playerCut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task SessionDetail_MapAutocomplete_UsesMapsPrefixPath_AndPassesParsedQuery()
    {
        var d = CreatePageDeps();
        var sessionId = Guid.NewGuid();
        ConfigureLoadedSession(d, sessionId, isGm: true, publicNotes: "<p>pub</p>", privateNotes: "<p>priv</p>");
        var cut = RenderPage(d, sessionId);
        cut.WaitForAssertion(() => Assert.NotNull(cut.Instance));

        var pageType = typeof(SessionDetail);
        var editorKindType = pageType.GetNestedType("SessionEditorKind", BindingFlags.NonPublic);
        Assert.NotNull(editorKindType);
        var publicKind = Enum.Parse(editorKindType!, "Public");
        var worldId = d.ViewModel.World!.Id;

        d.MapApi.GetMapAutocompleteAsync(worldId, null).Returns(new List<MapAutocompleteDto>());
        d.MapApi.GetMapAutocompleteAsync(worldId, "Sword").Returns(new List<MapAutocompleteDto>());
        d.MapApi.GetMapAutocompleteAsync(worldId, "amb").Returns(new List<MapAutocompleteDto>());

        await InvokeNonPublicTask(cut.Instance, "HandleAutocompleteTriggeredAsync", publicKind, "maps/   ", 10d, 20d);
        await InvokeNonPublicTask(cut.Instance, "HandleAutocompleteTriggeredAsync", publicKind, "maps/ Sword ", 10d, 20d);
        await InvokeNonPublicTask(cut.Instance, "HandleAutocompleteTriggeredAsync", publicKind, "maps/amb", 10d, 20d);

        await d.MapApi.Received(1).GetMapAutocompleteAsync(worldId, null);
        await d.MapApi.Received(1).GetMapAutocompleteAsync(worldId, "Sword");
        await d.MapApi.Received(1).GetMapAutocompleteAsync(worldId, "amb");
        await d.ExternalLinkApi.DidNotReceive().GetSuggestionsAsync(
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SessionDetail_MapAutocomplete_PrefersSessionWorldId_WhenAppContextIsDifferent()
    {
        var d = CreatePageDeps();
        var sessionId = Guid.NewGuid();
        ConfigureLoadedSession(d, sessionId, isGm: true, publicNotes: "<p>pub</p>", privateNotes: "<p>priv</p>");
        var cut = RenderPage(d, sessionId);
        cut.WaitForAssertion(() => Assert.NotNull(cut.Instance));

        var pageType = typeof(SessionDetail);
        var editorKindType = pageType.GetNestedType("SessionEditorKind", BindingFlags.NonPublic);
        Assert.NotNull(editorKindType);
        var publicKind = Enum.Parse(editorKindType!, "Public");

        var sessionWorldId = d.ViewModel.World!.Id;
        var staleWorldId = Guid.NewGuid();
        d.AppContext.CurrentWorldId.Returns((Guid?)staleWorldId);
        d.MapApi.GetMapAutocompleteAsync(sessionWorldId, null).Returns(new List<MapAutocompleteDto>());

        await InvokeNonPublicTask(cut.Instance, "HandleAutocompleteTriggeredAsync", publicKind, "maps/", 10d, 20d);

        await d.MapApi.Received(1).GetMapAutocompleteAsync(sessionWorldId, null);
        await d.MapApi.DidNotReceive().GetMapAutocompleteAsync(staleWorldId, Arg.Any<string?>());
    }

    [Fact]
    public async Task SessionDetail_MapAutocomplete_MapsSuggestionsAndPreservesMapId()
    {
        var d = CreatePageDeps();
        var sessionId = Guid.NewGuid();
        ConfigureLoadedSession(d, sessionId, isGm: true, publicNotes: "<p>pub</p>");
        var cut = RenderPage(d, sessionId);
        cut.WaitForAssertion(() => Assert.NotNull(cut.Instance));

        var pageType = typeof(SessionDetail);
        var editorKindType = pageType.GetNestedType("SessionEditorKind", BindingFlags.NonPublic);
        Assert.NotNull(editorKindType);
        var publicKind = Enum.Parse(editorKindType!, "Public");
        var worldId = d.ViewModel.World!.Id;
        var mapId = Guid.NewGuid();

        d.MapApi.GetMapAutocompleteAsync(worldId, null).Returns(new List<MapAutocompleteDto>
        {
            new() { MapId = mapId, Name = "Sword Coast" }
        });

        await InvokeNonPublicTask(cut.Instance, "HandleAutocompleteTriggeredAsync", publicKind, "maps/", 10d, 20d);

        var suggestions = Assert.IsType<List<Chronicis.Client.Components.Articles.WikiLinkAutocompleteItem>>(
            GetField(cut.Instance, "_autocompleteSuggestions"));
        Assert.Single(suggestions);
        Assert.Equal("Sword Coast", suggestions[0].Title);
        Assert.Equal("Map", suggestions[0].SecondaryText);
        Assert.Equal(mapId, suggestions[0].MapId);
    }

    [Fact]
    public async Task SessionDetail_MapAutocomplete_Select_InsertsMapTokenWithMapIdentity()
    {
        var d = CreatePageDeps();
        var sessionId = Guid.NewGuid();
        ConfigureLoadedSession(d, sessionId, isGm: true, publicNotes: "<p>pub</p>");
        var cut = RenderPage(d, sessionId);
        cut.WaitForAssertion(() => Assert.NotNull(cut.Instance));

        var mapId = Guid.NewGuid();
        var suggestion = Chronicis.Client.Components.Articles.WikiLinkAutocompleteItem.FromMapAutocomplete(
            new MapAutocompleteDto
            {
                MapId = mapId,
                Name = "Sword Coast"
            });

        JSInterop.SetupVoid("insertMapLinkToken", _ => true).SetVoidResult();

        await InvokeNonPublicTask(cut.Instance, "OnAutocompleteSelect", suggestion);

        var invocation = Assert.Single(JSInterop.Invocations.Where(i => i.Identifier == "insertMapLinkToken"));
        Assert.Equal($"session-public-editor-{sessionId}", invocation.Arguments[0]?.ToString());
        Assert.Equal(mapId.ToString(), invocation.Arguments[1]?.ToString());
        Assert.Equal("Sword Coast", invocation.Arguments[2]?.ToString());
    }

    [Fact]
    public async Task SessionDetail_MapChipCallback_OpensModal_WithMapIdentity()
    {
        var d = CreatePageDeps();
        var sessionId = Guid.NewGuid();
        ConfigureLoadedSession(d, sessionId, isGm: true, publicNotes: "<p>pub</p>");
        var mapId = Guid.NewGuid();
        d.MapApi.GetBasemapReadUrlAsync(Arg.Any<Guid>(), mapId)
            .Returns((new GetBasemapReadUrlResponseDto { ReadUrl = "https://blob.example.com/read" }, 200, null));

        var cut = RenderPage(d, sessionId);
        cut.WaitForAssertion(() => Assert.NotNull(cut.Instance));

        var bridge = GetField(cut.Instance, "_publicEditorBridge");
        Assert.NotNull(bridge);

        await cut.InvokeAsync(() => InvokeAnyTask(bridge!, "OnMapLinkClicked", mapId.ToString(), "Sword Coast"));

        Assert.True((bool)GetField(cut.Instance, "_isMapModalOpen")!);
        Assert.Equal(mapId, (Guid)GetField(cut.Instance, "_selectedMapId")!);
        Assert.Equal("Sword Coast", GetField(cut.Instance, "_selectedMapName"));
        cut.WaitForAssertion(() =>
            Assert.Contains("session-map-viewer-modal", cut.Markup, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SessionDetail_MapChipCallback_InvalidMapId_DoesNotOpenModal()
    {
        var d = CreatePageDeps();
        var sessionId = Guid.NewGuid();
        ConfigureLoadedSession(d, sessionId, isGm: true, publicNotes: "<p>pub</p>");
        var cut = RenderPage(d, sessionId);
        cut.WaitForAssertion(() => Assert.NotNull(cut.Instance));

        var bridge = GetField(cut.Instance, "_publicEditorBridge");
        Assert.NotNull(bridge);

        await cut.InvokeAsync(() => InvokeAnyTask(bridge!, "OnMapLinkClicked", "not-a-guid", "Sword Coast"));

        Assert.False((bool)GetField(cut.Instance, "_isMapModalOpen")!);
        Assert.Equal(Guid.Empty, (Guid)GetField(cut.Instance, "_selectedMapId")!);
        Assert.Null(GetField(cut.Instance, "_selectedMapName"));
    }

    [Fact]
    public async Task SessionDetail_MapChipCallback_WhitespaceName_NormalizesToNull()
    {
        var d = CreatePageDeps();
        var sessionId = Guid.NewGuid();
        ConfigureLoadedSession(d, sessionId, isGm: true, publicNotes: "<p>pub</p>");
        var mapId = Guid.NewGuid();
        d.MapApi.GetBasemapReadUrlAsync(Arg.Any<Guid>(), mapId)
            .Returns((new GetBasemapReadUrlResponseDto { ReadUrl = "https://blob.example.com/read" }, 200, null));
        var cut = RenderPage(d, sessionId);
        cut.WaitForAssertion(() => Assert.NotNull(cut.Instance));

        var bridge = GetField(cut.Instance, "_publicEditorBridge");
        Assert.NotNull(bridge);

        await cut.InvokeAsync(() => InvokeAnyTask(bridge!, "OnMapLinkClicked", mapId.ToString(), "   "));

        Assert.True((bool)GetField(cut.Instance, "_isMapModalOpen")!);
        Assert.Equal(mapId, (Guid)GetField(cut.Instance, "_selectedMapId")!);
        Assert.Null(GetField(cut.Instance, "_selectedMapName"));
    }

    [Fact]
    public void SessionDetail_CurrentWorldId_PrefersWorld_ThenAppContext_ThenEmpty()
    {
        var d = CreatePageDeps();
        var sessionId = Guid.NewGuid();
        ConfigureLoadedSession(d, sessionId, isGm: true, publicNotes: "<p>pub</p>");
        var cut = RenderPage(d, sessionId);
        cut.WaitForAssertion(() => Assert.NotNull(cut.Instance));

        var currentWorldIdProperty = typeof(SessionDetail).GetProperty("CurrentWorldId", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(currentWorldIdProperty);

        var appContextWorldId = Guid.NewGuid();
        d.AppContext.CurrentWorldId.Returns((Guid?)appContextWorldId);
        Assert.Equal(d.ViewModel.World!.Id, (Guid)currentWorldIdProperty!.GetValue(cut.Instance)!);

        SetField(d.ViewModel, "_world", null);
        d.AppContext.CurrentWorldId.Returns((Guid?)null);
        Assert.Equal(Guid.Empty, (Guid)currentWorldIdProperty.GetValue(cut.Instance)!);

        d.AppContext.CurrentWorldId.Returns((Guid?)appContextWorldId);
        Assert.Equal(appContextWorldId, (Guid)currentWorldIdProperty.GetValue(cut.Instance)!);
    }

    [Fact]
    public async Task SessionDetail_MapModal_Close_ClearsState_AndHidesModal()
    {
        var d = CreatePageDeps();
        var sessionId = Guid.NewGuid();
        ConfigureLoadedSession(d, sessionId, isGm: true, publicNotes: "<p>pub</p>");
        var mapId = Guid.NewGuid();
        d.MapApi.GetBasemapReadUrlAsync(Arg.Any<Guid>(), mapId)
            .Returns((new GetBasemapReadUrlResponseDto { ReadUrl = "https://blob.example.com/read" }, 200, null));

        var cut = RenderPage(d, sessionId);
        cut.WaitForAssertion(() => Assert.NotNull(cut.Instance));

        var bridge = GetField(cut.Instance, "_publicEditorBridge");
        Assert.NotNull(bridge);
        await cut.InvokeAsync(() => InvokeAnyTask(bridge!, "OnMapLinkClicked", mapId.ToString(), "Sword Coast"));

        cut.WaitForAssertion(() =>
            Assert.Contains("session-map-viewer-modal", cut.Markup, StringComparison.OrdinalIgnoreCase));

        cut.Find(".session-map-viewer-modal__close").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.False((bool)GetField(cut.Instance, "_isMapModalOpen")!);
            Assert.Equal(Guid.Empty, (Guid)GetField(cut.Instance, "_selectedMapId")!);
            Assert.Null(GetField(cut.Instance, "_selectedMapName"));
            Assert.DoesNotContain("session-map-viewer-modal", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task SessionDetail_WikiLinkClick_StillNavigatesToArticle()
    {
        var d = CreatePageDeps();
        var sessionId = Guid.NewGuid();
        ConfigureLoadedSession(d, sessionId, isGm: true, publicNotes: "<p>pub</p>");
        var articleId = Guid.NewGuid();
        const string articlePath = "world/wiki/castle-ward";
        d.ArticleCache.GetNavigationPathAsync(articleId).Returns(articlePath);

        var cut = RenderPage(d, sessionId);
        cut.WaitForAssertion(() => Assert.NotNull(cut.Instance));

        var bridge = GetField(cut.Instance, "_publicEditorBridge");
        Assert.NotNull(bridge);

        await cut.InvokeAsync(() => InvokeAnyTask(bridge!, "OnWikiLinkClicked", articleId.ToString()));

        var navigation = Services.GetRequiredService<FakeNavigationManager>();
        Assert.EndsWith($"/article/{articlePath}", navigation.Uri, StringComparison.OrdinalIgnoreCase);
        Assert.False((bool)GetField(cut.Instance, "_isMapModalOpen")!);
    }

    [Fact]
    public async Task SessionDetail_NonMapAutocomplete_PathRemainsUnchanged()
    {
        var d = CreatePageDeps();
        var sessionId = Guid.NewGuid();
        ConfigureLoadedSession(d, sessionId, isGm: true, publicNotes: "<p>pub</p>", privateNotes: "<p>priv</p>");
        var cut = RenderPage(d, sessionId);
        cut.WaitForAssertion(() => Assert.NotNull(cut.Instance));

        var pageType = typeof(SessionDetail);
        var editorKindType = pageType.GetNestedType("SessionEditorKind", BindingFlags.NonPublic);
        Assert.NotNull(editorKindType);
        var publicKind = Enum.Parse(editorKindType!, "Public");
        var worldId = d.ViewModel.World!.Id;

        d.LinkApi.GetSuggestionsAsync(worldId, "castle").Returns(new List<LinkSuggestionDto>
        {
            new()
            {
                ArticleId = Guid.NewGuid(),
                Title = "Castle Ward",
                Slug = "castle-ward",
                DisplayPath = "Waterdeep/Castle Ward"
            }
        });

        d.ExternalLinkApi.GetSuggestionsAsync(worldId, "srd", "fire", Arg.Any<CancellationToken>())
            .Returns(new List<ExternalLinkSuggestionDto>
            {
                new()
                {
                    Source = "srd",
                    Id = "spells/fireball",
                    Title = "Fireball",
                    Subtitle = "Spell",
                    Category = "spell"
                }
            });

        await InvokeNonPublicTask(cut.Instance, "HandleAutocompleteTriggeredAsync", publicKind, "castle", 10d, 20d);
        await InvokeNonPublicTask(cut.Instance, "HandleAutocompleteTriggeredAsync", publicKind, "srd/fire", 10d, 20d);

        await d.LinkApi.Received(1).GetSuggestionsAsync(worldId, "castle");
        await d.ExternalLinkApi.Received(1).GetSuggestionsAsync(worldId, "srd", "fire", Arg.Any<CancellationToken>());
        await d.MapApi.DidNotReceive().GetMapAutocompleteAsync(Arg.Any<Guid>(), Arg.Any<string?>());
    }

    [Fact]
    public async Task SessionDetail_PrivateHandlers_AndHelpers_AreCovered()
    {
        var d = CreatePageDeps();
        var sessionId = Guid.NewGuid();
        ConfigureLoadedSession(d, sessionId, isGm: true, publicNotes: "<p>pub</p>", privateNotes: "<p>priv</p>");

        var cut = RenderPage(d, sessionId);
        cut.WaitForAssertion(() => Assert.NotNull(cut.Instance));

        var instance = cut.Instance;
        var pageType = typeof(SessionDetail);
        var editorKindType = pageType.GetNestedType("SessionEditorKind", BindingFlags.NonPublic);
        Assert.NotNull(editorKindType);
        var publicKind = Enum.Parse(editorKindType!, "Public");
        var privateKind = Enum.Parse(editorKindType!, "Private");

        // OnViewModelChanged
        InvokeNonPublic(instance, "OnViewModelChanged", null, new PropertyChangedEventArgs(nameof(SessionDetailViewModel.EditName)));

        // Date change normalizes to Date
        await InvokeNonPublicTask(instance, "OnSessionDateChanged", new DateTime(2026, 2, 25, 14, 30, 0));
        Assert.Equal(new DateTime(2026, 2, 25), d.ViewModel.EditSessionDate);
        await InvokeNonPublicTask(instance, "OnSessionDateChanged", new object?[] { null });
        Assert.Null(d.ViewModel.EditSessionDate);

        // Editor update guard and both editor branches
        SetField(d.ViewModel, "_isCurrentUserGm", false);
        d.ViewModel.EditPublicNotes = "orig-public";
        await InvokeNonPublicTask(instance, "HandleEditorUpdateAsync", publicKind, "<p>ignored</p>");
        Assert.Equal("orig-public", d.ViewModel.EditPublicNotes);

        SetField(d.ViewModel, "_isCurrentUserGm", true);
        await InvokeNonPublicTask(instance, "HandleEditorUpdateAsync", publicKind, "<p>updated-public</p>");
        await InvokeNonPublicTask(instance, "HandleEditorUpdateAsync", privateKind, "<p>updated-private</p>");
        Assert.Equal("<p>updated-public</p>", d.ViewModel.EditPublicNotes);
        Assert.Equal("<p>updated-private</p>", d.ViewModel.EditPrivateNotes);

        // Autocomplete hide mismatched and matched
        SetField(instance, "_activeEditorKind", publicKind);
        SetField(instance, "_showAutocomplete", true);
        SetField(instance, "_autocompleteSuggestions", new List<Chronicis.Client.Components.Articles.WikiLinkAutocompleteItem>
        {
            Chronicis.Client.Components.Articles.WikiLinkAutocompleteItem.FromInternal(new LinkSuggestionDto
            {
                ArticleId = Guid.NewGuid(),
                Title = "A",
                Slug = "a",
                DisplayPath = "A"
            })
        });
        SetField(instance, "_autocompleteIsExternalQuery", true);
        SetField(instance, "_autocompleteExternalSourceKey", "srd");

        await InvokeNonPublicTask(instance, "HandleAutocompleteHiddenAsync", privateKind);
        Assert.True((bool)GetField(instance, "_showAutocomplete")!);

        await InvokeNonPublicTask(instance, "HandleAutocompleteHiddenAsync", publicKind);
        Assert.False((bool)GetField(instance, "_showAutocomplete")!);
        Assert.False((bool)GetField(instance, "_autocompleteIsExternalQuery")!);
        Assert.Null(GetField(instance, "_autocompleteExternalSourceKey"));

        // Arrow navigation empty + populated
        SetField(instance, "_autocompleteSuggestions", new List<Chronicis.Client.Components.Articles.WikiLinkAutocompleteItem>());
        await InvokeNonPublicTask(instance, "HandleAutocompleteArrowDownAsync", publicKind);
        await InvokeNonPublicTask(instance, "HandleAutocompleteArrowUpAsync", publicKind);

        SetField(instance, "_autocompleteSuggestions", new List<Chronicis.Client.Components.Articles.WikiLinkAutocompleteItem>
        {
            Chronicis.Client.Components.Articles.WikiLinkAutocompleteItem.FromInternal(new LinkSuggestionDto
            {
                ArticleId = Guid.NewGuid(),
                Title = "One",
                Slug = "one",
                DisplayPath = "One"
            }),
            Chronicis.Client.Components.Articles.WikiLinkAutocompleteItem.FromInternal(new LinkSuggestionDto
            {
                ArticleId = Guid.NewGuid(),
                Title = "Two",
                Slug = "two",
                DisplayPath = "Two"
            })
        });
        SetField(instance, "_autocompleteSelectedIndex", 0);
        await InvokeNonPublicTask(instance, "HandleAutocompleteArrowDownAsync", publicKind);
        Assert.Equal(1, (int)GetField(instance, "_autocompleteSelectedIndex")!);
        await InvokeNonPublicTask(instance, "HandleAutocompleteArrowUpAsync", publicKind);
        Assert.Equal(0, (int)GetField(instance, "_autocompleteSelectedIndex")!);

        await cut.InvokeAsync(() => InvokeNonPublicTask(instance, "OnAutocompleteIndexChanged", 1));
        Assert.Equal(1, (int)GetField(instance, "_autocompleteSelectedIndex")!);

        // Static helpers
        Assert.Equal("Not set", InvokeStatic(pageType, "FormatSessionDate", new object?[] { null }) as string);
        var formatted = (string)InvokeStatic(pageType, "FormatSessionDate", new object?[] { DateTime.UtcNow })!;
        Assert.False(string.IsNullOrWhiteSpace(formatted));

        Assert.Equal(Color.Success, InvokeStatic(pageType, "GetVisibilityColor", new object?[] { ArticleVisibility.Public }));
        Assert.Equal(Color.Warning, InvokeStatic(pageType, "GetVisibilityColor", new object?[] { ArticleVisibility.MembersOnly }));
        Assert.Equal(Color.Error, InvokeStatic(pageType, "GetVisibilityColor", new object?[] { ArticleVisibility.Private }));
        Assert.Equal(Color.Default, InvokeStatic(pageType, "GetVisibilityColor", new object?[] { (ArticleVisibility)999 }));

        // Parser helper branches
        Assert.False(CallTryParse(pageType, null, out _, out _));
        Assert.False(CallTryParse(pageType, "noslash", out _, out _));
        Assert.False(CallTryParse(pageType, "/starts-with-slash", out _, out _));
        Assert.False(CallTryParse(pageType, "  /abc", out _, out _));
        Assert.True(CallTryParse(pageType, " SRD/acid", out var source, out var remainder));
        Assert.Equal("srd", source);
        Assert.Equal("acid", remainder);

        // Broken link and preview close
        await InvokeNonPublicTask(instance, "OnBrokenLinkClicked", "missing-id");

        d.ExternalLinkApi.GetContentAsync("srd", "acid-arrow", Arg.Any<CancellationToken>())
            .Returns(new ExternalLinkContentDto
            {
                Source = "srd",
                Id = "acid-arrow",
                Title = "Acid Arrow",
                Kind = "Spell",
                Markdown = "content"
            });
        d.DrawerCoordinator.Open(DrawerType.Metadata);
        await cut.InvokeAsync(() => InvokeNonPublicTask(instance, "OnExternalLinkClicked", "srd", "acid-arrow", "Acid Arrow"));
        Assert.Equal(DrawerType.None, d.DrawerCoordinator.Current);
        Assert.True((bool)GetField(instance, "_externalPreviewOpen")!);

        d.DrawerCoordinator.Open(DrawerType.Quests);
        cut.WaitForAssertion(() => Assert.False((bool)GetField(instance, "_externalPreviewOpen")!));

        SetField(instance, "_externalPreviewOpen", true);
        await cut.InvokeAsync(() =>
        {
            InvokeNonPublic(instance, "CloseExternalPreview");
            return Task.CompletedTask;
        });
        Assert.False((bool)GetField(instance, "_externalPreviewOpen")!);

        // Bridge forwards (nested private class in same file)
        var bridge = GetField(instance, "_publicEditorBridge");
        Assert.NotNull(bridge);
        await cut.InvokeAsync(() => InvokeAnyTask(bridge!, "OnEditorUpdate", "<p>bridge</p>"));
        await cut.InvokeAsync(() => InvokeAnyTask(bridge!, "OnAutocompleteTriggered", "ab", 1d, 2d));
        await cut.InvokeAsync(() => InvokeAnyTask(bridge!, "OnAutocompleteHidden"));
        await cut.InvokeAsync(() => InvokeAnyTask(bridge!, "OnAutocompleteArrowDown"));
        await cut.InvokeAsync(() => InvokeAnyTask(bridge!, "OnAutocompleteArrowUp"));
        await cut.InvokeAsync(() => InvokeAnyTask(bridge!, "OnAutocompleteEnter"));
        await cut.InvokeAsync(() => InvokeAnyTask(bridge!, "OnWikiLinkClicked", "not-a-guid"));
        await cut.InvokeAsync(() => InvokeAnyTask(bridge!, "OnBrokenLinkClicked", "missing"));
        await cut.InvokeAsync(() => InvokeAnyTask(bridge!, "OnExternalLinkClicked", "", "", ""));
        _ = bridge!.GetType().GetMethod("GetArticlePath")!.Invoke(bridge, new object?[] { "not-a-guid" });
        _ = bridge.GetType().GetMethod("GetArticleSummaryPreview")!.Invoke(bridge, new object?[] { "not-a-guid" });
        Assert.Equal("chronicis-image:doc-123", instance.GetImageProxyUrl("doc-123"));
        await cut.InvokeAsync(() =>
        {
            instance.OnImageUploadStarted("map.png");
            instance.OnImageUploadError("upload failed");
            return Task.CompletedTask;
        });

        SetField(instance, "_publicEditorBridgeRef", null);
        SetField(instance, "_privateEditorBridgeRef", null);
        cut.Dispose();
    }

    private static void ConfigureLoadedSession(
        PageDeps d,
        Guid sessionId,
        bool isGm,
        string publicNotes,
        string privateNotes = "")
    {
        var userId = Guid.NewGuid();
        var worldId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var arcId = Guid.NewGuid();

        d.SessionApi.GetSessionAsync(sessionId).Returns(new SessionDto
        {
            Id = sessionId,
            ArcId = arcId,
            Name = "Session Name",
            SessionDate = new DateTime(2026, 2, 25),
            PublicNotes = publicNotes,
            PrivateNotes = privateNotes
        });
        d.ArcApi.GetArcAsync(arcId).Returns(new ArcDto { Id = arcId, CampaignId = campaignId, Name = "Arc" });
        d.CampaignApi.GetCampaignAsync(campaignId).Returns(new CampaignDetailDto { Id = campaignId, WorldId = worldId, Name = "Campaign" });
        d.WorldApi.GetWorldAsync(worldId).Returns(new WorldDetailDto
        {
            Id = worldId,
            Name = "World",
            Members = new List<WorldMemberDto>
            {
                new()
                {
                    UserId = userId,
                    Email = "user@example.com",
                    Role = isGm ? WorldRole.GM : WorldRole.Player
                }
            }
        });
        d.AuthService.GetCurrentUserAsync().Returns(new UserInfo
        {
            Email = "user@example.com",
            DisplayName = "User",
            Auth0UserId = "auth0|u"
        });
        d.ArticleApi.GetAllArticlesAsync().Returns(new List<ArticleTreeDto>());
        d.Breadcrumbs.ForArc(Arg.Any<ArcDto>(), Arg.Any<CampaignDto>(), Arg.Any<WorldDto>(), currentDisabled: false)
            .Returns(new List<BreadcrumbItem> { new("Dashboard", "/dashboard") });
    }

    private static object? GetField(object instance, string name)
        => instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(instance);

    private static void SetField(object instance, string name, object? value)
        => instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(instance, value);

    private static void InvokeNonPublic(object instance, string methodName, params object?[]? args)
        => instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(instance, args);

    private static async Task InvokeNonPublicTask(object instance, string methodName, params object?[] args)
    {
        var result = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(instance, args);
        if (result is Task task)
        {
            await task;
        }
    }

    private static object? InvokeStatic(Type type, string methodName, object?[]? args)
        => type.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, args);

    private static async Task InvokeAnyTask(object instance, string methodName, params object?[]? args)
    {
        var result = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.Invoke(instance, args);
        if (result is Task task)
        {
            await task;
        }
    }

    private static bool CallTryParse(Type pageType, string? query, out string source, out string remainder)
    {
        var args = new object?[] { query!, null, null };
        var result = (bool)InvokeStatic(pageType, "TryParseExternalAutocompleteQuery", args)!;
        source = args[1] as string ?? string.Empty;
        remainder = args[2] as string ?? string.Empty;
        return result;
    }
}
