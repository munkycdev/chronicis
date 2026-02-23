using Bunit;
using Bunit.TestDoubles;
using Chronicis.Client.Abstractions;
using Chronicis.Client.Pages;
using Chronicis.Client.Services;
using Chronicis.Client.ViewModels;
using Chronicis.Shared.DTOs;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MudBlazor;
using MudBlazor.Services;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Pages;

public class WorldDetailTests : Bunit.TestContext
{
    private IWorldApiService _worldApi = null!;
    private ITreeStateService _treeState = null!;
    private IBreadcrumbService _breadcrumbService = null!;
    private IAppNavigator _navigator = null!;
    private IUserNotifier _notifier = null!;
    private IPageTitleService _titleService = null!;
    private IDialogService _dialogService = null!;
    private AuthenticationStateProvider _authProvider = null!;

    public WorldDetailTests()
    {
        _worldApi = Substitute.For<IWorldApiService>();
        _treeState = Substitute.For<ITreeStateService>();
        _breadcrumbService = Substitute.For<IBreadcrumbService>();
        _navigator = Substitute.For<IAppNavigator>();
        _notifier = Substitute.For<IUserNotifier>();
        _titleService = Substitute.For<IPageTitleService>();
        _dialogService = Substitute.For<IDialogService>();
        _authProvider = Substitute.For<AuthenticationStateProvider>();

        var authState = new AuthenticationState(new System.Security.Claims.ClaimsPrincipal());
        _authProvider.GetAuthenticationStateAsync().Returns(Task.FromResult(authState));

        // Default API returns empty collections
        _worldApi.GetWorldLinksAsync(Arg.Any<Guid>()).Returns(new List<WorldLinkDto>());
        _worldApi.GetWorldDocumentsAsync(Arg.Any<Guid>()).Returns(new List<WorldDocumentDto>());

        Services.AddMudServices();
        Services.AddSingleton(_worldApi);
        Services.AddSingleton(_treeState);
        Services.AddSingleton(_breadcrumbService);
        Services.AddSingleton(_navigator);
        Services.AddSingleton(_notifier);
        Services.AddSingleton(_titleService);
        Services.AddSingleton(_dialogService);
        Services.AddSingleton(_authProvider);
        Services.AddSingleton(Substitute.For<ICampaignApiService>());
        Services.AddSingleton(Substitute.For<ILogger<WorldDetailViewModel>>());
        Services.AddSingleton(Substitute.For<ILogger<WorldLinksViewModel>>());
        Services.AddSingleton(Substitute.For<ILogger<WorldDocumentsViewModel>>());
        Services.AddSingleton(Substitute.For<ILogger<WorldSharingViewModel>>());

        Services.AddTransient<WorldDetailViewModel>();
        Services.AddTransient<WorldLinksViewModel>();
        Services.AddTransient<WorldDocumentsViewModel>();
        Services.AddTransient<WorldSharingViewModel>();

        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("test-user");

        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    // ---------------------------------------------------------------------------
    // Loading state
    // ---------------------------------------------------------------------------

    [Fact]
    public void WorldDetail_WhenLoading_ShowsLoadingSkeleton()
    {
        var tcs = new TaskCompletionSource<WorldDetailDto?>();
        _worldApi.GetWorldAsync(Arg.Any<Guid>()).Returns(tcs.Task);

        var cut = RenderComponent<WorldDetail>(p => p.Add(x => x.WorldId, Guid.NewGuid()));

        Assert.Contains("chronicis-loading-skeleton", cut.Markup);
    }

    // ---------------------------------------------------------------------------
    // World not found
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task WorldDetail_WhenWorldNull_NavigatesToDashboard()
    {
        _worldApi.GetWorldAsync(Arg.Any<Guid>()).Returns((WorldDetailDto?)null);

        var cut = RenderComponent<WorldDetail>(p => p.Add(x => x.WorldId, Guid.NewGuid()));
        await cut.InvokeAsync(() => Task.CompletedTask);

        _navigator.Received(1).NavigateTo("/dashboard", replace: true);
    }

    // ---------------------------------------------------------------------------
    // Loaded state
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task WorldDetail_WhenWorldLoaded_ShowsWorldName()
    {
        var world = new WorldDetailDto
        {
            Id = Guid.NewGuid(),
            Name = "Faerûn",
            CreatedAt = DateTime.UtcNow,
            Members = new List<WorldMemberDto>()
        };
        _worldApi.GetWorldAsync(world.Id).Returns(world);

        var cut = RenderComponent<WorldDetail>(p => p.Add(x => x.WorldId, world.Id));
        await cut.InvokeAsync(() => Task.CompletedTask);

        Assert.Contains("Faerûn", cut.Markup);
    }
}
