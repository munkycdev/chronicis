using System.Reflection;
using Bunit.TestDoubles;
using Chronicis.Client.Pages;
using Chronicis.Client.Services;
using Chronicis.Client.Tests.Components;
using Chronicis.Shared.DTOs;
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

    private static void SetProperty(object instance, string propertyName, object? value)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(property);
        property!.SetValue(instance, value);
    }
}
