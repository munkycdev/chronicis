using Chronicis.Client.Abstractions;
using Chronicis.Client.Services;
using Chronicis.Client.ViewModels;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.Logging;
using MudBlazor;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.ViewModels;

public class CampaignDetailViewModelTests
{
    private record Sut(
        CampaignDetailViewModel Vm,
        ICampaignApiService CampaignApi,
        IArcApiService ArcApi,
        IWorldApiService WorldApi,
        ITreeStateService TreeState,
        IBreadcrumbService BreadcrumbService,
        IAppNavigator Navigator,
        IUserNotifier Notifier,
        IPageTitleService TitleService,
        IDialogService DialogService);

    private static Sut CreateSut()
    {
        var campaignApi = Substitute.For<ICampaignApiService>();
        var arcApi = Substitute.For<IArcApiService>();
        var worldApi = Substitute.For<IWorldApiService>();
        var treeState = Substitute.For<ITreeStateService>();
        var breadcrumbs = Substitute.For<IBreadcrumbService>();
        var navigator = Substitute.For<IAppNavigator>();
        var notifier = Substitute.For<IUserNotifier>();
        var titleService = Substitute.For<IPageTitleService>();
        var dialogService = Substitute.For<IDialogService>();
        var logger = Substitute.For<ILogger<CampaignDetailViewModel>>();

        var vm = new CampaignDetailViewModel(
            campaignApi, arcApi, worldApi, treeState, breadcrumbs,
            navigator, notifier, titleService, dialogService, logger);

        return new Sut(vm, campaignApi, arcApi, worldApi, treeState,
            breadcrumbs, navigator, notifier, titleService, dialogService);
    }

    private static CampaignDetailDto MakeCampaign(Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Name = "Test Campaign",
        Description = "A test",
        WorldId = Guid.NewGuid(),
        IsActive = false,
        ArcCount = 0,
        OwnerName = "DM Dave",
        CreatedAt = new DateTime(2025, 1, 1)
    };

    // -----------------------------------------------------------------------
    // LoadAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task LoadAsync_WhenCampaignFound_PopulatesProperties()
    {
        var c = CreateSut();
        var campaign = MakeCampaign();
        c.CampaignApi.GetCampaignAsync(campaign.Id).Returns(campaign);
        c.ArcApi.GetArcsByCampaignAsync(campaign.Id).Returns(new List<ArcDto>());
        c.WorldApi.GetWorldAsync(campaign.WorldId).Returns(new WorldDetailDto { Id = campaign.WorldId });

        await c.Vm.LoadAsync(campaign.Id);

        Assert.Equal(campaign, c.Vm.Campaign);
        Assert.Equal(campaign.Name, c.Vm.EditName);
        Assert.Equal(campaign.Description, c.Vm.EditDescription);
        Assert.False(c.Vm.HasUnsavedChanges);
        Assert.False(c.Vm.IsLoading);
    }

    [Fact]
    public async Task LoadAsync_WhenCampaignNotFound_NavigatesToDashboard()
    {
        var c = CreateSut();
        var id = Guid.NewGuid();
        c.CampaignApi.GetCampaignAsync(id).Returns((CampaignDetailDto?)null);

        await c.Vm.LoadAsync(id);

        c.Navigator.Received(1).NavigateTo("/dashboard", replace: true);
    }

    [Fact]
    public async Task LoadAsync_WhenWorldFound_BuildsBreadcrumbsViaService()
    {
        var c = CreateSut();
        var campaign = MakeCampaign();
        var world = new WorldDetailDto { Id = campaign.WorldId, Name = "Faerun" };
        var expectedCrumbs = new List<BreadcrumbItem> { new("Dashboard", "/dashboard") };
        c.CampaignApi.GetCampaignAsync(campaign.Id).Returns(campaign);
        c.ArcApi.GetArcsByCampaignAsync(campaign.Id).Returns(new List<ArcDto>());
        c.WorldApi.GetWorldAsync(campaign.WorldId).Returns(world);
        c.BreadcrumbService.ForCampaign(campaign, world).Returns(expectedCrumbs);

        await c.Vm.LoadAsync(campaign.Id);

        Assert.Equal(expectedCrumbs, c.Vm.Breadcrumbs);
    }

    [Fact]
    public async Task LoadAsync_WhenWorldNull_UsesFallbackBreadcrumbs()
    {
        var c = CreateSut();
        var campaign = MakeCampaign();
        c.CampaignApi.GetCampaignAsync(campaign.Id).Returns(campaign);
        c.ArcApi.GetArcsByCampaignAsync(campaign.Id).Returns(new List<ArcDto>());
        c.WorldApi.GetWorldAsync(campaign.WorldId).Returns((WorldDetailDto?)null);

        await c.Vm.LoadAsync(campaign.Id);

        Assert.Contains(c.Vm.Breadcrumbs, b => b.Text == campaign.Name);
    }

    [Fact]
    public async Task LoadAsync_SetsPageTitleAndHighlightsTree()
    {
        var c = CreateSut();
        var campaign = MakeCampaign();
        c.CampaignApi.GetCampaignAsync(campaign.Id).Returns(campaign);
        c.ArcApi.GetArcsByCampaignAsync(campaign.Id).Returns(new List<ArcDto>());
        c.WorldApi.GetWorldAsync(campaign.WorldId).Returns(new WorldDetailDto());

        await c.Vm.LoadAsync(campaign.Id);

        await c.TitleService.Received(1).SetTitleAsync(campaign.Name);
        c.TreeState.Received(1).ExpandPathToAndSelect(campaign.Id);
    }

    [Fact]
    public async Task LoadAsync_WhenApiThrows_SetsIsLoadingFalseAndNotifiesError()
    {
        var c = CreateSut();
        var id = Guid.NewGuid();
        c.CampaignApi.GetCampaignAsync(id).Returns(Task.FromException<CampaignDetailDto?>(new Exception("boom")));

        await c.Vm.LoadAsync(id);

        Assert.False(c.Vm.IsLoading);
        c.Notifier.Received(1).Error(Arg.Any<string>());
    }

    // -----------------------------------------------------------------------
    // EditName / EditDescription change tracking
    // -----------------------------------------------------------------------

    [Fact]
    public async Task EditName_WhenChanged_SetsHasUnsavedChanges()
    {
        var c = CreateSut();
        var campaign = MakeCampaign();
        c.CampaignApi.GetCampaignAsync(campaign.Id).Returns(campaign);
        c.ArcApi.GetArcsByCampaignAsync(campaign.Id).Returns(new List<ArcDto>());
        c.WorldApi.GetWorldAsync(campaign.WorldId).Returns(new WorldDetailDto());
        await c.Vm.LoadAsync(campaign.Id);

        c.Vm.EditName = "New Name";

        Assert.True(c.Vm.HasUnsavedChanges);
    }

    [Fact]
    public async Task EditDescription_WhenChanged_SetsHasUnsavedChanges()
    {
        var c = CreateSut();
        var campaign = MakeCampaign();
        c.CampaignApi.GetCampaignAsync(campaign.Id).Returns(campaign);
        c.ArcApi.GetArcsByCampaignAsync(campaign.Id).Returns(new List<ArcDto>());
        c.WorldApi.GetWorldAsync(campaign.WorldId).Returns(new WorldDetailDto());
        await c.Vm.LoadAsync(campaign.Id);

        c.Vm.EditDescription = "New description";

        Assert.True(c.Vm.HasUnsavedChanges);
    }

    // -----------------------------------------------------------------------
    // SaveAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task SaveAsync_WhenSucceeds_ClearsUnsavedChangesAndNotifiesSuccess()
    {
        var c = CreateSut();
        var campaign = MakeCampaign();
        c.CampaignApi.GetCampaignAsync(campaign.Id).Returns(campaign);
        c.ArcApi.GetArcsByCampaignAsync(campaign.Id).Returns(new List<ArcDto>());
        c.WorldApi.GetWorldAsync(campaign.WorldId).Returns(new WorldDetailDto());
        await c.Vm.LoadAsync(campaign.Id);
        c.Vm.EditName = "Updated Name";

        var updated = MakeCampaign(campaign.Id);
        updated.Name = "Updated Name";
        c.CampaignApi.UpdateCampaignAsync(campaign.Id, Arg.Any<CampaignUpdateDto>()).Returns(updated);

        await c.Vm.SaveAsync();

        Assert.False(c.Vm.HasUnsavedChanges);
        Assert.False(c.Vm.IsSaving);
        c.Notifier.Received(1).Success(Arg.Any<string>());
    }

    [Fact]
    public async Task SaveAsync_WhenApiThrows_NotifiesError()
    {
        var c = CreateSut();
        var campaign = MakeCampaign();
        c.CampaignApi.GetCampaignAsync(campaign.Id).Returns(campaign);
        c.ArcApi.GetArcsByCampaignAsync(campaign.Id).Returns(new List<ArcDto>());
        c.WorldApi.GetWorldAsync(campaign.WorldId).Returns(new WorldDetailDto());
        await c.Vm.LoadAsync(campaign.Id);

        c.CampaignApi.UpdateCampaignAsync(campaign.Id, Arg.Any<CampaignUpdateDto>())
            .Returns(Task.FromException<CampaignDto?>(new Exception("save failed")));

        await c.Vm.SaveAsync();

        Assert.False(c.Vm.IsSaving);
        c.Notifier.Received(1).Error(Arg.Any<string>());
    }

    [Fact]
    public async Task SaveAsync_WhenCampaignNull_DoesNothing()
    {
        var c = CreateSut();
        await c.Vm.SaveAsync();
        await c.CampaignApi.DidNotReceive().UpdateCampaignAsync(Arg.Any<Guid>(), Arg.Any<CampaignUpdateDto>());
    }

    // -----------------------------------------------------------------------
    // OnActiveToggleAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task OnActiveToggle_WhenActivateSucceeds_SetsCampaignActive()
    {
        var c = CreateSut();
        var campaign = MakeCampaign();
        c.CampaignApi.GetCampaignAsync(campaign.Id).Returns(campaign);
        c.ArcApi.GetArcsByCampaignAsync(campaign.Id).Returns(new List<ArcDto>());
        c.WorldApi.GetWorldAsync(campaign.WorldId).Returns(new WorldDetailDto());
        await c.Vm.LoadAsync(campaign.Id);

        c.CampaignApi.ActivateCampaignAsync(campaign.Id).Returns(true);

        await c.Vm.OnActiveToggleAsync(true);

        Assert.True(c.Vm.Campaign!.IsActive);
        c.Notifier.Received(1).Success(Arg.Any<string>());
    }

    [Fact]
    public async Task OnActiveToggle_WhenActivateFails_NotifiesError()
    {
        var c = CreateSut();
        var campaign = MakeCampaign();
        c.CampaignApi.GetCampaignAsync(campaign.Id).Returns(campaign);
        c.ArcApi.GetArcsByCampaignAsync(campaign.Id).Returns(new List<ArcDto>());
        c.WorldApi.GetWorldAsync(campaign.WorldId).Returns(new WorldDetailDto());
        await c.Vm.LoadAsync(campaign.Id);

        c.CampaignApi.ActivateCampaignAsync(campaign.Id).Returns(false);

        await c.Vm.OnActiveToggleAsync(true);

        c.Notifier.Received(1).Error(Arg.Any<string>());
    }

    [Fact]
    public async Task OnActiveToggle_WhenDeactivate_NotifiesInfo()
    {
        var c = CreateSut();
        var campaign = MakeCampaign();
        c.CampaignApi.GetCampaignAsync(campaign.Id).Returns(campaign);
        c.ArcApi.GetArcsByCampaignAsync(campaign.Id).Returns(new List<ArcDto>());
        c.WorldApi.GetWorldAsync(campaign.WorldId).Returns(new WorldDetailDto());
        await c.Vm.LoadAsync(campaign.Id);

        await c.Vm.OnActiveToggleAsync(false);

        c.Notifier.Received(1).Info(Arg.Any<string>());
        await c.CampaignApi.DidNotReceive().ActivateCampaignAsync(Arg.Any<Guid>());
    }

    // -----------------------------------------------------------------------
    // NavigateToArc
    // -----------------------------------------------------------------------

    [Fact]
    public void NavigateToArc_NavigatesToArcUrl()
    {
        var c = CreateSut();
        var arcId = Guid.NewGuid();

        c.Vm.NavigateToArc(arcId);

        c.Navigator.Received(1).NavigateTo($"/arc/{arcId}");
    }
}
