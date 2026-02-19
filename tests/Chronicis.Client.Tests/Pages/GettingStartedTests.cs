using System.Reflection;
using Bunit;
using Bunit.TestDoubles;
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

public class GettingStartedTests : MudBlazorTestContext
{
    [Fact]
    public async Task GettingStarted_OnInitialized_SetsReturningUserTrue()
    {
        var (sut, userApi, _, _) = CreateSut(true, true);

        await InvokeProtectedAsync(sut, "OnInitializedAsync");

        var isReturning = GetPrivateBoolField(sut, "_isReturningUser");
        Assert.True(isReturning);
        await userApi.Received(1).GetUserProfileAsync();
    }

    [Fact]
    public async Task GettingStarted_OnInitialized_SetsReturningUserFalse()
    {
        var (sut, _, _, _) = CreateSut(false, true);

        await InvokeProtectedAsync(sut, "OnInitializedAsync");

        Assert.False(GetPrivateBoolField(sut, "_isReturningUser"));
    }

    [Fact]
    public async Task GettingStarted_CompleteOnboarding_ReturningUser_RedirectsImmediately()
    {
        var (sut, userApi, _, nav) = CreateSut(true, true);
        await InvokeProtectedAsync(sut, "OnInitializedAsync");

        await InvokePrivateAsync(sut, "CompleteOnboarding");

        await userApi.DidNotReceive().CompleteOnboardingAsync();
        Assert.EndsWith("/dashboard", nav.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GettingStarted_NextStep_WhenAlreadyLast_DoesNotChangeStep()
    {
        var (sut, _, _, _) = CreateSut(true, true);
        SetPrivateIntField(sut, "_currentStep", 3);

        await InvokePrivateAsync(sut, "NextStep");

        Assert.Equal(3, GetPrivateIntField(sut, "_currentStep"));
    }

    [Fact]
    public async Task GettingStarted_PreviousStep_WhenAlreadyFirst_DoesNotChangeStep()
    {
        var (sut, _, _, _) = CreateSut(true, true);
        SetPrivateIntField(sut, "_currentStep", 0);

        await InvokePrivateAsync(sut, "PreviousStep");

        Assert.Equal(0, GetPrivateIntField(sut, "_currentStep"));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    public async Task GettingStarted_GoToStep_WhenInvalid_DoesNotChangeStep(int step)
    {
        var (sut, _, _, _) = CreateSut(true, true);
        SetPrivateIntField(sut, "_currentStep", 2);

        await InvokePrivateAsync(sut, "GoToStep", step);

        Assert.Equal(2, GetPrivateIntField(sut, "_currentStep"));
    }

    [Fact]
    public async Task GettingStarted_NextStep_WhenLessThanMax_IncrementsStep()
    {
        var rendered = CreateRenderedSut();

        await InvokePrivateOnRendererAsync(rendered.Cut, "NextStep");

        Assert.Equal(1, GetPrivateIntField(rendered.Instance, "_currentStep"));
    }

    [Fact]
    public async Task GettingStarted_PreviousStep_WhenGreaterThanZero_DecrementsStep()
    {
        var rendered = CreateRenderedSut();
        SetPrivateIntField(rendered.Instance, "_currentStep", 2);

        await InvokePrivateOnRendererAsync(rendered.Cut, "PreviousStep");

        Assert.Equal(1, GetPrivateIntField(rendered.Instance, "_currentStep"));
    }

    [Fact]
    public async Task GettingStarted_GoToStep_WhenValid_UpdatesStep()
    {
        var rendered = CreateRenderedSut();

        await InvokePrivateOnRendererAsync(rendered.Cut, "GoToStep", 3);

        Assert.Equal(3, GetPrivateIntField(rendered.Instance, "_currentStep"));
    }

    [Fact]
    public async Task GettingStarted_CompleteOnboarding_WhenSuccess_NavigatesWithReplace()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_isReturningUser", false);
        rendered.UserApi.CompleteOnboardingAsync().Returns(true);

        await InvokePrivateOnRendererAsync(rendered.Cut, "CompleteOnboarding");

        Assert.EndsWith("/dashboard", rendered.Navigation.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GettingStarted_CompleteOnboarding_WhenFailure_ShowsSnackbar()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_isReturningUser", false);
        rendered.UserApi.CompleteOnboardingAsync().Returns(false);

        await InvokePrivateOnRendererAsync(rendered.Cut, "CompleteOnboarding");

        rendered.Snackbar.Received(1).Add("Failed to complete setup. Please try again.", Severity.Error);
    }

    [Fact]
    public async Task GettingStarted_CompleteOnboarding_WhenException_ShowsErrorSnackbar()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_isReturningUser", false);
        rendered.UserApi.CompleteOnboardingAsync().Returns(Task.FromException<bool>(new Exception("boom")));

        await InvokePrivateOnRendererAsync(rendered.Cut, "CompleteOnboarding");

        rendered.Snackbar.Received(1).Add("An error occurred. Please try again.", Severity.Error);
        Assert.False(GetPrivateBoolField(rendered.Instance, "_isCompleting"));
    }

    [Fact]
    public async Task GettingStarted_RenderStepTwo_ShowsSidebarSectionsContent()
    {
        var rendered = CreateRenderedSut();

        await InvokePrivateOnRendererAsync(rendered.Cut, "GoToStep", 2);

        Assert.Contains("Your Sidebar Sections", rendered.Cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("EXTERNAL RESOURCES", rendered.Cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GettingStarted_RenderStepThree_WhenReturningUser_ShowsBackToDashboardButtonText()
    {
        var rendered = CreateRenderedSut();

        await InvokePrivateOnRendererAsync(rendered.Cut, "GoToStep", 3);
        SetPrivateField(rendered.Instance, "_isReturningUser", true);
        await rendered.Cut.InvokeAsync(() => rendered.Cut.Render());

        Assert.Contains("Back to Dashboard", rendered.Cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GettingStarted_RenderStepThree_WhenCompleting_ShowsFinishingUp()
    {
        var rendered = CreateRenderedSut();

        await InvokePrivateOnRendererAsync(rendered.Cut, "GoToStep", 3);
        SetPrivateField(rendered.Instance, "_isCompleting", true);
        await rendered.Cut.InvokeAsync(() => rendered.Cut.Render());

        Assert.Contains("Finishing up...", rendered.Cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GettingStarted_ClickingProgressDot_ChangesStep()
    {
        var rendered = CreateRenderedSut();

        rendered.Cut.FindAll(".dot")[2].Click();

        Assert.Equal(2, GetPrivateIntField(rendered.Instance, "_currentStep"));
    }

    [Fact]
    public void GettingStarted_OnInitialized_WhenReturningUser_RendersReturningUserStepOneTexts()
    {
        var userApi = Substitute.For<IUserApiService>();
        var worldApi = Substitute.For<IWorldApiService>();
        var snackbar = Substitute.For<ISnackbar>();
        var logger = Substitute.For<ILogger<GettingStarted>>();

        userApi.GetUserProfileAsync().Returns(new UserProfileDto { HasCompletedOnboarding = true });

        Services.AddSingleton(userApi);
        Services.AddSingleton(worldApi);
        Services.AddSingleton(snackbar);
        Services.AddSingleton(logger);

        var cut = RenderComponent<GettingStarted>();

        Assert.Contains("Quick Start Guide", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Review the Basics", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    private (GettingStarted Sut, IUserApiService UserApi, ISnackbar Snackbar, FakeNavigationManager Nav) CreateSut(bool hasCompletedOnboarding, bool completeResult)
    {
        var userApi = Substitute.For<IUserApiService>();
        var snackbar = Substitute.For<ISnackbar>();
        var nav = Services.GetRequiredService<FakeNavigationManager>();

        userApi.GetUserProfileAsync().Returns(new UserProfileDto { HasCompletedOnboarding = hasCompletedOnboarding });
        userApi.CompleteOnboardingAsync().Returns(completeResult);

        var sut = new GettingStarted(Substitute.For<ILogger<GettingStarted>>());
        SetProperty(sut, "UserApi", userApi);
        SetProperty(sut, "WorldApi", Substitute.For<IWorldApiService>());
        SetProperty(sut, "Snackbar", snackbar);
        SetProperty(sut, "Navigation", nav);

        return (sut, userApi, snackbar, nav);
    }

    private static async Task InvokeProtectedAsync(object instance, string methodName)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        var task = (Task?)method!.Invoke(instance, null);
        if (task != null)
        {
            await task;
        }
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

    private static bool GetPrivateBoolField(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        return (bool)field!.GetValue(instance)!;
    }

    private static int GetPrivateIntField(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        return (int)field!.GetValue(instance)!;
    }

    private static void SetPrivateIntField(object instance, string fieldName, int value)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        field!.SetValue(instance, value);
    }

    private static void SetPrivateField(object instance, string fieldName, object? value)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        field!.SetValue(instance, value);
    }

    private static void SetProperty(object instance, string propertyName, object? value)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(property);
        property!.SetValue(instance, value);
    }

    private RenderedContext CreateRenderedSut()
    {
        var userApi = Substitute.For<IUserApiService>();
        var worldApi = Substitute.For<IWorldApiService>();
        var snackbar = Substitute.For<ISnackbar>();
        var logger = Substitute.For<ILogger<GettingStarted>>();

        userApi.GetUserProfileAsync().Returns(new UserProfileDto { HasCompletedOnboarding = false });
        userApi.CompleteOnboardingAsync().Returns(true);

        Services.AddSingleton(userApi);
        Services.AddSingleton(worldApi);
        Services.AddSingleton(snackbar);
        Services.AddSingleton(logger);

        var cut = RenderComponent<GettingStarted>();
        var navigation = Services.GetRequiredService<NavigationManager>();

        return new RenderedContext(cut, cut.Instance, userApi, snackbar, navigation);
    }

    private static Task InvokePrivateOnRendererAsync(IRenderedComponent<GettingStarted> cut, string methodName, params object[] args)
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

    private sealed record RenderedContext(
        IRenderedComponent<GettingStarted> Cut,
        GettingStarted Instance,
        IUserApiService UserApi,
        ISnackbar Snackbar,
        NavigationManager Navigation);
}
