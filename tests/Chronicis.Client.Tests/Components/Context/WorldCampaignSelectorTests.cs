using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Bunit;
using Chronicis.Client.Components.Context;
using Chronicis.Client.Components.Dialogs;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Components.Context;

[ExcludeFromCodeCoverage]
public class WorldCampaignSelectorTests : MudBlazorTestContext
{
    private readonly IAppContextService _appContext = Substitute.For<IAppContextService>();
    private readonly IDialogService _dialogService = Substitute.For<IDialogService>();
    private readonly ISnackbar _snackbar = Substitute.For<ISnackbar>();

    public WorldCampaignSelectorTests()
    {
        _appContext.IsInitialized.Returns(true);
        _appContext.Worlds.Returns(new List<WorldDto>
        {
            new() { Id = Guid.NewGuid(), Name = "World A" }
        });
        _appContext.CurrentWorldId.Returns((Guid?)null);
        _appContext.CurrentCampaignId.Returns((Guid?)null);
        _appContext.CurrentWorld.Returns((WorldDetailDto?)null);

        Services.AddSingleton(_appContext);
        Services.AddSingleton(_dialogService);
        Services.AddSingleton(_snackbar);
    }

    [Fact]
    public async Task OnWorldChanged_WhenDifferentWorld_SelectsWorld()
    {
        EnsurePopoverProvider();
        var selectedWorldId = _appContext.Worlds[0].Id;
        _appContext.CurrentWorldId.Returns((Guid?)Guid.NewGuid());

        var cut = RenderComponent<WorldCampaignSelector>();
        await InvokePrivateOnRendererAsync(cut, "OnWorldChanged", selectedWorldId);

        await _appContext.Received(1).SelectWorldAsync(selectedWorldId);
    }

    [Fact]
    public async Task OnWorldChanged_WhenSameWorld_DoesNothing()
    {
        EnsurePopoverProvider();
        var selectedWorldId = _appContext.Worlds[0].Id;
        _appContext.CurrentWorldId.Returns((Guid?)selectedWorldId);

        var cut = RenderComponent<WorldCampaignSelector>();
        await InvokePrivateOnRendererAsync(cut, "OnWorldChanged", selectedWorldId);

        await _appContext.DidNotReceive().SelectWorldAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task OnCampaignChanged_SelectsCampaign()
    {
        EnsurePopoverProvider();
        var campaignId = Guid.NewGuid();
        var cut = RenderComponent<WorldCampaignSelector>();

        await InvokePrivateOnRendererAsync(cut, "OnCampaignChanged", (Guid?)campaignId);

        await _appContext.Received(1).SelectCampaignAsync(campaignId);
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        EnsurePopoverProvider();
        var cut = RenderComponent<WorldCampaignSelector>();
        cut.Instance.Dispose();
    }

    [Fact]
    public async Task OpenCreateCampaignDialog_WhenNoCurrentWorld_ReturnsEarly()
    {
        EnsurePopoverProvider();
        _appContext.CurrentWorldId.Returns((Guid?)null);
        var cut = RenderComponent<WorldCampaignSelector>();

        await InvokePrivateOnRendererAsync(cut, "OpenCreateCampaignDialog");

        await _dialogService.DidNotReceive().ShowAsync<CreateCampaignDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>());
    }

    [Fact]
    public async Task OpenCreateCampaignDialog_WhenDialogCanceled_DoesNotRefresh()
    {
        EnsurePopoverProvider();
        var worldId = Guid.NewGuid();
        _appContext.CurrentWorldId.Returns((Guid?)worldId);
        var dialog = Substitute.For<IDialogReference>();
        dialog.Result.Returns(Task.FromResult<DialogResult?>(DialogResult.Cancel()));
        _dialogService.ShowAsync<CreateCampaignDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>())
            .Returns(Task.FromResult(dialog));

        var cut = RenderComponent<WorldCampaignSelector>();
        await InvokePrivateOnRendererAsync(cut, "OpenCreateCampaignDialog");

        await _appContext.DidNotReceive().RefreshCurrentWorldAsync();
        await _appContext.DidNotReceive().SelectCampaignAsync(Arg.Any<Guid?>());
    }

    [Fact]
    public async Task OpenCreateWorldDialog_WhenDialogCanceled_DoesNotRefresh()
    {
        EnsurePopoverProvider();
        var dialog = Substitute.For<IDialogReference>();
        dialog.Result.Returns(Task.FromResult<DialogResult?>(DialogResult.Cancel()));
        _dialogService.ShowAsync<CreateWorldDialog>(Arg.Any<string>())
            .Returns(Task.FromResult(dialog));

        var cut = RenderComponent<WorldCampaignSelector>();
        await InvokePrivateOnRendererAsync(cut, "OpenCreateWorldDialog");

        await _appContext.DidNotReceive().RefreshWorldsAsync();
        await _appContext.DidNotReceive().SelectWorldAsync(Arg.Any<Guid>());
    }

    private static Task InvokePrivateOnRendererAsync(IRenderedComponent<WorldCampaignSelector> cut, string methodName, params object[] args)
    {
        return cut.InvokeAsync(async () =>
        {
            var method = cut.Instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);
            var result = method!.Invoke(cut.Instance, args);
            if (result is Task task)
            {
                await task;
            }
        });
    }

    private void EnsurePopoverProvider()
    {
        _ = RenderComponent<MudPopoverProvider>();
    }
}
