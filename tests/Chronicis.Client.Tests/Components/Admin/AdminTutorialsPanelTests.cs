using System.Reflection;
using Bunit;
using Chronicis.Client.Components.Admin;
using Chronicis.Client.Services;
using Chronicis.Client.Tests.Components;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MudBlazor;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Components.Admin;

public class AdminTutorialsPanelTests : MudBlazorTestContext
{
    private void RegisterCommonServices(IAdminApiService adminApi)
    {
        Services.AddSingleton(adminApi);
        Services.AddSingleton(Substitute.For<IArticleApiService>());
        Services.AddSingleton(Substitute.For<IBreadcrumbService>());
        Services.AddSingleton(Substitute.For<ISnackbar>());
        Services.AddSingleton(Substitute.For<ILogger<AdminTutorialsPanel>>());
    }

    [Fact]
    public void Panel_ShowsLoadingBar_WhileLoading()
    {
        var adminApi = Substitute.For<IAdminApiService>();
        var tcs = new TaskCompletionSource<List<TutorialMappingDto>>();
        adminApi.GetTutorialMappingsAsync().Returns(tcs.Task);
        RegisterCommonServices(adminApi);
        RenderComponent<MudPopoverProvider>();

        var cut = RenderComponent<AdminTutorialsPanel>();

        Assert.Contains("mud-progress-linear", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Panel_ShowsErrorAlert_WhenLoadFails()
    {
        var adminApi = Substitute.For<IAdminApiService>();
        adminApi.GetTutorialMappingsAsync()
            .Returns(_ => Task.FromException<List<TutorialMappingDto>>(new InvalidOperationException("boom")));
        RegisterCommonServices(adminApi);
        RenderComponent<MudPopoverProvider>();

        var cut = RenderComponent<AdminTutorialsPanel>();

        cut.WaitForAssertion(() =>
            Assert.Contains("Failed to load tutorial mappings.", cut.Markup, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void OnPageTypeChanged_WithUnknownOption_LeavesGeneratedFieldsUnchanged()
    {
        var instance = new AdminTutorialsPanel();
        SetField(instance, "_pageTypeName", "Existing");
        SetField(instance, "_tutorialTitle", "Existing Tutorial");

        InvokePrivate(instance, "OnPageTypeChanged", "Page:Missing");

        Assert.Equal("Page:Missing", GetField<string?>(instance, "_selectedPageType"));
        Assert.Equal("Existing", GetField<string>(instance, "_pageTypeName"));
        Assert.Equal("Existing Tutorial", GetField<string>(instance, "_tutorialTitle"));
    }

    [Fact]
    public void OnPageTypeChanged_ForDefaultPage_UsesDefaultTutorialTitle()
    {
        var instance = new AdminTutorialsPanel();

        InvokePrivate(instance, "OnPageTypeChanged", "Page:Default");

        Assert.Equal("Default Tutorial", GetField<string>(instance, "_pageTypeName"));
        Assert.Equal("Default Tutorial", GetField<string>(instance, "_tutorialTitle"));
    }

    [Fact]
    public void OnPageTypeChanged_ForNonDefaultPage_AppendsTutorialSuffix()
    {
        var instance = new AdminTutorialsPanel();

        InvokePrivate(instance, "OnPageTypeChanged", "Page:Dashboard");

        Assert.Equal("Dashboard", GetField<string>(instance, "_pageTypeName"));
        Assert.Equal("Dashboard Tutorial", GetField<string>(instance, "_tutorialTitle"));
    }

    [Fact]
    public void ResetCreateForm_ClearsFields()
    {
        var instance = new AdminTutorialsPanel();
        SetField(instance, "_selectedPageType", "Page:Dashboard");
        SetField(instance, "_pageTypeName", "Dashboard");
        SetField(instance, "_tutorialTitle", "Dashboard Tutorial");

        InvokePrivate(instance, "ResetCreateForm");

        Assert.Null(GetField<string?>(instance, "_selectedPageType"));
        Assert.Equal(string.Empty, GetField<string>(instance, "_pageTypeName"));
        Assert.Equal(string.Empty, GetField<string>(instance, "_tutorialTitle"));
    }

    private static void InvokePrivate(object instance, string methodName, params object?[]? args)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(instance, args);
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
}
