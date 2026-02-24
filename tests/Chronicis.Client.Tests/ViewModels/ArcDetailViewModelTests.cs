using Chronicis.Client.Abstractions;
using Chronicis.Client.Services;
using Chronicis.Client.ViewModels;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.DTOs.Quests;
using Chronicis.Shared.DTOs.Sessions;
using Chronicis.Shared.Enums;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.ViewModels;

public class ArcDetailViewModelTests
{
    private record Sut(
        ArcDetailViewModel Vm,
        IArcApiService ArcApi,
        ICampaignApiService CampaignApi,
        IWorldApiService WorldApi,
        ISessionApiService SessionApi,
        IQuestApiService QuestApi,
        IAuthService AuthService,
        ITreeStateService TreeState,
        IBreadcrumbService BreadcrumbService,
        IAppNavigator Navigator,
        IUserNotifier Notifier,
        IPageTitleService TitleService,
        IConfirmationService Confirmation);

    private static Sut CreateSut()
    {
        var arcApi = Substitute.For<IArcApiService>();
        var campaignApi = Substitute.For<ICampaignApiService>();
        var worldApi = Substitute.For<IWorldApiService>();
        var sessionApi = Substitute.For<ISessionApiService>();
        var questApi = Substitute.For<IQuestApiService>();
        var authService = Substitute.For<IAuthService>();
        var treeState = Substitute.For<ITreeStateService>();
        var breadcrumbs = Substitute.For<IBreadcrumbService>();
        var navigator = Substitute.For<IAppNavigator>();
        var notifier = Substitute.For<IUserNotifier>();
        var titleService = Substitute.For<IPageTitleService>();
        var confirmation = Substitute.For<IConfirmationService>();
        var logger = Substitute.For<ILogger<ArcDetailViewModel>>();

        var vm = new ArcDetailViewModel(
            arcApi, campaignApi, worldApi, sessionApi, questApi, authService,
            treeState, breadcrumbs, navigator, notifier, titleService, confirmation, logger);

        return new Sut(vm, arcApi, campaignApi, worldApi, sessionApi, questApi,
            authService, treeState, breadcrumbs, navigator, notifier, titleService, confirmation);
    }

    private static ArcDto MakeArc(Guid? id = null, Guid? campaignId = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        CampaignId = campaignId ?? Guid.NewGuid(),
        Name = "Test Arc",
        Description = "Arc description",
        SortOrder = 1,
        IsActive = false,
        SessionCount = 0,
        CreatedAt = new DateTime(2025, 1, 1)
    };

    private static CampaignDto MakeCampaign(Guid? id = null, Guid? worldId = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        WorldId = worldId ?? Guid.NewGuid(),
        Name = "Test Campaign"
    };

    private void SetupHappyPath(Sut c, ArcDto arc, CampaignDto campaign, WorldDetailDto? world = null)
    {
        world ??= new WorldDetailDto { Id = campaign.WorldId };
        c.ArcApi.GetArcAsync(arc.Id).Returns(arc);
        c.CampaignApi.GetCampaignAsync(arc.CampaignId).Returns(new CampaignDetailDto
        {
            Id = campaign.Id,
            WorldId = campaign.WorldId,
            Name = campaign.Name
        });
        c.WorldApi.GetWorldAsync(campaign.WorldId).Returns(world);
        c.WorldApi.GetWorldAsync(world.Id).Returns(world);
        c.SessionApi.GetSessionsByArcAsync(arc.Id).Returns(new List<SessionTreeDto>());
        c.AuthService.GetCurrentUserAsync().Returns((UserInfo?)null);
    }

    // -----------------------------------------------------------------------
    // LoadAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task LoadAsync_WhenArcFound_PopulatesProperties()
    {
        var c = CreateSut();
        var arc = MakeArc();
        var campaign = MakeCampaign(id: arc.CampaignId);
        SetupHappyPath(c, arc, campaign);

        await c.Vm.LoadAsync(arc.Id);

        Assert.Equal(arc, c.Vm.Arc);
        Assert.Equal(arc.Name, c.Vm.EditName);
        Assert.Equal(arc.Description, c.Vm.EditDescription);
        Assert.Equal(arc.SortOrder, c.Vm.EditSortOrder);
        Assert.False(c.Vm.HasUnsavedChanges);
        Assert.False(c.Vm.IsLoading);
    }

    [Fact]
    public async Task LoadAsync_WhenArcNotFound_NavigatesToDashboard()
    {
        var c = CreateSut();
        var id = Guid.NewGuid();
        c.ArcApi.GetArcAsync(id).Returns((ArcDto?)null);

        await c.Vm.LoadAsync(id);

        c.Navigator.Received(1).NavigateTo("/dashboard", replace: true);
    }

    [Fact]
    public async Task LoadAsync_LoadsSessionsFromSessionApi()
    {
        var c = CreateSut();
        var arc = MakeArc();
        var campaign = MakeCampaign(id: arc.CampaignId);
        SetupHappyPath(c, arc, campaign);

        var sessionInArc = new SessionTreeDto { Id = Guid.NewGuid(), ArcId = arc.Id, Name = "Session 1" };
        c.SessionApi.GetSessionsByArcAsync(arc.Id).Returns(new List<SessionTreeDto> { sessionInArc });

        await c.Vm.LoadAsync(arc.Id);

        Assert.Single(c.Vm.Sessions);
        Assert.Equal(sessionInArc.Id, c.Vm.Sessions[0].Id);
    }

    [Fact]
    public async Task LoadAsync_SetsPageTitleAndHighlightsTree()
    {
        var c = CreateSut();
        var arc = MakeArc();
        var campaign = MakeCampaign(id: arc.CampaignId);
        SetupHappyPath(c, arc, campaign);

        await c.Vm.LoadAsync(arc.Id);

        await c.TitleService.Received(1).SetTitleAsync(arc.Name);
        c.TreeState.Received(1).ExpandPathToAndSelect(arc.Id);
    }

    [Fact]
    public async Task LoadAsync_WhenUserIsGM_SetsIsCurrentUserGM()
    {
        var c = CreateSut();
        var arc = MakeArc();
        var campaign = MakeCampaign(id: arc.CampaignId);
        var world = new WorldDetailDto { Id = campaign.WorldId };
        SetupHappyPath(c, arc, campaign, world);

        var user = new UserInfo { Email = "gm@example.com" };
        var userId = Guid.NewGuid();
        world.Members = new List<WorldMemberDto>
        {
            new() { Email = "gm@example.com", UserId = userId, Role = WorldRole.GM }
        };
        c.AuthService.GetCurrentUserAsync().Returns(user);

        await c.Vm.LoadAsync(arc.Id);

        Assert.True(c.Vm.IsCurrentUserGM);
        Assert.Equal(userId, c.Vm.CurrentUserId);
    }

    [Fact]
    public async Task LoadAsync_WhenApiThrows_SetsIsLoadingFalseAndNotifiesError()
    {
        var c = CreateSut();
        var id = Guid.NewGuid();
        c.ArcApi.GetArcAsync(id).Returns(Task.FromException<ArcDto?>(new Exception("boom")));

        await c.Vm.LoadAsync(id);

        Assert.False(c.Vm.IsLoading);
        c.Notifier.Received(1).Error(Arg.Any<string>());
    }

    // -----------------------------------------------------------------------
    // Edit field change tracking
    // -----------------------------------------------------------------------

    [Fact]
    public async Task EditName_WhenChanged_SetsHasUnsavedChanges()
    {
        var c = CreateSut();
        var arc = MakeArc();
        SetupHappyPath(c, arc, MakeCampaign(id: arc.CampaignId));
        await c.Vm.LoadAsync(arc.Id);

        c.Vm.EditName = "New Name";

        Assert.True(c.Vm.HasUnsavedChanges);
    }

    [Fact]
    public async Task EditSortOrder_WhenChanged_SetsHasUnsavedChanges()
    {
        var c = CreateSut();
        var arc = MakeArc();
        SetupHappyPath(c, arc, MakeCampaign(id: arc.CampaignId));
        await c.Vm.LoadAsync(arc.Id);

        c.Vm.EditSortOrder = 99;

        Assert.True(c.Vm.HasUnsavedChanges);
    }

    // -----------------------------------------------------------------------
    // SaveAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task SaveAsync_WhenSucceeds_ClearsUnsavedChangesAndNotifiesSuccess()
    {
        var c = CreateSut();
        var arc = MakeArc();
        SetupHappyPath(c, arc, MakeCampaign(id: arc.CampaignId));
        await c.Vm.LoadAsync(arc.Id);
        c.Vm.EditName = "Updated Arc";

        var updated = MakeArc(arc.Id, arc.CampaignId);
        updated.Name = "Updated Arc";
        c.ArcApi.UpdateArcAsync(arc.Id, Arg.Any<ArcUpdateDto>()).Returns(updated);

        await c.Vm.SaveAsync();

        Assert.False(c.Vm.HasUnsavedChanges);
        Assert.False(c.Vm.IsSaving);
        c.Notifier.Received(1).Success(Arg.Any<string>());
    }

    [Fact]
    public async Task SaveAsync_WhenArcNull_DoesNothing()
    {
        var c = CreateSut();
        await c.Vm.SaveAsync();
        await c.ArcApi.DidNotReceive().UpdateArcAsync(Arg.Any<Guid>(), Arg.Any<ArcUpdateDto>());
    }

    [Fact]
    public async Task SaveAsync_WhenApiThrows_NotifiesError()
    {
        var c = CreateSut();
        var arc = MakeArc();
        SetupHappyPath(c, arc, MakeCampaign(id: arc.CampaignId));
        await c.Vm.LoadAsync(arc.Id);

        c.ArcApi.UpdateArcAsync(arc.Id, Arg.Any<ArcUpdateDto>())
            .Returns(Task.FromException<ArcDto?>(new Exception("save failed")));

        await c.Vm.SaveAsync();

        Assert.False(c.Vm.IsSaving);
        c.Notifier.Received(1).Error(Arg.Any<string>());
    }

    // -----------------------------------------------------------------------
    // OnActiveToggleAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task OnActiveToggle_WhenActivateSucceeds_SetsArcActive()
    {
        var c = CreateSut();
        var arc = MakeArc();
        SetupHappyPath(c, arc, MakeCampaign(id: arc.CampaignId));
        await c.Vm.LoadAsync(arc.Id);

        c.ArcApi.ActivateArcAsync(arc.Id).Returns(true);

        await c.Vm.OnActiveToggleAsync(true);

        Assert.True(c.Vm.Arc!.IsActive);
        c.Notifier.Received(1).Success(Arg.Any<string>());
    }

    [Fact]
    public async Task OnActiveToggle_WhenDeactivate_NotifiesInfo()
    {
        var c = CreateSut();
        var arc = MakeArc();
        SetupHappyPath(c, arc, MakeCampaign(id: arc.CampaignId));
        await c.Vm.LoadAsync(arc.Id);

        await c.Vm.OnActiveToggleAsync(false);

        c.Notifier.Received(1).Info(Arg.Any<string>());
        await c.ArcApi.DidNotReceive().ActivateArcAsync(Arg.Any<Guid>());
    }

    // -----------------------------------------------------------------------
    // DeleteAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task DeleteAsync_WhenConfirmedAndSucceeds_NavigatesToCampaign()
    {
        var c = CreateSut();
        var arc = MakeArc();
        SetupHappyPath(c, arc, MakeCampaign(id: arc.CampaignId));
        await c.Vm.LoadAsync(arc.Id);
        c.Confirmation.ConfirmAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(true);

        await c.Vm.DeleteAsync();

        await c.ArcApi.Received(1).DeleteArcAsync(arc.Id);
        c.Navigator.Received(1).NavigateTo($"/campaign/{arc.CampaignId}");
        c.Notifier.Received(1).Success(Arg.Any<string>());
    }

    [Fact]
    public async Task DeleteAsync_WhenNotConfirmed_DoesNotDelete()
    {
        var c = CreateSut();
        var arc = MakeArc();
        SetupHappyPath(c, arc, MakeCampaign(id: arc.CampaignId));
        await c.Vm.LoadAsync(arc.Id);
        c.Confirmation.ConfirmAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(false);

        await c.Vm.DeleteAsync();

        await c.ArcApi.DidNotReceive().DeleteArcAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task DeleteAsync_WhenSessionsExist_DoesNotDelete()
    {
        var c = CreateSut();
        var arc = MakeArc();
        SetupHappyPath(c, arc, MakeCampaign(id: arc.CampaignId));
        c.SessionApi.GetSessionsByArcAsync(arc.Id).Returns(new List<SessionTreeDto>
        {
            new() { ArcId = arc.Id, Name = "Session 1" }
        });
        await c.Vm.LoadAsync(arc.Id);

        await c.Vm.DeleteAsync();

        await c.ArcApi.DidNotReceive().DeleteArcAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task DeleteAsync_WhenApiThrows_NotifiesError()
    {
        var c = CreateSut();
        var arc = MakeArc();
        SetupHappyPath(c, arc, MakeCampaign(id: arc.CampaignId));
        await c.Vm.LoadAsync(arc.Id);
        c.Confirmation.ConfirmAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(true);
        c.ArcApi.DeleteArcAsync(arc.Id).Returns(Task.FromException<bool>(new Exception("delete failed")));

        await c.Vm.DeleteAsync();

        c.Notifier.Received(1).Error(Arg.Any<string>());
    }

    // -----------------------------------------------------------------------
    // NavigateToSessionAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task NavigateToSessionAsync_WhenBreadcrumbsExist_NavigatesWithPath()
    {
        var c = CreateSut();
        var session = new SessionTreeDto { Id = Guid.NewGuid(), Name = "Session 1" };

        await c.Vm.NavigateToSessionAsync(session);

        c.Navigator.Received(1).NavigateTo($"/session/{session.Id}");
    }

    // -----------------------------------------------------------------------
    // Quest event handlers
    // -----------------------------------------------------------------------

    [Fact]
    public void OnEditQuest_SetsSelectedQuest()
    {
        var c = CreateSut();
        var quest = new QuestDto { Id = Guid.NewGuid(), Title = "Find the ring" };

        c.Vm.OnEditQuest(quest);

        Assert.Equal(quest, c.Vm.SelectedQuest);
    }

    [Fact]
    public void OnQuestUpdated_ReplacesSelectedQuest()
    {
        var c = CreateSut();
        var original = new QuestDto { Id = Guid.NewGuid(), Title = "Find the ring" };
        var updated = new QuestDto { Id = original.Id, Title = "Destroy the ring" };
        c.Vm.OnEditQuest(original);

        c.Vm.OnQuestUpdated(updated);

        Assert.Equal(updated, c.Vm.SelectedQuest);
        Assert.Equal("Destroy the ring", c.Vm.SelectedQuest!.Title);
    }
}
