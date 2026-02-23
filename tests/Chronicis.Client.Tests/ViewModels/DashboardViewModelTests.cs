using Chronicis.Client.Abstractions;
using Chronicis.Client.Services;
using Chronicis.Client.ViewModels;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.Logging;
using MudBlazor;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Chronicis.Client.Tests.ViewModels;

public class DashboardViewModelTests
{
    private record SutComponents(
        DashboardViewModel Sut,
        IDashboardApiService DashboardApi,
        IUserApiService UserApi,
        IWorldApiService WorldApi,
        IArticleApiService ArticleApi,
        IQuoteService QuoteService,
        ITreeStateService TreeState,
        IDialogService DialogService,
        IAppNavigator Navigator,
        IUserNotifier Notifier);

    private static SutComponents CreateSut()
    {
        var dashboardApi = Substitute.For<IDashboardApiService>();
        var userApi = Substitute.For<IUserApiService>();
        var worldApi = Substitute.For<IWorldApiService>();
        var articleApi = Substitute.For<IArticleApiService>();
        var quoteService = Substitute.For<IQuoteService>();
        var treeState = Substitute.For<ITreeStateService>();
        var dialogService = Substitute.For<IDialogService>();
        var navigator = Substitute.For<IAppNavigator>();
        var notifier = Substitute.For<IUserNotifier>();
        var logger = Substitute.For<ILogger<DashboardViewModel>>();

        var sut = new DashboardViewModel(
            dashboardApi, userApi, worldApi, articleApi, quoteService,
            treeState, dialogService, navigator, notifier, logger);

        return new SutComponents(sut, dashboardApi, userApi, worldApi, articleApi,
            quoteService, treeState, dialogService, navigator, notifier);
    }

    private static DashboardDto MakeDashboard(int worldCount = 1) => new()
    {
        UserDisplayName = "Gandalf",
        Worlds = Enumerable.Range(0, worldCount).Select(i => new DashboardWorldDto
        {
            Id = Guid.NewGuid(),
            Name = $"World {i}",
            Campaigns = new(),
            ArticleCount = 10,
            CreatedAt = DateTime.UtcNow.AddDays(-i),
        }).ToList(),
        ClaimedCharacters = new(),
        Prompts = new(),
    };

    // ---------------------------------------------------------------------------
    // Initial state
    // ---------------------------------------------------------------------------

    [Fact]
    public void InitialState_IsLoadingTrue_DashboardNull()
    {
        var c = CreateSut();
        Assert.True(c.Sut.IsLoading);
        Assert.Null(c.Sut.Dashboard);
        Assert.Empty(c.Sut.OrderedWorlds);
        Assert.Null(c.Sut.Quote);
    }

    // ---------------------------------------------------------------------------
    // InitializeAsync
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task InitializeAsync_WhenOnboardingIncomplete_RedirectsAndSkipsLoad()
    {
        var c = CreateSut();
        c.UserApi.GetUserProfileAsync().Returns(new UserProfileDto { HasCompletedOnboarding = false });

        await c.Sut.InitializeAsync();

        c.Navigator.Received(1).NavigateTo("/getting-started", replace: true);
        await c.DashboardApi.DidNotReceive().GetDashboardAsync();
    }

    [Fact]
    public async Task InitializeAsync_WhenOnboardingComplete_LoadsDashboardAndQuote()
    {
        var c = CreateSut();
        c.UserApi.GetUserProfileAsync().Returns(new UserProfileDto { HasCompletedOnboarding = true });
        c.DashboardApi.GetDashboardAsync().Returns(MakeDashboard());
        c.QuoteService.GetRandomQuoteAsync().Returns(new Quote { Content = "q", Author = "a" });

        await c.Sut.InitializeAsync();

        await c.DashboardApi.Received(1).GetDashboardAsync();
        await c.QuoteService.Received(1).GetRandomQuoteAsync();
    }

    [Fact]
    public async Task InitializeAsync_WhenProfileNull_StillLoads()
    {
        var c = CreateSut();
        c.UserApi.GetUserProfileAsync().Returns((UserProfileDto?)null);
        c.DashboardApi.GetDashboardAsync().Returns(MakeDashboard());
        c.QuoteService.GetRandomQuoteAsync().Returns((Quote?)null);

        await c.Sut.InitializeAsync();

        await c.DashboardApi.Received(1).GetDashboardAsync();
    }

    // ---------------------------------------------------------------------------
    // LoadDashboardAsync
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task LoadDashboardAsync_SetsDashboardAndClearsLoading()
    {
        var c = CreateSut();
        var dashboard = MakeDashboard(2);
        c.DashboardApi.GetDashboardAsync().Returns(dashboard);

        await c.Sut.LoadDashboardAsync();

        Assert.Equal(dashboard, c.Sut.Dashboard);
        Assert.False(c.Sut.IsLoading);
    }

    [Fact]
    public async Task LoadDashboardAsync_OrdersWorldsByActiveAndRecency()
    {
        var c = CreateSut();
        var old = new DashboardWorldDto { Id = Guid.NewGuid(), Name = "Old", Campaigns = new(), CreatedAt = DateTime.UtcNow.AddDays(-10) };
        var recent = new DashboardWorldDto { Id = Guid.NewGuid(), Name = "Recent", Campaigns = new(), CreatedAt = DateTime.UtcNow };
        c.DashboardApi.GetDashboardAsync().Returns(new DashboardDto
        {
            UserDisplayName = "X",
            Worlds = new List<DashboardWorldDto> { old, recent },
            ClaimedCharacters = new(),
            Prompts = new(),
        });

        await c.Sut.LoadDashboardAsync();

        Assert.Equal("Recent", c.Sut.OrderedWorlds[0].Name);
        Assert.Equal("Old", c.Sut.OrderedWorlds[1].Name);
    }

    [Fact]
    public async Task LoadDashboardAsync_WhenApiThrows_NotifiesErrorAndClearsLoading()
    {
        var c = CreateSut();
        c.DashboardApi.GetDashboardAsync().ThrowsAsync(new HttpRequestException());

        await c.Sut.LoadDashboardAsync();

        c.Notifier.Received(1).Error(Arg.Any<string>());
        Assert.False(c.Sut.IsLoading);
    }

    // ---------------------------------------------------------------------------
    // LoadQuoteAsync
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task LoadQuoteAsync_SetsQuote()
    {
        var c = CreateSut();
        var quote = new Quote { Content = "The road goes ever on", Author = "Tolkien" };
        c.QuoteService.GetRandomQuoteAsync().Returns(quote);

        await c.Sut.LoadQuoteAsync();

        Assert.Equal(quote, c.Sut.Quote);
    }

    [Fact]
    public async Task LoadQuoteAsync_WhenApiThrows_SilentlySwallows()
    {
        var c = CreateSut();
        c.QuoteService.GetRandomQuoteAsync().ThrowsAsync(new Exception("network"));

        var ex = await Record.ExceptionAsync(() => c.Sut.LoadQuoteAsync());

        Assert.Null(ex);
        Assert.Null(c.Sut.Quote);
    }
}
