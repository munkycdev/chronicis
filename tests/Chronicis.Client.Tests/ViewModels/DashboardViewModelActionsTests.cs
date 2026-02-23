using Chronicis.Client.Abstractions;
using Chronicis.Client.Components.Dialogs;
using Chronicis.Client.Services;
using Chronicis.Client.ViewModels;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.Logging;
using MudBlazor;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Chronicis.Client.Tests.ViewModels;

public class DashboardViewModelActionsTests
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

    // ---------------------------------------------------------------------------
    // CreateNewWorldAsync
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task CreateNewWorldAsync_Success_WithRootArticle_NavigatesToWorld()
    {
        var c = CreateSut();
        var world = new WorldDto
        {
            Name = "Middle-Earth",
            Slug = "middle-earth",
            WorldRootArticleId = Guid.NewGuid()
        };
        c.WorldApi.CreateWorldAsync(Arg.Any<WorldCreateDto>()).Returns(world);

        await c.Sut.CreateNewWorldAsync();

        c.Notifier.Received(1).Success(Arg.Is<string>(s => s.Contains("Middle-Earth")));
        await c.TreeState.Received(1).RefreshAsync();
        Assert.True(c.TreeState.ShouldFocusTitle);
        c.Navigator.Received(1).NavigateTo("/world/middle-earth");
    }

    [Fact]
    public async Task CreateNewWorldAsync_Success_WithoutRootArticle_ReloadsDashboard()
    {
        var c = CreateSut();
        var world = new WorldDto { Name = "Void", Slug = "void", WorldRootArticleId = null };
        c.WorldApi.CreateWorldAsync(Arg.Any<WorldCreateDto>()).Returns(world);
        c.DashboardApi.GetDashboardAsync().Returns(new DashboardDto
        {
            UserDisplayName = "X",
            Worlds = new(),
            ClaimedCharacters = new(),
            Prompts = new()
        });

        await c.Sut.CreateNewWorldAsync();

        await c.DashboardApi.Received(1).GetDashboardAsync();
        c.Navigator.DidNotReceive().NavigateTo(Arg.Any<string>());
    }

    [Fact]
    public async Task CreateNewWorldAsync_WhenApiReturnsNull_NotifiesError()
    {
        var c = CreateSut();
        c.WorldApi.CreateWorldAsync(Arg.Any<WorldCreateDto>()).Returns((WorldDto?)null);

        await c.Sut.CreateNewWorldAsync();

        c.Notifier.Received(1).Error(Arg.Any<string>());
        c.Navigator.DidNotReceive().NavigateTo(Arg.Any<string>());
    }

    [Fact]
    public async Task CreateNewWorldAsync_WhenApiThrows_NotifiesError()
    {
        var c = CreateSut();
        c.WorldApi.CreateWorldAsync(Arg.Any<WorldCreateDto>())
            .ThrowsAsync(new HttpRequestException("fail"));

        await c.Sut.CreateNewWorldAsync();

        c.Notifier.Received(1).Error(Arg.Any<string>());
    }

    // ---------------------------------------------------------------------------
    // JoinWorldAsync
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task JoinWorldAsync_WhenConfirmedWithWorldId_NotifiesAndNavigates()
    {
        var c = CreateSut();
        var joinResult = new WorldJoinResultDto { WorldName = "Faerûn", WorldId = Guid.NewGuid() };
        var dialogRef = Substitute.For<IDialogReference>();
        dialogRef.Result.Returns(Task.FromResult(
            DialogResult.Ok<WorldJoinResultDto>(joinResult)));
        c.DialogService.ShowAsync<JoinWorldDialog>(Arg.Any<string>()).Returns(dialogRef);
        c.DashboardApi.GetDashboardAsync().Returns(new DashboardDto
        {
            UserDisplayName = "X",
            Worlds = new(),
            ClaimedCharacters = new(),
            Prompts = new()
        });

        await c.Sut.JoinWorldAsync();

        c.Notifier.Received(1).Success(Arg.Is<string>(s => s.Contains("Faerûn")));
        await c.TreeState.Received(1).RefreshAsync();
        c.Navigator.Received(1).NavigateTo($"/world/{joinResult.WorldId}");
    }

    [Fact]
    public async Task JoinWorldAsync_WhenConfirmedWithoutWorldId_DoesNotNavigate()
    {
        var c = CreateSut();
        var joinResult = new WorldJoinResultDto { WorldName = "Nowhere", WorldId = null };
        var dialogRef = Substitute.For<IDialogReference>();
        dialogRef.Result.Returns(Task.FromResult(
            DialogResult.Ok<WorldJoinResultDto>(joinResult)));
        c.DialogService.ShowAsync<JoinWorldDialog>(Arg.Any<string>()).Returns(dialogRef);
        c.DashboardApi.GetDashboardAsync().Returns(new DashboardDto
        {
            UserDisplayName = "X",
            Worlds = new(),
            ClaimedCharacters = new(),
            Prompts = new()
        });

        await c.Sut.JoinWorldAsync();

        c.Navigator.DidNotReceive().NavigateTo(Arg.Any<string>());
    }

    [Fact]
    public async Task JoinWorldAsync_WhenCancelled_DoesNothing()
    {
        var c = CreateSut();
        var dialogRef = Substitute.For<IDialogReference>();
        dialogRef.Result.Returns(Task.FromResult(DialogResult.Cancel()));
        c.DialogService.ShowAsync<JoinWorldDialog>(Arg.Any<string>()).Returns(dialogRef);

        await c.Sut.JoinWorldAsync();

        c.Notifier.DidNotReceive().Success(Arg.Any<string>());
        c.Navigator.DidNotReceive().NavigateTo(Arg.Any<string>());
    }

    // ---------------------------------------------------------------------------
    // NavigateToCharacterAsync
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task NavigateToCharacterAsync_WhenArticleHasBreadcrumbs_NavigatesWithPath()
    {
        var c = CreateSut();
        var id = Guid.NewGuid();
        c.ArticleApi.GetArticleDetailAsync(id).Returns(new ArticleDto
        {
            Breadcrumbs = new List<BreadcrumbDto>
            {
                new() { Slug = "world" },
                new() { Slug = "characters" },
                new() { Slug = "aragorn" }
            }
        });

        await c.Sut.NavigateToCharacterAsync(id);

        c.Navigator.Received(1).NavigateTo("/article/world/characters/aragorn");
    }

    [Fact]
    public async Task NavigateToCharacterAsync_WhenArticleIsNull_DoesNotNavigate()
    {
        var c = CreateSut();
        c.ArticleApi.GetArticleDetailAsync(Arg.Any<Guid>()).Returns((ArticleDto?)null);

        await c.Sut.NavigateToCharacterAsync(Guid.NewGuid());

        c.Navigator.DidNotReceive().NavigateTo(Arg.Any<string>());
    }

    [Fact]
    public async Task NavigateToCharacterAsync_WhenBreadcrumbsEmpty_DoesNotNavigate()
    {
        var c = CreateSut();
        c.ArticleApi.GetArticleDetailAsync(Arg.Any<Guid>()).Returns(
            new ArticleDto { Breadcrumbs = new() });

        await c.Sut.NavigateToCharacterAsync(Guid.NewGuid());

        c.Navigator.DidNotReceive().NavigateTo(Arg.Any<string>());
    }

    // ---------------------------------------------------------------------------
    // HandlePromptClick
    // ---------------------------------------------------------------------------

    [Fact]
    public void HandlePromptClick_WithActionUrl_Navigates()
    {
        var c = CreateSut();
        var prompt = new PromptDto { ActionUrl = "/world/magic", Title = "t", Message = "m" };

        c.Sut.HandlePromptClick(prompt);

        c.Navigator.Received(1).NavigateTo("/world/magic");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void HandlePromptClick_WithoutActionUrl_DoesNotNavigate(string? url)
    {
        var c = CreateSut();
        var prompt = new PromptDto { ActionUrl = url, Title = "t", Message = "m" };

        c.Sut.HandlePromptClick(prompt);

        c.Navigator.DidNotReceive().NavigateTo(Arg.Any<string>());
    }

    // ---------------------------------------------------------------------------
    // GetCategoryClass
    // ---------------------------------------------------------------------------

    [Theory]
    [InlineData(PromptCategory.MissingFundamental, "missing-fundamental")]
    [InlineData(PromptCategory.NeedsAttention, "needs-attention")]
    [InlineData(PromptCategory.Suggestion, "suggestion")]
    [InlineData((PromptCategory)99, "")]
    public void GetCategoryClass_ReturnsExpectedCssClass(PromptCategory category, string expected)
    {
        Assert.Equal(expected, DashboardViewModel.GetCategoryClass(category));
    }

    // ---------------------------------------------------------------------------
    // Dispose
    // ---------------------------------------------------------------------------

    [Fact]
    public void Dispose_UnsubscribesFromTreeStateChanges()
    {
        var c = CreateSut();
        c.Sut.Dispose();

        // Firing the event after dispose should not raise PropertyChanged
        var raised = false;
        c.Sut.PropertyChanged += (_, _) => raised = true;
        c.TreeState.OnStateChanged += Raise.Event<Action>();

        Assert.False(raised);
    }
}
