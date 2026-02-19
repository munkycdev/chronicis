using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Security.Claims;
using Bunit.TestDoubles;
using Chronicis.Client.Components.Layout;
using Chronicis.Client.Models;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using MudBlazor;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Components.Layout;

[ExcludeFromCodeCoverage]
public class AuthenticatedLayoutTests : MudBlazorTestContext
{
    private readonly ITreeStateService _treeState = Substitute.For<ITreeStateService>();
    private readonly IArticleApiService _articleApi = Substitute.For<IArticleApiService>();
    private readonly ISnackbar _snackbar = Substitute.For<ISnackbar>();
    private readonly IAuthService _authService = Substitute.For<IAuthService>();
    private readonly IAdminAuthService _adminAuthService = Substitute.For<IAdminAuthService>();
    private readonly IMetadataDrawerService _metadataDrawerService = Substitute.For<IMetadataDrawerService>();
    private readonly IQuestDrawerService _questDrawerService = Substitute.For<IQuestDrawerService>();
    private readonly IKeyboardShortcutService _keyboardShortcutService = Substitute.For<IKeyboardShortcutService>();
    private readonly AuthenticationStateProvider _authStateProvider = new TestAuthStateProvider();
    private readonly ILogger<AuthenticatedLayout> _logger = Substitute.For<ILogger<AuthenticatedLayout>>();

    public AuthenticatedLayoutTests()
    {
        Services.AddSingleton(_treeState);
        Services.AddSingleton(_snackbar);
    }

    [Fact]
    public void Component_ImplementsAsyncDisposable()
    {
        Assert.Contains(typeof(IAsyncDisposable), typeof(AuthenticatedLayout).GetInterfaces());
    }

    [Fact]
    public void ToggleDrawer_TogglesState()
    {
        var instance = CreateInstance();
        Assert.True(GetField<bool>(instance, "_drawerOpen"));

        InvokePrivate(instance, "ToggleDrawer");
        Assert.False(GetField<bool>(instance, "_drawerOpen"));
    }

    [Fact]
    public void OnTreeSearchChanged_WithWhitespace_ClearsSearch()
    {
        var instance = CreateInstance();
        SetField(instance, "_treeSearch", "   ");

        InvokePrivate(instance, "OnTreeSearchChanged");

        _treeState.Received(1).ClearSearch();
        _treeState.DidNotReceive().SetSearchQuery(Arg.Any<string>());
    }

    [Fact]
    public void OnTreeSearchChanged_WithValue_SetsQuery()
    {
        var instance = CreateInstance();
        SetField(instance, "_treeSearch", "dragon");

        InvokePrivate(instance, "OnTreeSearchChanged");

        _treeState.Received(1).SetSearchQuery("dragon");
    }

    [Fact]
    public void OnGlobalSearchKeyDown_Enter_Navigates()
    {
        var instance = CreateInstance();
        var nav = Services.GetRequiredService<NavigationManager>() as FakeNavigationManager;
        Assert.NotNull(nav);
        SetField(instance, "_globalSearchQuery", "acid arrow");

        InvokePrivate(instance, "OnGlobalSearchKeyDown", new KeyboardEventArgs { Key = "Enter" });

        Assert.Contains("/search?q=acid%20arrow", nav!.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OnGlobalSearchKeyDown_NonEnter_DoesNotNavigate()
    {
        var instance = CreateInstance();
        var nav = Services.GetRequiredService<NavigationManager>() as FakeNavigationManager;
        Assert.NotNull(nav);
        var before = nav!.Uri;
        SetField(instance, "_globalSearchQuery", "acid arrow");

        InvokePrivate(instance, "OnGlobalSearchKeyDown", new KeyboardEventArgs { Key = "a" });

        Assert.Equal(before, nav.Uri);
    }

    [Fact]
    public void ClearSearch_ResetsTreeSearchAndClearsState()
    {
        var instance = CreateInstance();
        SetField(instance, "_treeSearch", "dragon");

        InvokePrivate(instance, "ClearSearch");

        Assert.Null(GetField<string?>(instance, "_treeSearch"));
        _treeState.Received(1).ClearSearch();
    }

    [Fact]
    public void BeginSignOut_NavigatesToLogout()
    {
        var instance = CreateInstance();
        var nav = Services.GetRequiredService<NavigationManager>() as FakeNavigationManager;
        Assert.NotNull(nav);

        InvokePrivate(instance, "BeginSignOut");

        Assert.Contains("authentication/logout", nav!.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OnCtrlN_WhenNoSelection_ShowsInfo()
    {
        _treeState.SelectedNodeId.Returns((Guid?)null);
        var instance = CreateInstance();

        await InvokePrivateAsync(instance, "OnCtrlN");

        _snackbar.Received().Add(Arg.Is<string>(m => m.Contains("Select an article first", StringComparison.OrdinalIgnoreCase)), Severity.Info);
    }

    [Fact]
    public async Task OnCtrlN_WhenSelectedNodeMissing_ShowsInfo()
    {
        var selectedId = Guid.NewGuid();
        _treeState.SelectedNodeId.Returns((Guid?)selectedId);
        _treeState.TryGetNode(selectedId, out Arg.Any<TreeNode?>())
            .Returns(x =>
            {
                x[1] = null;
                return false;
            });

        var instance = CreateInstance();
        await InvokePrivateAsync(instance, "OnCtrlN");

        _snackbar.Received().Add(Arg.Is<string>(m => m.Contains("Select an article first", StringComparison.OrdinalIgnoreCase)), Severity.Info);
    }

    [Fact]
    public async Task OnCtrlN_WhenSelectedNodeNotArticle_ShowsTypeSpecificMessage()
    {
        var selectedId = Guid.NewGuid();
        _treeState.SelectedNodeId.Returns((Guid?)selectedId);
        var node = new TreeNode { Id = selectedId, NodeType = TreeNodeType.World };
        _treeState.TryGetNode(selectedId, out Arg.Any<TreeNode?>())
            .Returns(x =>
            {
                x[1] = node;
                return true;
            });

        var instance = CreateInstance();
        await InvokePrivateAsync(instance, "OnCtrlN");

        _snackbar.Received().Add(Arg.Is<string>(m => m.Contains("instead of a World", StringComparison.OrdinalIgnoreCase)), Severity.Info);
    }

    [Fact]
    public async Task OnCtrlN_WhenSelectedArticleNotLoaded_ShowsWarning()
    {
        var selectedId = Guid.NewGuid();
        _treeState.SelectedNodeId.Returns((Guid?)selectedId);
        var node = new TreeNode { Id = selectedId, NodeType = TreeNodeType.Article };
        _treeState.TryGetNode(selectedId, out Arg.Any<TreeNode?>())
            .Returns(x =>
            {
                x[1] = node;
                return true;
            });
        _articleApi.GetArticleDetailAsync(selectedId).Returns((ArticleDto?)null);

        var instance = CreateInstance();
        await InvokePrivateAsync(instance, "OnCtrlN");

        _snackbar.Received().Add("Could not load the selected article", Severity.Warning);
    }

    [Fact]
    public async Task OnCtrlN_WhenChildCreateFails_ShowsError()
    {
        var selectedId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        _treeState.SelectedNodeId.Returns((Guid?)selectedId);
        var node = new TreeNode { Id = selectedId, NodeType = TreeNodeType.Article };
        _treeState.TryGetNode(selectedId, out Arg.Any<TreeNode?>())
            .Returns(x =>
            {
                x[1] = node;
                return true;
            });
        _articleApi.GetArticleDetailAsync(selectedId).Returns(new ArticleDto { Id = selectedId, ParentId = parentId });
        _treeState.CreateChildArticleAsync(parentId).Returns((Guid?)null);

        var instance = CreateInstance();
        await InvokePrivateAsync(instance, "OnCtrlN");

        _snackbar.Received().Add("Failed to create article", Severity.Error);
    }

    [Fact]
    public async Task OnCtrlN_WhenChildCreateSucceeds_ShowsSuccessAndNavigates()
    {
        var selectedId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var newId = Guid.NewGuid();

        _treeState.SelectedNodeId.Returns((Guid?)selectedId);
        var node = new TreeNode { Id = selectedId, NodeType = TreeNodeType.Article };
        _treeState.TryGetNode(selectedId, out Arg.Any<TreeNode?>())
            .Returns(x =>
            {
                x[1] = node;
                return true;
            });
        _articleApi.GetArticleDetailAsync(selectedId).Returns(new ArticleDto { Id = selectedId, ParentId = parentId });
        _treeState.CreateChildArticleAsync(parentId).Returns((Guid?)newId);
        _articleApi.GetArticleDetailAsync(newId).Returns(new ArticleDto
        {
            Id = newId,
            Breadcrumbs = new List<BreadcrumbDto>
            {
                new() { Slug = "world" },
                new() { Slug = "new-article" }
            }
        });

        var instance = CreateInstance();
        var nav = Services.GetRequiredService<NavigationManager>() as FakeNavigationManager;
        Assert.NotNull(nav);

        await InvokePrivateAsync(instance, "OnCtrlN");

        _snackbar.Received().Add("Article created (Ctrl+N)", Severity.Success);
        Assert.Contains("/article/world/new-article", nav!.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OnCtrlN_WhenRootCreateSucceeds_RefreshesSelectsAndNavigates()
    {
        var selectedId = Guid.NewGuid();
        var newId = Guid.NewGuid();

        _treeState.SelectedNodeId.Returns((Guid?)selectedId);
        var node = new TreeNode { Id = selectedId, NodeType = TreeNodeType.Article };
        _treeState.TryGetNode(selectedId, out Arg.Any<TreeNode?>())
            .Returns(x =>
            {
                x[1] = node;
                return true;
            });
        _articleApi.GetArticleDetailAsync(selectedId).Returns(new ArticleDto
        {
            Id = selectedId,
            ParentId = null,
            WorldId = Guid.NewGuid(),
            CampaignId = Guid.NewGuid(),
            ArcId = Guid.NewGuid(),
            Type = ArticleType.WikiArticle
        });
        _articleApi.CreateArticleAsync(Arg.Any<ArticleCreateDto>())
            .Returns(new ArticleDto { Id = newId });
        _articleApi.GetArticleDetailAsync(newId).Returns(new ArticleDto
        {
            Id = newId,
            Breadcrumbs = new List<BreadcrumbDto>
            {
                new() { Slug = "world" },
                new() { Slug = "root-sibling" }
            }
        });

        var instance = CreateInstance();
        var nav = Services.GetRequiredService<NavigationManager>() as FakeNavigationManager;
        Assert.NotNull(nav);

        await InvokePrivateAsync(instance, "OnCtrlN");

        await _treeState.Received(1).RefreshAsync();
        _treeState.Received(1).SelectNode(newId);
        Assert.Contains("/article/world/root-sibling", nav!.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OnCtrlN_WhenRootCreateReturnsNull_ShowsError()
    {
        var selectedId = Guid.NewGuid();

        _treeState.SelectedNodeId.Returns((Guid?)selectedId);
        var node = new TreeNode { Id = selectedId, NodeType = TreeNodeType.Article };
        _treeState.TryGetNode(selectedId, out Arg.Any<TreeNode?>())
            .Returns(x =>
            {
                x[1] = node;
                return true;
            });
        _articleApi.GetArticleDetailAsync(selectedId).Returns(new ArticleDto
        {
            Id = selectedId,
            ParentId = null,
            WorldId = Guid.NewGuid(),
            CampaignId = Guid.NewGuid(),
            ArcId = Guid.NewGuid(),
            Type = ArticleType.WikiArticle
        });
        _articleApi.CreateArticleAsync(Arg.Any<ArticleCreateDto>())
            .Returns((ArticleDto?)null);

        var instance = CreateInstance();
        await InvokePrivateAsync(instance, "OnCtrlN");

        _snackbar.Received().Add("Failed to create article", Severity.Error);
    }

    [Fact]
    public async Task OnInitializedAsync_LoadsCurrentUserAndAdminFlag()
    {
        var instance = CreateInstance();
        _authService.GetCurrentUserAsync().Returns(new UserInfo
        {
            DisplayName = "Test User",
            Email = "test@example.com"
        });
        _adminAuthService.IsSysAdminAsync().Returns(true);

        await InvokePrivateAsync(instance, "OnInitializedAsync");

        Assert.NotNull(GetField<UserInfo?>(instance, "_currentUser"));
        Assert.True(GetField<bool>(instance, "_isSysAdmin"));
    }

    [Fact]
    public async Task OnAfterRenderAsync_FirstRender_InitializesKeyboardShortcuts()
    {
        var instance = CreateInstance();

        await InvokePrivateAsync(instance, "OnAfterRenderAsync", true);

        Assert.NotNull(GetField<object?>(instance, "_dotNetRef"));
    }

    [Fact]
    public async Task OnAfterRenderAsync_NonFirstRender_DoesNotInitializeRef()
    {
        var instance = CreateInstance();

        await InvokePrivateAsync(instance, "OnAfterRenderAsync", false);

        Assert.Null(GetField<object?>(instance, "_dotNetRef"));
    }

    [Fact]
    public void OnCtrlM_TogglesMetadataDrawer()
    {
        var instance = CreateInstance();

        InvokePrivate(instance, "OnCtrlM");

        _metadataDrawerService.Received(1).Toggle();
    }

    [Fact]
    public void OnCtrlQ_TogglesQuestDrawer()
    {
        var instance = CreateInstance();

        InvokePrivate(instance, "OnCtrlQ");

        _questDrawerService.Received(1).Toggle();
    }

    [Fact]
    public void OnCtrlS_RequestsSave()
    {
        var instance = CreateInstance();

        InvokePrivate(instance, "OnCtrlS");

        _keyboardShortcutService.Received(1).RequestSave();
    }

    [Fact]
    public void ExecuteGlobalSearch_WithWhitespace_DoesNotNavigate()
    {
        var instance = CreateInstance();
        var nav = Services.GetRequiredService<NavigationManager>() as FakeNavigationManager;
        Assert.NotNull(nav);
        var before = nav!.Uri;
        SetField(instance, "_globalSearchQuery", "   ");

        InvokePrivate(instance, "ExecuteGlobalSearch");

        Assert.Equal(before, nav.Uri);
    }

    [Fact]
    public async Task DisposeAsync_WithNoDotNetRef_DoesNotThrow()
    {
        var instance = CreateInstance();

        await InvokePrivateAsync(instance, "DisposeAsync");

        Assert.Null(GetField<object?>(instance, "_dotNetRef"));
    }

    [Fact]
    public async Task DisposeAsync_WhenJsDisposeThrows_StillDisposesReference()
    {
        var instance = CreateInstance();
        var js = Substitute.For<IJSRuntime>();
        js.InvokeAsync<object>(Arg.Any<string>(), Arg.Any<object?[]>())
            .Returns(_ => ValueTask.FromException<object>(new InvalidOperationException("js gone")));
        SetProperty(instance, "JSRuntime", js);
        SetField(instance, "_dotNetRef", DotNetObjectReference.Create(instance));

        await InvokePrivateAsync(instance, "DisposeAsync");

        var reference = GetField<DotNetObjectReference<AuthenticatedLayout>?>(instance, "_dotNetRef");
        Assert.NotNull(reference);
        Assert.Throws<ObjectDisposedException>(() => _ = reference!.Value);
    }

    private AuthenticatedLayout CreateInstance()
    {
        var instance = new AuthenticatedLayout();
        SetProperty(instance, "TreeState", _treeState);
        SetProperty(instance, "Snackbar", _snackbar);
        SetProperty(instance, "Navigation", Services.GetRequiredService<NavigationManager>());
        SetProperty(instance, "ArticleApi", _articleApi);
        SetProperty(instance, "AuthService", _authService);
        SetProperty(instance, "AdminAuthService", _adminAuthService);
        SetProperty(instance, "AuthenticationStateProvider", _authStateProvider);
        SetProperty(instance, "JSRuntime", JSInterop.JSRuntime);
        SetProperty(instance, "MetadataDrawerService", _metadataDrawerService);
        SetProperty(instance, "QuestDrawerService", _questDrawerService);
        SetProperty(instance, "KeyboardShortcutService", _keyboardShortcutService);
        SetProperty(instance, "Logger", _logger);
        return instance;
    }

    private static void SetProperty(object instance, string propertyName, object value)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.NotNull(property);
        property!.SetValue(instance, value);
    }

    private static void SetField(object instance, string fieldName, object? value)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field!.SetValue(instance, value);
    }

    private static T GetField<T>(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return (T)field!.GetValue(instance)!;
    }

    private static object? InvokePrivate(object instance, string methodName, params object[] args)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.NotNull(method);
        return method!.Invoke(instance, args);
    }

    private static async Task InvokePrivateAsync(object instance, string methodName, params object[] args)
    {
        var result = InvokePrivate(instance, methodName, args);
        if (result is Task task)
        {
            await task;
        }
    }

    private sealed class TestAuthStateProvider : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var identity = new ClaimsIdentity();
            var user = new ClaimsPrincipal(identity);
            return Task.FromResult(new AuthenticationState(user));
        }
    }
}
