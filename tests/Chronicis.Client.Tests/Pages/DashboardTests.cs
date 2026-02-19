using System.Reflection;
using Bunit;
using Bunit.TestDoubles;
using Chronicis.Client.Components.Dialogs;
using Chronicis.Client.Pages;
using Chronicis.Client.Services;
using Chronicis.Client.Tests.Components;
using Chronicis.Shared.DTOs;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MudBlazor;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Pages;

public class DashboardTests : MudBlazorTestContext
{
    [Fact]
    public async Task Dashboard_NavigateToCharacter_WithBreadcrumbs_NavigatesToArticlePath()
    {
        var (sut, articleApi, nav, _, _, _, _) = CreateSut();
        var characterId = Guid.NewGuid();

        articleApi.GetArticleDetailAsync(characterId).Returns(new ArticleDto
        {
            Breadcrumbs =
            [
                new BreadcrumbDto { Slug = "world" },
                new BreadcrumbDto { Slug = "character" }
            ]
        });

        await InvokePrivateAsync(sut, "NavigateToCharacter", characterId);

        Assert.EndsWith("/article/world/character", nav.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Dashboard_OnInitialized_UserNeedsOnboarding_RedirectsAndReturnsEarly()
    {
        var (sut, _, nav, treeState, userApi, _, _) = CreateSut();
        userApi.GetUserProfileAsync().Returns(new UserProfileDto { HasCompletedOnboarding = false });

        await InvokeProtectedAsync(sut, "OnInitializedAsync");

        Assert.EndsWith("/getting-started", nav.Uri, StringComparison.OrdinalIgnoreCase);
        treeState.DidNotReceive().OnStateChanged += Arg.Any<Action>();
    }

    [Fact]
    public async Task Dashboard_CreateNewWorld_WhenCreateReturnsWorldWithRoot_NavigatesToWorld()
    {
        var (sut, _, nav, treeState, _, worldApi, _) = CreateSut();
        worldApi.CreateWorldAsync(Arg.Any<WorldCreateDto>()).Returns(new WorldDto
        {
            Name = "New World",
            Slug = "new-world",
            WorldRootArticleId = Guid.NewGuid()
        });

        await InvokePrivateAsync(sut, "CreateNewWorld");

        await treeState.Received(1).RefreshAsync();
        Assert.EndsWith("/world/new-world", nav.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Dashboard_CreateNewWorld_WhenCreateReturnsNull_DoesNotRefreshTree()
    {
        var (sut, _, _, treeState, _, worldApi, _) = CreateSut();
        worldApi.CreateWorldAsync(Arg.Any<WorldCreateDto>()).Returns((WorldDto?)null);

        await InvokePrivateAsync(sut, "CreateNewWorld");

        await treeState.DidNotReceive().RefreshAsync();
    }

    [Fact]
    public async Task Dashboard_LoadQuote_WhenServiceThrows_DoesNotThrow()
    {
        var (sut, _, _, _, _, _, quoteService) = CreateSut();
        quoteService.GetRandomQuoteAsync().Returns(Task.FromException<Quote>(new Exception("boom")));

        var ex = await Record.ExceptionAsync(() => InvokePrivateAsync(sut, "LoadQuote"));

        Assert.Null(ex);
    }

    [Fact]
    public void Dashboard_OnInitialized_WhenDashboardIsNull_RendersErrorState()
    {
        var rendered = CreateRenderedSut();

        Assert.Contains("Unable to load dashboard", rendered.Cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Dashboard_LoadDashboard_WhenApiThrows_ShowsErrorSnackbar()
    {
        var rendered = CreateRenderedSut();
        rendered.DashboardApi.GetDashboardAsync().Returns(Task.FromException<DashboardDto?>(new Exception("boom")));

        await InvokePrivateOnRendererAsync(rendered.Cut, "LoadDashboard");

        rendered.Snackbar.Received(1).Add("Failed to load dashboard", Severity.Error);
    }

    [Fact]
    public async Task Dashboard_CreateNewWorld_WhenRootMissing_LoadsDashboardInsteadOfNavigate()
    {
        var rendered = CreateRenderedSut();

        rendered.WorldApi.CreateWorldAsync(Arg.Any<WorldCreateDto>()).Returns(new WorldDto
        {
            Name = "No Root World",
            Slug = "no-root-world",
            WorldRootArticleId = null
        });

        await InvokePrivateOnRendererAsync(rendered.Cut, "CreateNewWorld");

        await rendered.TreeState.Received(1).RefreshAsync();
        await rendered.DashboardApi.Received(2).GetDashboardAsync();
        Assert.DoesNotContain("/world/no-root-world", rendered.Navigation.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Dashboard_OnInitialized_WithRichDashboard_RendersExpandedBranches()
    {
        var dashboardApi = Substitute.For<IDashboardApiService>();
        var userApi = Substitute.For<IUserApiService>();
        var worldApi = Substitute.For<IWorldApiService>();
        var quoteService = Substitute.For<IQuoteService>();
        var treeState = Substitute.For<ITreeStateService>();
        var dialogService = Substitute.For<IDialogService>();
        var articleApi = Substitute.For<IArticleApiService>();
        var snackbar = Substitute.For<ISnackbar>();
        var logger = Substitute.For<ILogger<Dashboard>>();

        userApi.GetUserProfileAsync().Returns(new UserProfileDto { HasCompletedOnboarding = true });
        quoteService.GetRandomQuoteAsync().Returns(new Quote { Content = "Test quote", Author = "Test author" });
        dashboardApi.GetDashboardAsync().Returns(new DashboardDto
        {
            UserDisplayName = "Tester",
            Worlds =
            [
                new DashboardWorldDto
                {
                    Id = Guid.NewGuid(),
                    Name = "Primary World",
                    Slug = "primary-world",
                    CreatedAt = DateTime.UtcNow,
                    ArticleCount = 10,
                    Campaigns =
                    [
                        new DashboardCampaignDto
                        {
                            Id = Guid.NewGuid(),
                            Name = "Active Campaign",
                            IsActive = true,
                            CurrentArc = new DashboardArcDto { Name = "Arc One", LatestSessionDate = DateTime.UtcNow.AddDays(-1) }
                        }
                    ]
                },
                new DashboardWorldDto
                {
                    Id = Guid.NewGuid(),
                    Name = "Secondary World",
                    Slug = "secondary-world",
                    CreatedAt = DateTime.UtcNow.AddDays(-5),
                    ArticleCount = 2
                }
            ],
            Prompts =
            [
                new PromptDto { Title = "Prompt One", Message = "Action prompt", Category = PromptCategory.Tip, ActionUrl = "/settings" },
                new PromptDto { Title = "Prompt Two", Message = "No action prompt", Category = PromptCategory.Suggestion, ActionUrl = null }
            ],
            ClaimedCharacters =
            [
                new ClaimedCharacterDto { Id = Guid.NewGuid(), Title = "C1", WorldName = "W1" },
                new ClaimedCharacterDto { Id = Guid.NewGuid(), Title = "C2", WorldName = "W1" },
                new ClaimedCharacterDto { Id = Guid.NewGuid(), Title = "C3", WorldName = "W1" },
                new ClaimedCharacterDto { Id = Guid.NewGuid(), Title = "C4", WorldName = "W1" },
                new ClaimedCharacterDto { Id = Guid.NewGuid(), Title = "C5", WorldName = "W1" },
                new ClaimedCharacterDto { Id = Guid.NewGuid(), Title = "C6", WorldName = "W1" }
            ]
        });

        Services.AddSingleton(dashboardApi);
        Services.AddSingleton(userApi);
        Services.AddSingleton(worldApi);
        Services.AddSingleton(quoteService);
        Services.AddSingleton(treeState);
        Services.AddSingleton(dialogService);
        Services.AddSingleton(articleApi);
        Services.AddSingleton(snackbar);
        Services.AddSingleton(logger);

        var cut = RenderComponent<Dashboard>();

        Assert.Contains("Welcome back", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Other Worlds", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("My Characters", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("+1 more characters", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Test quote", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Dashboard_CreateNewWorld_WhenCreateThrows_ShowsErrorSnackbar()
    {
        var rendered = CreateRenderedSut();
        rendered.WorldApi.CreateWorldAsync(Arg.Any<WorldCreateDto>()).Returns(Task.FromException<WorldDto?>(new Exception("nope")));

        await InvokePrivateOnRendererAsync(rendered.Cut, "CreateNewWorld");

        rendered.Snackbar.Received(1).Add("Error: nope", Severity.Error);
    }

    [Fact]
    public async Task Dashboard_JoinWorld_WhenDialogReturnsJoinResult_RefreshesAndNavigates()
    {
        var rendered = CreateRenderedSut();
        var dialogRef = Substitute.For<IDialogReference>();
        dialogRef.Result.Returns(Task.FromResult<DialogResult?>(DialogResult.Ok(new WorldJoinResultDto
        {
            WorldId = Guid.NewGuid(),
            WorldName = "Joined World"
        })));

        rendered.DialogService.ShowAsync<JoinWorldDialog>("Join a World").Returns(Task.FromResult(dialogRef));

        await InvokePrivateOnRendererAsync(rendered.Cut, "JoinWorld");

        await rendered.TreeState.Received(1).RefreshAsync();
        await rendered.DashboardApi.Received(2).GetDashboardAsync();
        Assert.Contains("/world/", rendered.Navigation.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Dashboard_JoinWorld_WhenCanceled_DoesNotRefreshOrNavigate()
    {
        var rendered = CreateRenderedSut();
        var before = rendered.Navigation.Uri;
        var dialogRef = Substitute.For<IDialogReference>();
        dialogRef.Result.Returns(Task.FromResult<DialogResult?>(DialogResult.Cancel()));

        rendered.DialogService.ShowAsync<JoinWorldDialog>("Join a World").Returns(Task.FromResult(dialogRef));

        await InvokePrivateOnRendererAsync(rendered.Cut, "JoinWorld");

        await rendered.TreeState.DidNotReceive().RefreshAsync();
        Assert.Equal(before, rendered.Navigation.Uri);
    }

    [Fact]
    public void Dashboard_RichDashboard_ClickingPromptAndCharacter_TriggersNavigation()
    {
        var dashboardApi = Substitute.For<IDashboardApiService>();
        var userApi = Substitute.For<IUserApiService>();
        var worldApi = Substitute.For<IWorldApiService>();
        var quoteService = Substitute.For<IQuoteService>();
        var treeState = Substitute.For<ITreeStateService>();
        var dialogService = Substitute.For<IDialogService>();
        var articleApi = Substitute.For<IArticleApiService>();
        var snackbar = Substitute.For<ISnackbar>();
        var logger = Substitute.For<ILogger<Dashboard>>();
        var characterId = Guid.NewGuid();

        userApi.GetUserProfileAsync().Returns(new UserProfileDto { HasCompletedOnboarding = true });
        quoteService.GetRandomQuoteAsync().Returns((Quote?)null);
        dashboardApi.GetDashboardAsync().Returns(new DashboardDto
        {
            UserDisplayName = "Tester",
            Worlds = [new DashboardWorldDto { Id = Guid.NewGuid(), Name = "W1", Slug = "w1", CreatedAt = DateTime.UtcNow }],
            Prompts = [new PromptDto { Title = "Prompt", Message = "Message", ActionUrl = "/settings" }],
            ClaimedCharacters = [new ClaimedCharacterDto { Id = characterId, Title = "Hero", WorldName = "W1" }]
        });

        articleApi.GetArticleDetailAsync(characterId).Returns(new ArticleDto
        {
            Breadcrumbs =
            [
                new BreadcrumbDto { Slug = "world" },
                new BreadcrumbDto { Slug = "hero" }
            ]
        });

        Services.AddSingleton(dashboardApi);
        Services.AddSingleton(userApi);
        Services.AddSingleton(worldApi);
        Services.AddSingleton(quoteService);
        Services.AddSingleton(treeState);
        Services.AddSingleton(dialogService);
        Services.AddSingleton(articleApi);
        Services.AddSingleton(snackbar);
        Services.AddSingleton(logger);

        var cut = RenderComponent<Dashboard>();
        var nav = Services.GetRequiredService<FakeNavigationManager>();

        cut.Find(".hero-prompt-card").Click();
        Assert.EndsWith("/settings", nav.Uri, StringComparison.OrdinalIgnoreCase);

        cut.Find(".character-item").Click();
        cut.WaitForAssertion(() =>
            Assert.EndsWith("/article/world/hero", nav.Uri, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Dashboard_OnInitialized_WithNoWorlds_RendersEmptyState()
    {
        var dashboardApi = Substitute.For<IDashboardApiService>();
        var userApi = Substitute.For<IUserApiService>();
        var worldApi = Substitute.For<IWorldApiService>();
        var quoteService = Substitute.For<IQuoteService>();
        var treeState = Substitute.For<ITreeStateService>();
        var dialogService = Substitute.For<IDialogService>();
        var articleApi = Substitute.For<IArticleApiService>();
        var snackbar = Substitute.For<ISnackbar>();
        var logger = Substitute.For<ILogger<Dashboard>>();

        userApi.GetUserProfileAsync().Returns(new UserProfileDto { HasCompletedOnboarding = true });
        dashboardApi.GetDashboardAsync().Returns(new DashboardDto
        {
            UserDisplayName = "Tester",
            Worlds = [],
            Prompts = [],
            ClaimedCharacters = []
        });
        quoteService.GetRandomQuoteAsync().Returns((Quote?)null);

        Services.AddSingleton(dashboardApi);
        Services.AddSingleton(userApi);
        Services.AddSingleton(worldApi);
        Services.AddSingleton(quoteService);
        Services.AddSingleton(treeState);
        Services.AddSingleton(dialogService);
        Services.AddSingleton(articleApi);
        Services.AddSingleton(snackbar);
        Services.AddSingleton(logger);

        var cut = RenderComponent<Dashboard>();

        Assert.Contains("Begin Your Chronicle", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Create Your First World", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Dashboard_OnTreeStateChanged_WhenInvoked_DoesNotThrow()
    {
        var rendered = CreateRenderedSut();

        var ex = await Record.ExceptionAsync(() => InvokePrivateOnRendererAsync(rendered.Cut, "OnTreeStateChanged"));

        Assert.Null(ex);
    }

    [Fact]
    public async Task Dashboard_NavigateToCharacter_WithoutBreadcrumbs_DoesNotNavigate()
    {
        var (sut, articleApi, nav, _, _, _, _) = CreateSut();
        var characterId = Guid.NewGuid();

        articleApi.GetArticleDetailAsync(characterId).Returns(new ArticleDto());
        var before = nav.Uri;

        await InvokePrivateAsync(sut, "NavigateToCharacter", characterId);

        Assert.Equal(before, nav.Uri);
    }

    [Fact]
    public async Task Dashboard_HandlePromptClick_WithActionUrl_Navigates()
    {
        var (sut, _, nav, _, _, _, _) = CreateSut();
        var prompt = new PromptDto { ActionUrl = "/settings" };

        await InvokePrivateAsync(sut, "HandlePromptClick", prompt);

        Assert.EndsWith("/settings", nav.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Dashboard_HandlePromptClick_WithoutActionUrl_DoesNotNavigate()
    {
        var (sut, _, nav, _, _, _, _) = CreateSut();
        var prompt = new PromptDto { ActionUrl = null };
        var before = nav.Uri;

        await InvokePrivateAsync(sut, "HandlePromptClick", prompt);

        Assert.Equal(before, nav.Uri);
    }

    [Theory]
    [InlineData(PromptCategory.MissingFundamental, "missing-fundamental")]
    [InlineData(PromptCategory.NeedsAttention, "needs-attention")]
    [InlineData(PromptCategory.Suggestion, "suggestion")]
    [InlineData(PromptCategory.Tip, "")]
    public async Task Dashboard_GetCategoryClass_ReturnsExpectedCssClass(PromptCategory category, string expected)
    {
        var (sut, _, _, _, _, _, _) = CreateSut();

        var actual = await InvokePrivateWithResultAsync<string>(sut, "GetCategoryClass", category);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Dashboard_Dispose_UnsubscribesFromTreeState()
    {
        var (sut, _, _, treeState, _, _, _) = CreateSut();

        sut.Dispose();

        treeState.Received().OnStateChanged -= Arg.Any<Action>();
    }

    private (Dashboard Sut, IArticleApiService ArticleApi, FakeNavigationManager Nav, ITreeStateService TreeState, IUserApiService UserApi, IWorldApiService WorldApi, IQuoteService QuoteService) CreateSut()
    {
        var articleApi = Substitute.For<IArticleApiService>();
        var treeState = Substitute.For<ITreeStateService>();
        var userApi = Substitute.For<IUserApiService>();
        var worldApi = Substitute.For<IWorldApiService>();
        var quoteService = Substitute.For<IQuoteService>();
        var snackbar = Substitute.For<ISnackbar>();
        var nav = Services.GetRequiredService<FakeNavigationManager>();

        var sut = new Dashboard(Substitute.For<ILogger<Dashboard>>());
        SetProperty(sut, "ArticleApi", articleApi);
        SetProperty(sut, "TreeStateService", treeState);
        SetProperty(sut, "UserApi", userApi);
        SetProperty(sut, "WorldApi", worldApi);
        SetProperty(sut, "QuoteService", quoteService);
        SetProperty(sut, "Snackbar", snackbar);
        SetProperty(sut, "Navigation", nav);

        return (sut, articleApi, nav, treeState, userApi, worldApi, quoteService);
    }

    private static async Task InvokePrivateAsync(object instance, string methodName, params object[] args)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        var result = method!.Invoke(instance, args);
        if (result is Task task)
        {
            await task;
        }
    }

    private static async Task<T?> InvokePrivateWithResultAsync<T>(object instance, string methodName, params object[] args)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        var result = method!.Invoke(instance, args);

        if (result is Task<T> taskOfT)
        {
            return await taskOfT;
        }

        return (T?)result;
    }

    private static async Task InvokeProtectedAsync(object instance, string methodName)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        var result = method!.Invoke(instance, null);
        if (result is Task task)
        {
            await task;
        }
    }

    private RenderedContext CreateRenderedSut()
    {
        var dashboardApi = Substitute.For<IDashboardApiService>();
        var userApi = Substitute.For<IUserApiService>();
        var worldApi = Substitute.For<IWorldApiService>();
        var quoteService = Substitute.For<IQuoteService>();
        var treeState = Substitute.For<ITreeStateService>();
        var dialogService = Substitute.For<IDialogService>();
        var articleApi = Substitute.For<IArticleApiService>();
        var snackbar = Substitute.For<ISnackbar>();
        var logger = Substitute.For<ILogger<Dashboard>>();

        userApi.GetUserProfileAsync().Returns(new UserProfileDto { HasCompletedOnboarding = true });
        dashboardApi.GetDashboardAsync().Returns((DashboardDto?)null);
        quoteService.GetRandomQuoteAsync().Returns((Quote?)null);

        Services.AddSingleton(dashboardApi);
        Services.AddSingleton(userApi);
        Services.AddSingleton(worldApi);
        Services.AddSingleton(quoteService);
        Services.AddSingleton(treeState);
        Services.AddSingleton(dialogService);
        Services.AddSingleton(articleApi);
        Services.AddSingleton(snackbar);
        Services.AddSingleton(logger);

        var cut = RenderComponent<Dashboard>();
        var navigation = Services.GetRequiredService<NavigationManager>();

        return new RenderedContext(cut, dashboardApi, worldApi, treeState, dialogService, snackbar, navigation);
    }

    private static Task InvokePrivateOnRendererAsync(IRenderedComponent<Dashboard> cut, string methodName, params object[] args)
    {
        return cut.InvokeAsync(async () =>
        {
            var method = cut.Instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            var result = method!.Invoke(cut.Instance, args);
            if (result is Task task)
            {
                await task;
            }
        });
    }

    private static void SetProperty(object instance, string propertyName, object? value)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(property);
        property!.SetValue(instance, value);
    }

    private sealed record RenderedContext(
        IRenderedComponent<Dashboard> Cut,
        IDashboardApiService DashboardApi,
        IWorldApiService WorldApi,
        ITreeStateService TreeState,
        IDialogService DialogService,
        ISnackbar Snackbar,
        NavigationManager Navigation);
}
