using Bunit;
using Bunit.TestDoubles;
using Chronicis.Client.Abstractions;
using Chronicis.Client.Pages;
using Chronicis.Client.Services;
using Chronicis.Client.Tests.Components;
using Chronicis.Client.ViewModels;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MudBlazor;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Pages;

/// <summary>
/// Tests for the Dashboard page shell.
/// Business logic is covered by DashboardViewModelTests / DashboardViewModelActionsTests.
/// These tests verify that the shell renders the correct UI states based on ViewModel properties.
/// </summary>
public class DashboardTests : MudBlazorTestContext
{
    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    private DashboardViewModel CreateViewModel(
        IDashboardApiService? dashboardApi = null,
        IUserApiService? userApi = null,
        IWorldApiService? worldApi = null,
        IArticleApiService? articleApi = null,
        IQuoteService? quoteService = null,
        ITreeStateService? treeState = null,
        IDialogService? dialogService = null,
        IAppNavigator? navigator = null,
        IUserNotifier? notifier = null)
    {
        dashboardApi ??= Substitute.For<IDashboardApiService>();
        userApi ??= Substitute.For<IUserApiService>();
        worldApi ??= Substitute.For<IWorldApiService>();
        articleApi ??= Substitute.For<IArticleApiService>();
        quoteService ??= Substitute.For<IQuoteService>();
        treeState ??= Substitute.For<ITreeStateService>();
        dialogService ??= Substitute.For<IDialogService>();
        navigator ??= Substitute.For<IAppNavigator>();
        notifier ??= Substitute.For<IUserNotifier>();
        var logger = Substitute.For<ILogger<DashboardViewModel>>();

        return new DashboardViewModel(
            dashboardApi, userApi, worldApi, articleApi,
            quoteService, treeState, dialogService, navigator, notifier, logger);
    }

    private IRenderedComponent<Dashboard> RenderWithViewModel(DashboardViewModel vm)
    {
        Services.AddSingleton(vm);
        Services.AddSingleton(Substitute.For<ILogger<Dashboard>>());
        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/dashboard");

        return RenderComponent<Dashboard>();
    }

    // ---------------------------------------------------------------------------
    // Loading state
    // ---------------------------------------------------------------------------

    [Fact]
    public void Dashboard_WhenIsLoadingTrue_RendersLoadingSpinner()
    {
        // Use a TCS to pause the API call so IsLoading stays true during render
        var tcs = new TaskCompletionSource<UserProfileDto?>();
        var userApi = Substitute.For<IUserApiService>();
        userApi.GetUserProfileAsync().Returns(tcs.Task);

        var vm = CreateViewModel(userApi: userApi);
        var cut = RenderWithViewModel(vm);

        // IsLoading is still true because GetUserProfileAsync hasn't completed
        Assert.Contains("mud-progress-circular", cut.Markup, StringComparison.OrdinalIgnoreCase);

        // Unblock to avoid test hangs
        tcs.SetResult(null);
    }

    // ---------------------------------------------------------------------------
    // Error / null dashboard state
    // ---------------------------------------------------------------------------

    [Fact]
    public void Dashboard_WhenDashboardNullAndNotLoading_RendersErrorState()
    {
        var userApi = Substitute.For<IUserApiService>();
        userApi.GetUserProfileAsync().Returns(new UserProfileDto { HasCompletedOnboarding = true });

        var dashboardApi = Substitute.For<IDashboardApiService>();
        dashboardApi.GetDashboardAsync().Returns((DashboardDto?)null);

        var vm = CreateViewModel(dashboardApi: dashboardApi, userApi: userApi);
        var cut = RenderWithViewModel(vm);

        cut.WaitForAssertion(() =>
            Assert.Contains("Unable to load dashboard", cut.Markup, StringComparison.OrdinalIgnoreCase));
    }

    // ---------------------------------------------------------------------------
    // Populated state
    // ---------------------------------------------------------------------------

    [Fact]
    public void Dashboard_WhenDashboardLoaded_ShowsWelcomeMessage()
    {
        var userApi = Substitute.For<IUserApiService>();
        userApi.GetUserProfileAsync().Returns(new UserProfileDto { HasCompletedOnboarding = true });

        var dashboardApi = Substitute.For<IDashboardApiService>();
        dashboardApi.GetDashboardAsync().Returns(new DashboardDto
        {
            UserDisplayName = "Gandalf",
            Worlds = new(),
            ClaimedCharacters = new(),
            Prompts = new()
        });

        var vm = CreateViewModel(dashboardApi: dashboardApi, userApi: userApi);
        var cut = RenderWithViewModel(vm);

        cut.WaitForAssertion(() =>
            Assert.Contains("Gandalf", cut.Markup, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Dashboard_WhenDashboardHasNoWorlds_ShowsEmptyState()
    {
        var userApi = Substitute.For<IUserApiService>();
        userApi.GetUserProfileAsync().Returns(new UserProfileDto { HasCompletedOnboarding = true });

        var dashboardApi = Substitute.For<IDashboardApiService>();
        dashboardApi.GetDashboardAsync().Returns(new DashboardDto
        {
            UserDisplayName = "Frodo",
            Worlds = new(),
            ClaimedCharacters = new(),
            Prompts = new()
        });

        var vm = CreateViewModel(dashboardApi: dashboardApi, userApi: userApi);
        var cut = RenderWithViewModel(vm);

        cut.WaitForAssertion(() =>
            Assert.Contains("Begin Your Chronicle", cut.Markup, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Dashboard_WhenQuoteAndPromptsPresent_RendersHeroQuoteAndPromptVariants()
    {
        var userApi = Substitute.For<IUserApiService>();
        userApi.GetUserProfileAsync().Returns(new UserProfileDto { HasCompletedOnboarding = true });

        var dashboardApi = Substitute.For<IDashboardApiService>();
        dashboardApi.GetDashboardAsync().Returns(new DashboardDto
        {
            UserDisplayName = "Bilbo",
            Worlds = new(),
            ClaimedCharacters = new(),
            Prompts = new()
            {
                new PromptDto
                {
                    Title = "Add a village",
                    Message = "Your world needs a hometown.",
                    ActionUrl = "/world/shire",
                    Category = PromptCategory.MissingFundamental,
                    Icon = "🏘️"
                },
                new PromptDto
                {
                    Title = "Review notes",
                    Message = "Summarize last session.",
                    ActionUrl = null,
                    Category = PromptCategory.Suggestion,
                    Icon = "📝"
                }
            }
        });

        var quoteService = Substitute.For<IQuoteService>();
        quoteService.GetRandomQuoteAsync().Returns(new Quote { Content = "Not all those who wander are lost", Author = "Tolkien" });

        var vm = CreateViewModel(dashboardApi: dashboardApi, userApi: userApi, quoteService: quoteService);
        var cut = RenderWithViewModel(vm);

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Tolkien", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Add a village", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Review notes", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("prompt-arrow", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }
}
