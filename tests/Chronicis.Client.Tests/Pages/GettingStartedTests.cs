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
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Pages;

/// <summary>
/// Render-only tests for the GettingStarted page shell.
/// Business logic is fully covered by GettingStartedViewModelTests.
/// These tests verify that the page renders correct UI based on ViewModel state.
/// </summary>
public class GettingStartedTests : MudBlazorTestContext
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private GettingStartedViewModel CreateViewModel(
        IUserApiService? userApi = null,
        IAppNavigator? navigator = null,
        IUserNotifier? notifier = null)
    {
        var hasCustomUserApi = userApi is not null;
        userApi ??= Substitute.For<IUserApiService>();
        navigator ??= Substitute.For<IAppNavigator>();
        notifier ??= Substitute.For<IUserNotifier>();
        var logger = Substitute.For<ILogger<GettingStartedViewModel>>();

        if (!hasCustomUserApi)
        {
            userApi.GetUserProfileAsync().Returns(new UserProfileDto { HasCompletedOnboarding = false });
            userApi.CompleteOnboardingAsync().Returns(true);
        }

        return new GettingStartedViewModel(userApi, navigator, notifier, logger);
    }

    private IRenderedComponent<GettingStarted> RenderWithViewModel(GettingStartedViewModel vm)
    {
        Services.AddSingleton(vm);
        Services.AddSingleton(Substitute.For<ILogger<GettingStarted>>());

        var nav = Services.GetRequiredService<FakeNavigationManager>();
        nav.NavigateTo("http://localhost/getting-started");

        return RenderComponent<GettingStarted>();
    }

    // -------------------------------------------------------------------------
    // Step 0 — Welcome (new user)
    // -------------------------------------------------------------------------

    [Fact]
    public void GettingStarted_NewUser_StepZero_RendersWelcomeHeading()
    {
        var vm = CreateViewModel();
        var cut = RenderWithViewModel(vm);

        Assert.Contains("Welcome to Chronicis", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GettingStarted_ReturningUser_StepZero_RendersQuickStartGuide()
    {
        var userApi = Substitute.For<IUserApiService>();
        userApi.GetUserProfileAsync().Returns(new UserProfileDto { HasCompletedOnboarding = true });
        userApi.CompleteOnboardingAsync().Returns(true);

        var vm = CreateViewModel(userApi: userApi);
        var cut = RenderWithViewModel(vm);

        cut.WaitForAssertion(() =>
            Assert.Contains("Quick Start Guide", cut.Markup, StringComparison.OrdinalIgnoreCase));
    }

    // -------------------------------------------------------------------------
    // Progress dots
    // -------------------------------------------------------------------------

    [Fact]
    public void GettingStarted_RendersProgressDots()
    {
        var vm = CreateViewModel();
        var cut = RenderWithViewModel(vm);

        var dots = cut.FindAll(".dot");
        Assert.Equal(GettingStartedViewModel.TotalSteps, dots.Count);
    }

    [Fact]
    public void GettingStarted_FirstDotIsActive_OnStepZero()
    {
        var vm = CreateViewModel();
        var cut = RenderWithViewModel(vm);

        var firstDot = cut.FindAll(".dot").First();
        Assert.Contains("active", firstDot.ClassName, StringComparison.OrdinalIgnoreCase);
    }

    // -------------------------------------------------------------------------
    // Navigation between steps
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GettingStarted_ClickingNextDot_MovesToNextStep()
    {
        var vm = CreateViewModel();
        var cut = RenderWithViewModel(vm);

        Assert.Equal(0, vm.CurrentStep);

        // Click dot at index 1
        await cut.InvokeAsync(() => vm.NextStep());
        cut.Render();

        Assert.Equal(1, vm.CurrentStep);
        var dots = cut.FindAll(".dot");
        Assert.Contains("active", dots[1].ClassName, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GettingStarted_ClickingProgressDot_ChangesStep()
    {
        var vm = CreateViewModel();
        var cut = RenderWithViewModel(vm);

        // Click the third dot (index 2)
        await cut.InvokeAsync(() => vm.GoToStep(2));
        cut.Render();

        Assert.Equal(2, vm.CurrentStep);
    }

    // -------------------------------------------------------------------------
    // Step rendering branches
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GettingStarted_StepTwo_ShowsSidebarSectionsContent()
    {
        var vm = CreateViewModel();
        var cut = RenderWithViewModel(vm);

        await cut.InvokeAsync(() => vm.GoToStep(2));
        cut.Render();

        Assert.Contains("Your Sidebar Sections", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GettingStarted_StepThree_ShowsFinishButton()
    {
        var vm = CreateViewModel();
        var cut = RenderWithViewModel(vm);

        await cut.InvokeAsync(() => vm.GoToStep(3));
        cut.Render();

        // Step 3 has a completion button
        var buttons = cut.FindAll("button");
        Assert.Contains(buttons, b =>
            b.TextContent.Contains("Start Your Chronicle", StringComparison.OrdinalIgnoreCase)
            || b.TextContent.Contains("Back to Dashboard", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GettingStarted_WhenCompleting_ShowsFinishingUpText()
    {
        var tcs = new TaskCompletionSource<bool>();
        var userApi = Substitute.For<IUserApiService>();
        userApi.GetUserProfileAsync().Returns(new UserProfileDto { HasCompletedOnboarding = false });
        userApi.CompleteOnboardingAsync().Returns(tcs.Task);

        var navigator = Substitute.For<IAppNavigator>();
        var vm = CreateViewModel(userApi: userApi, navigator: navigator);
        var cut = RenderWithViewModel(vm);

        await cut.InvokeAsync(() => vm.GoToStep(3));
        cut.Render();

        // Fire complete but don't resolve tcs so IsCompleting stays true
        var completeTask = cut.InvokeAsync(() => vm.CompleteOnboardingAsync());
        cut.Render();

        cut.WaitForAssertion(() =>
        {
            Assert.True(vm.IsCompleting);
            Assert.Contains("Finishing up", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });

        // Unblock
        tcs.SetResult(true);
        await completeTask;
    }

    [Fact]
    public async Task GettingStarted_ReturningUser_StepThree_ShowsBackToDashboardText()
    {
        var userApi = Substitute.For<IUserApiService>();
        userApi.GetUserProfileAsync().Returns(new UserProfileDto { HasCompletedOnboarding = true });
        userApi.CompleteOnboardingAsync().Returns(true);

        var vm = CreateViewModel(userApi: userApi);
        var cut = RenderWithViewModel(vm);

        cut.WaitForAssertion(() => Assert.True(vm.IsReturningUser));
        await cut.InvokeAsync(() => vm.GoToStep(3));
        cut.Render();

        Assert.Contains("Back to Dashboard", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }
}
