using Bunit;
using Bunit.TestDoubles;
using Chronicis.Client.Abstractions;
using Chronicis.Client.Pages;
using Chronicis.Client.Services;
using Chronicis.Client.Tests.Components;
using Chronicis.Client.ViewModels;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.DTOs.Sessions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MudBlazor;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Pages;

/// <summary>
/// Tests for the ArcDetail page shell.
/// Business logic is covered by ArcDetailViewModelTests.
/// These tests verify the shell renders correctly based on ViewModel state.
/// </summary>
public class ArcDetailTests : MudBlazorTestContext
{
    private ArcDetailViewModel CreateViewModel(
        IArcApiService? arcApi = null,
        ICampaignApiService? campaignApi = null,
        IWorldApiService? worldApi = null,
        ISessionApiService? sessionApi = null,
        IQuestApiService? questApi = null,
        IAuthService? authService = null,
        ITreeStateService? treeState = null,
        IBreadcrumbService? breadcrumbs = null,
        IAppNavigator? navigator = null,
        IUserNotifier? notifier = null,
        IPageTitleService? titleService = null,
        IConfirmationService? confirmation = null)
    {
        arcApi ??= Substitute.For<IArcApiService>();
        campaignApi ??= Substitute.For<ICampaignApiService>();
        worldApi ??= Substitute.For<IWorldApiService>();
        sessionApi ??= Substitute.For<ISessionApiService>();
        questApi ??= Substitute.For<IQuestApiService>();
        authService ??= Substitute.For<IAuthService>();
        treeState ??= Substitute.For<ITreeStateService>();
        breadcrumbs ??= Substitute.For<IBreadcrumbService>();
        navigator ??= Substitute.For<IAppNavigator>();
        notifier ??= Substitute.For<IUserNotifier>();
        titleService ??= Substitute.For<IPageTitleService>();
        confirmation ??= Substitute.For<IConfirmationService>();
        var logger = Substitute.For<ILogger<ArcDetailViewModel>>();

        return new ArcDetailViewModel(
            arcApi, campaignApi, worldApi, sessionApi, questApi, authService,
            treeState, breadcrumbs, navigator, notifier, titleService, confirmation, logger);
    }

    private IRenderedComponent<ArcDetail> RenderWithViewModel(ArcDetailViewModel vm, Guid? arcId = null)
    {
        Services.AddSingleton(vm);
        Services.GetRequiredService<FakeNavigationManager>().NavigateTo("http://localhost/arc/test");

        // Stub child components that inject services not relevant to page shell tests
        ComponentFactories.AddStub<Chronicis.Client.Components.Quests.ArcQuestList>();
        ComponentFactories.AddStub<Chronicis.Client.Components.Shared.AISummarySection>();
        ComponentFactories.AddStub<Chronicis.Client.Components.Quests.ArcQuestEditor>();
        ComponentFactories.AddStub<Chronicis.Client.Components.Quests.ArcQuestTimeline>();

        return RenderComponent<ArcDetail>(p =>
            p.Add(c => c.ArcId, arcId ?? Guid.NewGuid()));
    }

    [Fact]
    public void ArcDetail_WhenIsLoadingTrue_RendersLoadingSkeleton()
    {
        var tcs = new TaskCompletionSource<ArcDto?>();
        var arcApi = Substitute.For<IArcApiService>();
        arcApi.GetArcAsync(Arg.Any<Guid>()).Returns(tcs.Task);

        var vm = CreateViewModel(arcApi: arcApi);
        var cut = RenderWithViewModel(vm);

        Assert.Contains("chronicis-loading-skeleton", cut.Markup, StringComparison.OrdinalIgnoreCase);

        tcs.SetResult(null);
    }

    [Fact]
    public void ArcDetail_WhenArcLoaded_ShowsArcName()
    {
        var arcId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var worldId = Guid.NewGuid();

        var arc = new ArcDto { Id = arcId, Name = "The Shadow Rising", CampaignId = campaignId };
        var campaign = new CampaignDto { Id = campaignId, Name = "Test Campaign", WorldId = worldId };
        var world = new WorldDetailDto { Id = worldId, Name = "Test World" };

        var arcApi = Substitute.For<IArcApiService>();
        arcApi.GetArcAsync(arcId).Returns(arc);

        var campaignApi = Substitute.For<ICampaignApiService>();
        campaignApi.GetCampaignAsync(campaignId).Returns(new CampaignDetailDto
        { Id = campaignId, Name = "Test Campaign", WorldId = worldId });

        var worldApi = Substitute.For<IWorldApiService>();
        worldApi.GetWorldAsync(worldId).Returns(world);

        var sessionApi = Substitute.For<ISessionApiService>();
        sessionApi.GetSessionsByArcAsync(arcId).Returns(new List<SessionTreeDto>());

        var authService = Substitute.For<IAuthService>();
        authService.GetCurrentUserAsync().Returns((UserInfo?)null);

        var breadcrumbs = Substitute.For<IBreadcrumbService>();
        breadcrumbs.ForArc(Arg.Any<ArcDto>(), Arg.Any<CampaignDto>(), Arg.Any<WorldDto>())
            .Returns(new List<BreadcrumbItem>());

        var vm = CreateViewModel(arcApi: arcApi, campaignApi: campaignApi, worldApi: worldApi,
            sessionApi: sessionApi, authService: authService, breadcrumbs: breadcrumbs);
        var cut = RenderWithViewModel(vm, arcId);

        cut.WaitForAssertion(() =>
            Assert.Contains("The Shadow Rising", cut.Markup, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ArcDetail_WhenNoSessions_ShowsNoSessionsMessage()
    {
        var arcId = Guid.NewGuid();
        var campaignId = Guid.NewGuid();
        var worldId = Guid.NewGuid();

        var arc = new ArcDto { Id = arcId, Name = "Empty Arc", CampaignId = campaignId };

        var arcApi = Substitute.For<IArcApiService>();
        arcApi.GetArcAsync(arcId).Returns(arc);

        var campaignApi = Substitute.For<ICampaignApiService>();
        campaignApi.GetCampaignAsync(campaignId).Returns(
            new CampaignDetailDto { Id = campaignId, WorldId = worldId });

        var worldApi = Substitute.For<IWorldApiService>();
        worldApi.GetWorldAsync(worldId).Returns(new WorldDetailDto { Id = worldId });

        var sessionApi = Substitute.For<ISessionApiService>();
        sessionApi.GetSessionsByArcAsync(arcId).Returns(new List<SessionTreeDto>());

        var authService = Substitute.For<IAuthService>();
        authService.GetCurrentUserAsync().Returns((UserInfo?)null);

        var breadcrumbs = Substitute.For<IBreadcrumbService>();
        breadcrumbs.ForArc(Arg.Any<ArcDto>(), Arg.Any<CampaignDto>(), Arg.Any<WorldDto>())
            .Returns(new List<BreadcrumbItem>());

        var vm = CreateViewModel(arcApi: arcApi, campaignApi: campaignApi, worldApi: worldApi,
            sessionApi: sessionApi, authService: authService, breadcrumbs: breadcrumbs);
        var cut = RenderWithViewModel(vm, arcId);

        cut.WaitForAssertion(() =>
            Assert.Contains("No sessions yet", cut.Markup, StringComparison.OrdinalIgnoreCase));
    }
}
