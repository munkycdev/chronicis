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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _snackbar.Dispose();
        }

        base.Dispose(disposing);
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
    public async Task OnWorldChanged_WhenCurrentWorldNull_SelectsWorld()
    {
        EnsurePopoverProvider();
        var selectedWorldId = _appContext.Worlds[0].Id;
        _appContext.CurrentWorldId.Returns((Guid?)null);

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

    [Fact]
    public void Render_WhenNotInitialized_ShowsSpinner()
    {
        _appContext.IsInitialized.Returns(false);
        EnsurePopoverProvider();

        var cut = RenderComponent<WorldCampaignSelector>();

        Assert.NotEmpty(cut.FindAll(".mud-progress-circular"));
        Assert.DoesNotContain("No worlds available", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_WhenNoWorlds_ShowsInfoAlert()
    {
        _appContext.IsInitialized.Returns(true);
        _appContext.Worlds.Returns(new List<WorldDto>());
        EnsurePopoverProvider();

        var cut = RenderComponent<WorldCampaignSelector>();

        Assert.Contains("No worlds available", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_WhenWorldHasNoCampaigns_ShowsCreateFirstCampaignButton()
    {
        var worldId = _appContext.Worlds[0].Id;
        _appContext.CurrentWorld.Returns(new WorldDetailDto
        {
            Id = worldId,
            Name = "World A",
            Campaigns = new List<CampaignDto>()
        });
        EnsurePopoverProvider();

        var cut = RenderComponent<WorldCampaignSelector>();

        Assert.Contains("Create First Campaign", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_WhenWorldHasCampaigns_ShowsCampaignSelector()
    {
        var worldId = _appContext.Worlds[0].Id;
        var campaignId = Guid.NewGuid();
        _appContext.CurrentWorld.Returns(new WorldDetailDto
        {
            Id = worldId,
            Name = "World A",
            Campaigns = new List<CampaignDto> { new() { Id = campaignId, Name = "Campaign A" } }
        });
        EnsurePopoverProvider();

        var cut = RenderComponent<WorldCampaignSelector>();

        Assert.Contains("Campaign", cut.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Create First Campaign", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("Create new campaign", cut.Markup, StringComparison.Ordinal);
        var campaignItems = cut.FindComponents<MudSelectItem<Guid?>>();
        var campaignItemMarkup = Render(campaignItems.Last().Instance.ChildContent).Markup;
        Assert.Contains("Campaign A", campaignItemMarkup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OpenCreateWorldDialog_WhenDialogSucceeds_RefreshesAndSelectsWorld()
    {
        EnsurePopoverProvider();
        var newWorld = new WorldDto { Id = Guid.NewGuid(), Name = "World B" };
        var dialog = Substitute.For<IDialogReference>();
        dialog.Result.Returns(Task.FromResult<DialogResult?>(DialogResult.Ok(newWorld)));
        _dialogService.ShowAsync<CreateWorldDialog>(Arg.Any<string>())
            .Returns(Task.FromResult(dialog));

        var cut = RenderComponent<WorldCampaignSelector>();
        await InvokePrivateOnRendererAsync(cut, "OpenCreateWorldDialog");

        await _appContext.Received(1).RefreshWorldsAsync();
        await _appContext.Received(1).SelectWorldAsync(newWorld.Id);
        _snackbar.Received(1).Add("World 'World B' created!", Severity.Success);
    }

    [Fact]
    public async Task OpenCreateCampaignDialog_WhenDialogSucceeds_RefreshesAndSelectsCampaign()
    {
        EnsurePopoverProvider();
        var worldId = Guid.NewGuid();
        var campaign = new CampaignDto { Id = Guid.NewGuid(), Name = "Campaign B" };
        _appContext.CurrentWorldId.Returns((Guid?)worldId);
        var dialog = Substitute.For<IDialogReference>();
        dialog.Result.Returns(Task.FromResult<DialogResult?>(DialogResult.Ok(campaign)));
        _dialogService.ShowAsync<CreateCampaignDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>())
            .Returns(Task.FromResult(dialog));

        var cut = RenderComponent<WorldCampaignSelector>();
        await InvokePrivateOnRendererAsync(cut, "OpenCreateCampaignDialog");

        await _appContext.Received(1).RefreshCurrentWorldAsync();
        await _appContext.Received(1).SelectCampaignAsync(campaign.Id);
        _snackbar.Received(1).Add("Campaign 'Campaign B' created!", Severity.Success);
    }

    [Fact]
    public async Task OnWorldChanged_WhenEmptyGuid_DoesNotSelectWorld()
    {
        EnsurePopoverProvider();
        var cut = RenderComponent<WorldCampaignSelector>();

        await InvokePrivateOnRendererAsync(cut, "OnWorldChanged", Guid.Empty);

        await _appContext.DidNotReceive().SelectWorldAsync(Arg.Any<Guid>());
    }

    [Fact]
    public void OnContextChanged_EventRaised_UpdatesSelectedValues()
    {
        EnsurePopoverProvider();
        var worldId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        _appContext.CurrentWorldId.Returns((Guid?)null, worldId);
        _appContext.CurrentCampaignId.Returns((Guid?)null, campaignId);

        var cut = RenderComponent<WorldCampaignSelector>();

        _appContext.OnContextChanged += Raise.Event<Action>();

        var selectedWorldId = GetField<Guid>(cut.Instance, "_selectedWorldId");
        var selectedCampaignId = GetField<Guid?>(cut.Instance, "_selectedCampaignId");
        Assert.Equal(worldId, selectedWorldId);
        Assert.Equal(campaignId, selectedCampaignId);
    }

    [Fact]
    public async Task OpenCreateWorldDialog_WhenDialogResultIsNull_DoesNotRefresh()
    {
        EnsurePopoverProvider();
        var dialog = Substitute.For<IDialogReference>();
        dialog.Result.Returns(Task.FromResult<DialogResult?>(null));
        _dialogService.ShowAsync<CreateWorldDialog>(Arg.Any<string>())
            .Returns(Task.FromResult(dialog));

        var cut = RenderComponent<WorldCampaignSelector>();
        await InvokePrivateOnRendererAsync(cut, "OpenCreateWorldDialog");

        await _appContext.DidNotReceive().RefreshWorldsAsync();
        await _appContext.DidNotReceive().SelectWorldAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task OnContextChanged_WhenInvoked_RefreshesSelections()
    {
        EnsurePopoverProvider();
        var cut = RenderComponent<WorldCampaignSelector>();
        var worldId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        _appContext.CurrentWorldId.Returns((Guid?)worldId);
        _appContext.CurrentCampaignId.Returns((Guid?)campaignId);

        await InvokePrivateOnRendererAsync(cut, "OnContextChanged");

        var selectedWorldId = GetField<Guid>(cut.Instance, "_selectedWorldId");
        var selectedCampaignId = GetField<Guid?>(cut.Instance, "_selectedCampaignId");
        Assert.Equal(worldId, selectedWorldId);
        Assert.Equal(campaignId, selectedCampaignId);
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

    private static T GetField<T>(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return (T)field!.GetValue(instance)!;
    }

    private void EnsurePopoverProvider()
    {
        _ = RenderComponent<MudPopoverProvider>();
    }
}
