using Chronicis.Client.Abstractions;
using Chronicis.Client.Services;
using Chronicis.Client.ViewModels;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Chronicis.Client.Tests.ViewModels;

public class GettingStartedViewModelTests
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private record Sut(
        GettingStartedViewModel VM,
        IUserApiService UserApi,
        IAppNavigator Navigator,
        IUserNotifier Notifier);

    private static Sut CreateSut()
    {
        var userApi = Substitute.For<IUserApiService>();
        var navigator = Substitute.For<IAppNavigator>();
        var notifier = Substitute.For<IUserNotifier>();
        var logger = Substitute.For<ILogger<GettingStartedViewModel>>();

        var vm = new GettingStartedViewModel(userApi, navigator, notifier, logger);
        return new Sut(vm, userApi, navigator, notifier);
    }

    // -------------------------------------------------------------------------
    // Constants
    // -------------------------------------------------------------------------

    [Fact]
    public void TotalSteps_IsFour()
    {
        Assert.Equal(4, GettingStartedViewModel.TotalSteps);
    }

    // -------------------------------------------------------------------------
    // Initial state
    // -------------------------------------------------------------------------

    [Fact]
    public void InitialState_StepZero_NotCompleting_NotReturning()
    {
        var c = CreateSut();
        Assert.Equal(0, c.VM.CurrentStep);
        Assert.False(c.VM.IsCompleting);
        Assert.False(c.VM.IsReturningUser);
    }

    // -------------------------------------------------------------------------
    // InitializeAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task InitializeAsync_NewUser_IsReturningUserFalse()
    {
        var c = CreateSut();
        c.UserApi.GetUserProfileAsync().Returns(new UserProfileDto { HasCompletedOnboarding = false });

        await c.VM.InitializeAsync();

        Assert.False(c.VM.IsReturningUser);
    }

    [Fact]
    public async Task InitializeAsync_ReturningUser_IsReturningUserTrue()
    {
        var c = CreateSut();
        c.UserApi.GetUserProfileAsync().Returns(new UserProfileDto { HasCompletedOnboarding = true });

        await c.VM.InitializeAsync();

        Assert.True(c.VM.IsReturningUser);
    }

    [Fact]
    public async Task InitializeAsync_NullProfile_IsReturningUserFalse()
    {
        var c = CreateSut();
        c.UserApi.GetUserProfileAsync().Returns((UserProfileDto?)null);

        await c.VM.InitializeAsync();

        Assert.False(c.VM.IsReturningUser);
    }

    // -------------------------------------------------------------------------
    // NextStep
    // -------------------------------------------------------------------------

    [Fact]
    public void NextStep_AdvancesCurrentStep()
    {
        var c = CreateSut();
        c.VM.NextStep();
        Assert.Equal(1, c.VM.CurrentStep);
    }

    [Fact]
    public void NextStep_AtLastStep_DoesNotAdvance()
    {
        var c = CreateSut();
        c.VM.GoToStep(GettingStartedViewModel.TotalSteps - 1);
        c.VM.NextStep();
        Assert.Equal(GettingStartedViewModel.TotalSteps - 1, c.VM.CurrentStep);
    }

    [Fact]
    public void NextStep_RaisesPropertyChanged()
    {
        var c = CreateSut();
        string? changedProp = null;
        c.VM.PropertyChanged += (_, e) => changedProp = e.PropertyName;

        c.VM.NextStep();

        Assert.Equal(nameof(GettingStartedViewModel.CurrentStep), changedProp);
    }

    // -------------------------------------------------------------------------
    // PreviousStep
    // -------------------------------------------------------------------------

    [Fact]
    public void PreviousStep_DecreasesCurrentStep()
    {
        var c = CreateSut();
        c.VM.NextStep();
        c.VM.PreviousStep();
        Assert.Equal(0, c.VM.CurrentStep);
    }

    [Fact]
    public void PreviousStep_AtStepZero_DoesNotDecrease()
    {
        var c = CreateSut();
        c.VM.PreviousStep();
        Assert.Equal(0, c.VM.CurrentStep);
    }

    // -------------------------------------------------------------------------
    // GoToStep
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void GoToStep_ValidStep_SetsCurrentStep(int step)
    {
        var c = CreateSut();
        c.VM.GoToStep(step);
        Assert.Equal(step, c.VM.CurrentStep);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    [InlineData(99)]
    public void GoToStep_InvalidStep_DoesNotChangeCurrentStep(int step)
    {
        var c = CreateSut();
        c.VM.GoToStep(step);
        Assert.Equal(0, c.VM.CurrentStep); // unchanged from initial
    }

    // -------------------------------------------------------------------------
    // CompleteOnboardingAsync — returning user
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CompleteOnboardingAsync_ReturningUser_NavigatesWithoutCallingApi()
    {
        var c = CreateSut();
        c.UserApi.GetUserProfileAsync().Returns(new UserProfileDto { HasCompletedOnboarding = true });
        await c.VM.InitializeAsync();

        await c.VM.CompleteOnboardingAsync();

        c.Navigator.Received(1).NavigateTo("/dashboard");
        await c.UserApi.DidNotReceive().CompleteOnboardingAsync();
    }

    // -------------------------------------------------------------------------
    // CompleteOnboardingAsync — new user
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CompleteOnboardingAsync_Success_NavigatesToDashboard()
    {
        var c = CreateSut();
        c.UserApi.CompleteOnboardingAsync().Returns(true);

        await c.VM.CompleteOnboardingAsync();

        c.Navigator.Received(1).NavigateTo("/dashboard", replace: true);
        Assert.False(c.VM.IsCompleting);
    }

    [Fact]
    public async Task CompleteOnboardingAsync_ApiReturnsFalse_ShowsError()
    {
        var c = CreateSut();
        c.UserApi.CompleteOnboardingAsync().Returns(false);

        await c.VM.CompleteOnboardingAsync();

        c.Notifier.Received(1).Error(Arg.Any<string>());
        c.Navigator.DidNotReceive().NavigateTo(Arg.Any<string>());
        Assert.False(c.VM.IsCompleting);
    }

    [Fact]
    public async Task CompleteOnboardingAsync_ApiThrows_ShowsError()
    {
        var c = CreateSut();
        c.UserApi.CompleteOnboardingAsync().ThrowsAsync(new Exception("network failure"));

        await c.VM.CompleteOnboardingAsync();

        c.Notifier.Received(1).Error(Arg.Any<string>());
        Assert.False(c.VM.IsCompleting);
    }

    [Fact]
    public async Task CompleteOnboardingAsync_SetsIsCompletingDuringCall()
    {
        var c = CreateSut();
        var wasCompleting = false;

        c.UserApi.CompleteOnboardingAsync().Returns(_ =>
        {
            wasCompleting = c.VM.IsCompleting;
            return Task.FromResult(true);
        });

        await c.VM.CompleteOnboardingAsync();

        Assert.True(wasCompleting);
        Assert.False(c.VM.IsCompleting);
    }
}
