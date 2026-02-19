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

    public DataManagementSectionTests()
    {
        Services.AddSingleton(_worldApi);
        Services.AddSingleton(_exportApi);
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
}
