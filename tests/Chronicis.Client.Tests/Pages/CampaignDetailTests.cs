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
using MudBlazor;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Pages;

/// <summary>
/// Tests for the CampaignDetail page shell.
/// Business logic is covered by CampaignDetailViewModelTests.
/// These tests verify the shell renders correctly based on ViewModel state.
/// </summary>
public class CampaignDetailTests : MudBlazorTestContext
{
    private CampaignDetailViewModel CreateViewModel(
        ICampaignApiService? campaignApi = null,
        IArcApiService? arcApi = null,
        IWorldApiService? worldApi = null,
        IAuthService? authService = null,
        ITreeStateService? treeState = null,
        IBreadcrumbService? breadcrumbs = null,
        IAppNavigator? navigator = null,
        IUserNotifier? notifier = null,
        IPageTitleService? titleService = null,
        IDialogService? dialogService = null)
    {
        campaignApi ??= Substitute.For<ICampaignApiService>();
        arcApi ??= Substitute.For<IArcApiService>();
        worldApi ??= Substitute.For<IWorldApiService>();
        authService ??= Substitute.For<IAuthService>();
        treeState ??= Substitute.For<ITreeStateService>();
        breadcrumbs ??= Substitute.For<IBreadcrumbService>();
        navigator ??= Substitute.For<IAppNavigator>();
        notifier ??= Substitute.For<IUserNotifier>();
        titleService ??= Substitute.For<IPageTitleService>();
        dialogService ??= Substitute.For<IDialogService>();
        var logger = Substitute.For<ILogger<CampaignDetailViewModel>>();

        return new CampaignDetailViewModel(
            campaignApi, arcApi, worldApi, authService, treeState, breadcrumbs,
            navigator, notifier, titleService, dialogService, logger);
    }

    private IRenderedComponent<CampaignDetail> RenderWithViewModel(CampaignDetailViewModel vm, Guid? campaignId = null)
    {
        Services.AddSingleton(vm);
        Services.GetRequiredService<FakeNavigationManager>().NavigateTo("http://localhost/campaign/test");

        // Stub child components that inject services not relevant to page shell tests
        ComponentFactories.AddStub<Chronicis.Client.Components.Shared.AISummarySection>();

        return RenderComponent<CampaignDetail>(p =>
            p.Add(c => c.CampaignId, campaignId ?? Guid.NewGuid()));
    }

    [Fact]
    public void CampaignDetail_WhenIsLoadingTrue_RendersLoadingSkeleton()
    {
        // Pause load so IsLoading stays true
        var tcs = new TaskCompletionSource<CampaignDetailDto?>();
        var campaignApi = Substitute.For<ICampaignApiService>();
        campaignApi.GetCampaignAsync(Arg.Any<Guid>()).Returns(tcs.Task);

        var vm = CreateViewModel(campaignApi: campaignApi);
        var cut = RenderWithViewModel(vm);

        // LoadingSkeleton renders a mud-skeleton
        Assert.Contains("chronicis-loading-skeleton", cut.Markup, StringComparison.OrdinalIgnoreCase);

        tcs.SetResult(null);
    }

    [Fact]
    public void CampaignDetail_WhenCampaignLoaded_ShowsCampaignName()
    {
        var campaignId = Guid.NewGuid();
        var campaign = new CampaignDetailDto
        {
            Id = campaignId,
            Name = "The Fellowship Campaign",
            WorldId = Guid.NewGuid(),
            OwnerName = "Gandalf"
        };

        var campaignApi = Substitute.For<ICampaignApiService>();
        campaignApi.GetCampaignAsync(campaignId).Returns(campaign);

        var arcApi = Substitute.For<IArcApiService>();
        arcApi.GetArcsByCampaignAsync(campaignId).Returns(new List<ArcDto>());

        var worldApi = Substitute.For<IWorldApiService>();
        worldApi.GetWorldAsync(campaign.WorldId).Returns(new WorldDetailDto { Name = "Middle Earth" });

        var breadcrumbs = Substitute.For<IBreadcrumbService>();
        breadcrumbs.ForCampaign(Arg.Any<CampaignDto>(), Arg.Any<WorldDto>()).Returns(new List<BreadcrumbItem>());

        var vm = CreateViewModel(campaignApi: campaignApi, arcApi: arcApi, worldApi: worldApi, breadcrumbs: breadcrumbs);
        var cut = RenderWithViewModel(vm, campaignId);

        cut.WaitForAssertion(() =>
            Assert.Contains("The Fellowship Campaign", cut.Markup, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CampaignDetail_WhenNoArcs_ShowsNoArcsMessage()
    {
        var campaignId = Guid.NewGuid();
        var campaign = new CampaignDetailDto { Id = campaignId, Name = "Empty Campaign", WorldId = Guid.NewGuid() };

        var campaignApi = Substitute.For<ICampaignApiService>();
        campaignApi.GetCampaignAsync(campaignId).Returns(campaign);

        var arcApi = Substitute.For<IArcApiService>();
        arcApi.GetArcsByCampaignAsync(campaignId).Returns(new List<ArcDto>());

        var worldApi = Substitute.For<IWorldApiService>();
        worldApi.GetWorldAsync(campaign.WorldId).Returns(new WorldDetailDto());

        var breadcrumbs = Substitute.For<IBreadcrumbService>();
        breadcrumbs.ForCampaign(Arg.Any<CampaignDto>(), Arg.Any<WorldDto>()).Returns(new List<BreadcrumbItem>());

        var vm = CreateViewModel(campaignApi: campaignApi, arcApi: arcApi, worldApi: worldApi, breadcrumbs: breadcrumbs);
        var cut = RenderWithViewModel(vm, campaignId);

        cut.WaitForAssertion(() =>
            Assert.Contains("No arcs yet", cut.Markup, StringComparison.OrdinalIgnoreCase));
    }
}
