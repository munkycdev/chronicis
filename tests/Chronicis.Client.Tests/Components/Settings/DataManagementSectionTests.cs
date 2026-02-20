using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Bunit;
using Chronicis.Client.Components.Settings;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Components.Settings;

[ExcludeFromCodeCoverage]
public class DataManagementSectionTests : MudBlazorTestContext
{
    private readonly IWorldApiService _worldApi = Substitute.For<IWorldApiService>();
    private readonly IExportApiService _exportApi = Substitute.For<IExportApiService>();
    private readonly ISnackbar _snackbar = Substitute.For<ISnackbar>();

    public DataManagementSectionTests()
    {
        Services.AddSingleton(_worldApi);
        Services.AddSingleton(_exportApi);
        Services.AddSingleton(_snackbar);
    }

    [Fact]
    public void ShowsNoWorldsMessage_WhenWorldListEmpty()
    {
        _worldApi.GetWorldsAsync().Returns(new List<WorldDto>());
        RenderComponent<MudPopoverProvider>();

        var cut = RenderComponent<DataManagementSection>();

        cut.WaitForAssertion(() =>
            Assert.Contains("don't have any worlds to export", cut.Markup, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ShowsNoWorldsMessage_WhenWorldListIsNull()
    {
        _worldApi.GetWorldsAsync().Returns((List<WorldDto>?)null);

        var cut = RenderComponent<DataManagementSection>();

        cut.WaitForAssertion(() =>
            Assert.Contains("don't have any worlds to export", cut.Markup, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ShowsWorldSelector_WhenWorldsExist()
    {
        var worlds = new List<WorldDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Faerun" }
        };
        _worldApi.GetWorldsAsync().Returns(worlds);
        RenderComponent<MudPopoverProvider>();

        var cut = RenderComponent<DataManagementSection>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Select World", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Export to Markdown", cut.Markup, StringComparison.Ordinal);
        });

        cut.Render();
        cut.WaitForAssertion(() =>
            Assert.Contains("Select World", cut.Markup, StringComparison.Ordinal));

        var select = cut.FindComponent<MudSelect<WorldDto>>();
        await cut.InvokeAsync(() => select.Instance.ValueChanged.InvokeAsync((WorldDto?)null));
        await cut.InvokeAsync(() => select.Instance.ValueChanged.InvokeAsync(worlds[0]));
        Assert.Equal(worlds[0], GetField<WorldDto?>(cut.Instance, "_selectedWorld"));
    }

    [Fact]
    public async Task ExportWorld_Success_SetsSuccessFlag()
    {
        var world = new WorldDto { Id = Guid.NewGuid(), Name = "Eberron" };
        _worldApi.GetWorldsAsync().Returns([world]);
        _exportApi.ExportWorldToMarkdownAsync(world.Id, world.Name).Returns(true);
        RenderComponent<MudPopoverProvider>();
        var cut = RenderComponent<DataManagementSection>();
        cut.WaitForState(() => cut.Markup.Contains("Export to Markdown", StringComparison.Ordinal));

        await cut.InvokeAsync(() => InvokePrivateAsync(cut.Instance, "ExportWorld"));

        await _exportApi.Received(1).ExportWorldToMarkdownAsync(world.Id, world.Name);
        Assert.True(GetField<bool?>(cut.Instance, "_exportSuccess"));
    }

    [Fact]
    public async Task ExportWorld_Exception_SetsFailureFlag()
    {
        var world = new WorldDto { Id = Guid.NewGuid(), Name = "Greyhawk" };
        _worldApi.GetWorldsAsync().Returns([world]);
        _exportApi.ExportWorldToMarkdownAsync(world.Id, world.Name).Returns<Task<bool>>(_ => throw new Exception("fail"));
        RenderComponent<MudPopoverProvider>();

        var cut = RenderComponent<DataManagementSection>();
        cut.WaitForState(() => cut.Markup.Contains("Export to Markdown", StringComparison.Ordinal));

        await cut.InvokeAsync(() => InvokePrivateAsync(cut.Instance, "ExportWorld"));

        Assert.False(GetField<bool?>(cut.Instance, "_exportSuccess"));
    }

    [Fact]
    public void ShowsLoadingIndicator_WhileWorldsAreLoading()
    {
        var tcs = new TaskCompletionSource<List<WorldDto>>();
        _worldApi.GetWorldsAsync().Returns(tcs.Task);
        RenderComponent<MudPopoverProvider>();

        var cut = RenderComponent<DataManagementSection>();

        Assert.NotEmpty(cut.FindAll(".mud-progress-linear"));
        tcs.SetResult([]);
    }

    [Fact]
    public async Task ExportWorld_WhenSelectedWorldIsNull_ReturnsEarly()
    {
        _worldApi.GetWorldsAsync().Returns(new List<WorldDto>());
        RenderComponent<MudPopoverProvider>();
        var cut = RenderComponent<DataManagementSection>();
        cut.WaitForState(() => cut.Markup.Contains("don't have any worlds to export", StringComparison.OrdinalIgnoreCase));

        await cut.InvokeAsync(() => InvokePrivateAsync(cut.Instance, "ExportWorld"));

        await _exportApi.DidNotReceive().ExportWorldToMarkdownAsync(Arg.Any<Guid>(), Arg.Any<string>());
    }

    [Fact]
    public async Task ExportWorld_Failure_SetsFailureFlagAndShowsErrorSnackbar()
    {
        var world = new WorldDto { Id = Guid.NewGuid(), Name = "Nentir Vale" };
        _worldApi.GetWorldsAsync().Returns([world]);
        _exportApi.ExportWorldToMarkdownAsync(world.Id, world.Name).Returns(false);
        RenderComponent<MudPopoverProvider>();
        var cut = RenderComponent<DataManagementSection>();
        cut.WaitForState(() => cut.Markup.Contains("Export to Markdown", StringComparison.Ordinal));

        await cut.InvokeAsync(() => InvokePrivateAsync(cut.Instance, "ExportWorld"));

        Assert.False(GetField<bool?>(cut.Instance, "_exportSuccess"));
        _snackbar.Received(1).Add("Export failed. Please try again.", Severity.Error);
    }

    [Fact]
    public void DismissExportAlert_ClearsSuccessState()
    {
        _worldApi.GetWorldsAsync().Returns(new List<WorldDto>());
        var cut = RenderComponent<DataManagementSection>();
        SetField(cut.Instance, "_exportSuccess", true);

        InvokePrivate(cut.Instance, "DismissExportAlert", (MudAlert?)null);

        Assert.Null(GetField<bool?>(cut.Instance, "_exportSuccess"));
    }

    private static async Task InvokePrivateAsync(object target, string methodName, params object[] args)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        var result = method!.Invoke(target, args);
        if (result is Task task)
        {
            await task;
        }
    }

    private static T GetField<T>(object target, string fieldName)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return (T)field!.GetValue(target)!;
    }

    private static void SetField(object target, string fieldName, object? value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field!.SetValue(target, value);
    }

    private static void InvokePrivate(object target, string methodName, params object[] args)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        _ = method!.Invoke(target, args);
    }

}
