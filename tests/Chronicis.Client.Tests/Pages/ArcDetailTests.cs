using System.Reflection;
using Bunit;
using Chronicis.Client.Pages;
using Chronicis.Client.Services;
using Chronicis.Client.Tests.Components;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.DTOs.Quests;
using Chronicis.Shared.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Pages;

public class ArcDetailTests : MudBlazorTestContext
{
    [Fact]
    public void ArcDetail_OnNameChanged_SetsUnsavedChanges()
    {
        var rendered = CreateRenderedSut();

        InvokePrivate(rendered.Instance, "OnNameChanged");

        Assert.True(GetPrivateBoolField(rendered.Instance, "_hasUnsavedChanges"));
    }

    [Fact]
    public void ArcDetail_OnDescriptionChanged_SetsUnsavedChanges()
    {
        var rendered = CreateRenderedSut();

        InvokePrivate(rendered.Instance, "OnDescriptionChanged");

        Assert.True(GetPrivateBoolField(rendered.Instance, "_hasUnsavedChanges"));
    }

    [Fact]
    public void ArcDetail_OnSortOrderChanged_SetsUnsavedChanges()
    {
        var rendered = CreateRenderedSut();

        InvokePrivate(rendered.Instance, "OnSortOrderChanged");

        Assert.True(GetPrivateBoolField(rendered.Instance, "_hasUnsavedChanges"));
    }

    [Fact]
    public async Task ArcDetail_SaveArc_WhenArcNull_DoesNotCallUpdate()
    {
        var rendered = CreateRenderedSut();

        await InvokePrivateOnRendererAsync(rendered.Cut, "SaveArc");

        await rendered.ArcApi.DidNotReceive().UpdateArcAsync(Arg.Any<Guid>(), Arg.Any<ArcUpdateDto>());
    }

    [Fact]
    public async Task ArcDetail_SaveArc_WhenAlreadySaving_DoesNotCallUpdate()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_arc", new ArcDto { Id = rendered.ArcId, Name = "Arc" });
        SetPrivateField(rendered.Instance, "_isSaving", true);

        await InvokePrivateOnRendererAsync(rendered.Cut, "SaveArc");

        await rendered.ArcApi.DidNotReceive().UpdateArcAsync(Arg.Any<Guid>(), Arg.Any<ArcUpdateDto>());
    }

    [Fact]
    public async Task ArcDetail_SaveArc_WhenUpdateSucceeds_RefreshesAndClearsUnsaved()
    {
        var rendered = CreateRenderedSut();
        var arc = new ArcDto { Id = rendered.ArcId, Name = "Old", Description = "Old", SortOrder = 1 };

        SetPrivateField(rendered.Instance, "_arc", arc);
        SetPrivateField(rendered.Instance, "_editName", " Updated Arc ");
        SetPrivateField(rendered.Instance, "_editDescription", "   ");
        SetPrivateField(rendered.Instance, "_editSortOrder", 5);
        SetPrivateField(rendered.Instance, "_hasUnsavedChanges", true);

        rendered.ArcApi.UpdateArcAsync(rendered.ArcId, Arg.Any<ArcUpdateDto>())
            .Returns(new ArcDto { Id = rendered.ArcId, Name = "Updated Arc", Description = null, SortOrder = 5 });

        await InvokePrivateOnRendererAsync(rendered.Cut, "SaveArc");

        await rendered.ArcApi.Received(1).UpdateArcAsync(rendered.ArcId,
            Arg.Is<ArcUpdateDto>(d => d.Name == "Updated Arc" && d.Description == null && d.SortOrder == 5));
        await rendered.TreeState.Received(1).RefreshAsync();
        Assert.False(GetPrivateBoolField(rendered.Instance, "_hasUnsavedChanges"));
        Assert.False(GetPrivateBoolField(rendered.Instance, "_isSaving"));
    }

    [Fact]
    public async Task ArcDetail_OnActiveToggle_WhenArcNull_ReturnsEarly()
    {
        var rendered = CreateRenderedSut();

        await InvokePrivateOnRendererAsync(rendered.Cut, "OnActiveToggle", true);

        await rendered.ArcApi.DidNotReceive().ActivateArcAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task ArcDetail_OnActiveToggle_ActivateSuccess_SetsArcActive()
    {
        var rendered = CreateRenderedSut();
        var arc = new ArcDto { Id = rendered.ArcId, Name = "Arc", IsActive = false };
        SetPrivateField(rendered.Instance, "_arc", arc);
        rendered.ArcApi.ActivateArcAsync(rendered.ArcId).Returns(true);

        await InvokePrivateOnRendererAsync(rendered.Cut, "OnActiveToggle", true);

        Assert.True(arc.IsActive);
        Assert.False(GetPrivateBoolField(rendered.Instance, "_isTogglingActive"));
    }

    [Fact]
    public async Task ArcDetail_OnActiveToggle_ActivateFailure_LeavesArcInactive()
    {
        var rendered = CreateRenderedSut();
        var arc = new ArcDto { Id = rendered.ArcId, Name = "Arc", IsActive = false };
        SetPrivateField(rendered.Instance, "_arc", arc);
        rendered.ArcApi.ActivateArcAsync(rendered.ArcId).Returns(false);

        await InvokePrivateOnRendererAsync(rendered.Cut, "OnActiveToggle", true);

        Assert.False(arc.IsActive);
    }

    [Fact]
    public async Task ArcDetail_OnActiveToggle_DeactivatePath_DoesNotCallActivateApi()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_arc", new ArcDto { Id = rendered.ArcId, Name = "Arc", IsActive = true });

        await InvokePrivateOnRendererAsync(rendered.Cut, "OnActiveToggle", false);

        await rendered.ArcApi.DidNotReceive().ActivateArcAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task ArcDetail_DeleteArc_WithExistingSessions_ReturnsEarly()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_arc", new ArcDto { Id = rendered.ArcId, Name = "Arc" });
        SetPrivateField(rendered.Instance, "_sessions", new List<ArticleTreeDto> { new() { Id = Guid.NewGuid() } });

        await InvokePrivateOnRendererAsync(rendered.Cut, "DeleteArc");

        await rendered.ArcApi.DidNotReceive().DeleteArcAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task ArcDetail_DeleteArc_ConfirmFalse_DoesNotDelete()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_arc", new ArcDto { Id = rendered.ArcId, Name = "Arc", CampaignId = Guid.NewGuid() });
        SetPrivateField(rendered.Instance, "_sessions", new List<ArticleTreeDto>());
        rendered.JsRuntime.InvokeAsync<bool>("confirm", Arg.Any<object?[]>())
            .Returns(new ValueTask<bool>(false));

        await InvokePrivateOnRendererAsync(rendered.Cut, "DeleteArc");

        await rendered.ArcApi.DidNotReceive().DeleteArcAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task ArcDetail_DeleteArc_ConfirmTrue_DeletesAndNavigatesToCampaign()
    {
        var rendered = CreateRenderedSut();
        var campaignId = Guid.NewGuid();
        SetPrivateField(rendered.Instance, "_arc", new ArcDto { Id = rendered.ArcId, Name = "Arc", CampaignId = campaignId });
        SetPrivateField(rendered.Instance, "_sessions", new List<ArticleTreeDto>());
        rendered.JsRuntime.InvokeAsync<bool>("confirm", Arg.Any<object?[]>())
            .Returns(new ValueTask<bool>(true));

        await InvokePrivateOnRendererAsync(rendered.Cut, "DeleteArc");

        await rendered.ArcApi.Received(1).DeleteArcAsync(rendered.ArcId);
        await rendered.TreeState.Received(1).RefreshAsync();
        Assert.EndsWith($"/campaign/{campaignId}", rendered.Navigation.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ArcDetail_CreateSession_WhenCreateFails_DoesNotRefreshTree()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_arc", new ArcDto { Id = rendered.ArcId, Name = "Arc" });
        SetPrivateField(rendered.Instance, "_campaign", new CampaignDto { Id = Guid.NewGuid(), WorldId = Guid.NewGuid(), Name = "Campaign" });
        SetPrivateField(rendered.Instance, "_sessions", new List<ArticleTreeDto>());

        rendered.ArticleApi.CreateArticleAsync(Arg.Any<ArticleCreateDto>()).Returns((ArticleDto?)null);

        await InvokePrivateOnRendererAsync(rendered.Cut, "CreateSession");

        await rendered.TreeState.DidNotReceive().RefreshAsync();
    }

    [Fact]
    public async Task ArcDetail_CreateSession_WithBreadcrumbs_NavigatesUsingBreadcrumbService()
    {
        var rendered = CreateRenderedSut();
        var campaignId = Guid.NewGuid();
        var worldId = Guid.NewGuid();
        var created = new ArticleDto
        {
            Id = Guid.NewGuid(),
            Slug = "session-1",
            Breadcrumbs = [new BreadcrumbDto { Slug = "world" }, new BreadcrumbDto { Slug = "session-1" }]
        };

        SetPrivateField(rendered.Instance, "_arc", new ArcDto { Id = rendered.ArcId, Name = "Arc", CampaignId = campaignId });
        SetPrivateField(rendered.Instance, "_campaign", new CampaignDto { Id = campaignId, WorldId = worldId, Name = "Campaign" });
        SetPrivateField(rendered.Instance, "_sessions", new List<ArticleTreeDto>());

        rendered.ArticleApi.CreateArticleAsync(Arg.Any<ArticleCreateDto>()).Returns(created);
        rendered.ArcApi.GetArcAsync(rendered.ArcId).Returns(new ArcDto { Id = rendered.ArcId, Name = "Arc", CampaignId = campaignId });
        rendered.ArticleApi.GetAllArticlesAsync().Returns(new List<ArticleTreeDto>());
        rendered.CampaignApi.GetCampaignAsync(campaignId).Returns(new CampaignDetailDto { Id = campaignId, WorldId = worldId, Name = "Campaign" });
        rendered.WorldApi.GetWorldAsync(worldId).Returns(new WorldDetailDto { Id = worldId, Name = "World", Slug = "world" });
        rendered.BreadcrumbService.BuildArticleUrl(Arg.Any<List<BreadcrumbDto>>()).Returns("/article/world/session-1");

        await InvokePrivateOnRendererAsync(rendered.Cut, "CreateSession");

        await rendered.TreeState.Received(1).RefreshAsync();
        Assert.EndsWith("/article/world/session-1", rendered.Navigation.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ArcDetail_NavigateToSession_WithoutBreadcrumbs_UsesSlugFallback()
    {
        var rendered = CreateRenderedSut();
        var session = new ArticleTreeDto { Id = Guid.NewGuid(), Slug = "fallback-session" };

        rendered.ArticleApi.GetArticleDetailAsync(session.Id).Returns(new ArticleDto { Id = session.Id, Slug = session.Slug });

        await InvokePrivateAsync(rendered.Instance, "NavigateToSession", session);

        Assert.EndsWith("/article/fallback-session", rendered.Navigation.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ArcDetail_LoadArcAsync_WhenArcNull_NavigatesToDashboard()
    {
        var rendered = CreateRenderedSut();
        rendered.ArcApi.GetArcAsync(rendered.ArcId).Returns((ArcDto?)null);

        await InvokePrivateOnRendererAsync(rendered.Cut, "LoadArcAsync");

        Assert.EndsWith("/dashboard", rendered.Navigation.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ArcDetail_LoadArcAsync_WhenCampaignMissing_UsesFallbackBreadcrumbsAndSessionFilter()
    {
        var rendered = CreateRenderedSut();
        var campaignId = Guid.NewGuid();
        rendered.ArcApi.GetArcAsync(rendered.ArcId).Returns(new ArcDto
        {
            Id = rendered.ArcId,
            CampaignId = campaignId,
            Name = "Arc Name",
            SortOrder = 2
        });
        rendered.ArticleApi.GetAllArticlesAsync().Returns(new List<ArticleTreeDto>
        {
            new() { Id = Guid.NewGuid(), ArcId = rendered.ArcId, Type = ArticleType.Session, Title = "Session A" },
            new() { Id = Guid.NewGuid(), ArcId = rendered.ArcId, Type = ArticleType.WikiArticle, Title = "Wiki" },
            new() { Id = Guid.NewGuid(), ArcId = Guid.NewGuid(), Type = ArticleType.Session, Title = "Other Arc Session" }
        });
        rendered.CampaignApi.GetCampaignAsync(campaignId).Returns((CampaignDetailDto?)null);
        rendered.AuthService.GetCurrentUserAsync().Returns((UserInfo?)null);

        await InvokePrivateOnRendererAsync(rendered.Cut, "LoadArcAsync");
        rendered.Cut.Render();

        var breadcrumbs = GetPrivateField<List<BreadcrumbItem>>(rendered.Instance, "_breadcrumbs");
        var sessions = GetPrivateField<List<ArticleTreeDto>>(rendered.Instance, "_sessions");
        Assert.Equal(2, breadcrumbs.Count);
        Assert.Equal("Arc Name", breadcrumbs[1].Text);
        Assert.Single(sessions);
        Assert.Contains("Delete all sessions before deleting this arc.", rendered.Cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ArcDetail_LoadArcAsync_WhenUserIsMember_SetsCurrentUserAndGmFlag()
    {
        var rendered = CreateRenderedSut();
        var campaignId = Guid.NewGuid();
        var worldId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var email = "gm@example.com";
        rendered.ArcApi.GetArcAsync(rendered.ArcId).Returns(new ArcDto
        {
            Id = rendered.ArcId,
            CampaignId = campaignId,
            Name = "Arc"
        });
        rendered.ArticleApi.GetAllArticlesAsync().Returns(new List<ArticleTreeDto>());
        rendered.CampaignApi.GetCampaignAsync(campaignId).Returns(new CampaignDetailDto
        {
            Id = campaignId,
            WorldId = worldId,
            Name = "Campaign"
        });
        rendered.WorldApi.GetWorldAsync(worldId).Returns(new WorldDetailDto
        {
            Id = worldId,
            Name = "World",
            Slug = "world",
            Members = new List<WorldMemberDto>
            {
                new() { UserId = userId, Email = email, Role = WorldRole.GM }
            }
        });
        rendered.BreadcrumbService.ForArc(Arg.Any<ArcDto>(), Arg.Any<CampaignDto>(), Arg.Any<WorldDto>())
            .Returns(new List<BreadcrumbItem> { new("Dashboard", "/dashboard"), new("Arc", null, true) });
        rendered.AuthService.GetCurrentUserAsync().Returns(new UserInfo { Email = email });

        await InvokePrivateOnRendererAsync(rendered.Cut, "LoadArcAsync");

        Assert.True(GetPrivateField<bool>(rendered.Instance, "_isCurrentUserGM"));
        Assert.Equal(userId, GetPrivateField<Guid>(rendered.Instance, "_currentUserId"));
    }

    [Fact]
    public async Task ArcDetail_SaveArc_WhenUpdateReturnsNull_KeepsUnsavedChanges()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_arc", new ArcDto { Id = rendered.ArcId, Name = "Arc" });
        SetPrivateField(rendered.Instance, "_editName", "Changed");
        SetPrivateField(rendered.Instance, "_hasUnsavedChanges", true);
        rendered.ArcApi.UpdateArcAsync(rendered.ArcId, Arg.Any<ArcUpdateDto>()).Returns((ArcDto?)null);

        await InvokePrivateOnRendererAsync(rendered.Cut, "SaveArc");

        Assert.True(GetPrivateBoolField(rendered.Instance, "_hasUnsavedChanges"));
        await rendered.TreeState.DidNotReceive().RefreshAsync();
    }

    [Fact]
    public async Task ArcDetail_SaveArc_WhenUpdateThrows_ResetsSavingFlag()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_arc", new ArcDto { Id = rendered.ArcId, Name = "Arc" });
        SetPrivateField(rendered.Instance, "_editName", "Changed");
        rendered.ArcApi.UpdateArcAsync(rendered.ArcId, Arg.Any<ArcUpdateDto>())
            .Returns(Task.FromException<ArcDto?>(new Exception("boom")));

        await InvokePrivateOnRendererAsync(rendered.Cut, "SaveArc");

        Assert.False(GetPrivateBoolField(rendered.Instance, "_isSaving"));
    }

    [Fact]
    public async Task ArcDetail_OnActiveToggle_WhenAlreadyToggling_ReturnsEarly()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_arc", new ArcDto { Id = rendered.ArcId, Name = "Arc" });
        SetPrivateField(rendered.Instance, "_isTogglingActive", true);

        await InvokePrivateOnRendererAsync(rendered.Cut, "OnActiveToggle", true);

        await rendered.ArcApi.DidNotReceive().ActivateArcAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task ArcDetail_OnActiveToggle_WhenActivateThrows_ResetsTogglingFlag()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_arc", new ArcDto { Id = rendered.ArcId, Name = "Arc" });
        rendered.ArcApi.ActivateArcAsync(rendered.ArcId).Returns(Task.FromException<bool>(new Exception("boom")));

        await InvokePrivateOnRendererAsync(rendered.Cut, "OnActiveToggle", true);

        Assert.False(GetPrivateBoolField(rendered.Instance, "_isTogglingActive"));
    }

    [Fact]
    public async Task ArcDetail_DeleteArc_WhenDeleteThrows_DoesNotThrow()
    {
        var rendered = CreateRenderedSut();
        var campaignId = Guid.NewGuid();
        SetPrivateField(rendered.Instance, "_arc", new ArcDto { Id = rendered.ArcId, Name = "Arc", CampaignId = campaignId });
        SetPrivateField(rendered.Instance, "_sessions", new List<ArticleTreeDto>());
        rendered.JsRuntime.InvokeAsync<bool>("confirm", Arg.Any<object?[]>()).Returns(new ValueTask<bool>(true));
        rendered.ArcApi.DeleteArcAsync(rendered.ArcId).Returns(Task.FromException<bool>(new Exception("boom")));

        var ex = await Record.ExceptionAsync(() => InvokePrivateOnRendererAsync(rendered.Cut, "DeleteArc"));

        Assert.Null(ex);
    }

    [Fact]
    public async Task ArcDetail_CreateSession_WhenArcOrCampaignMissing_ReturnsEarly()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Instance, "_arc", null);
        SetPrivateField(rendered.Instance, "_campaign", null);

        await InvokePrivateOnRendererAsync(rendered.Cut, "CreateSession");

        await rendered.ArticleApi.DidNotReceive().CreateArticleAsync(Arg.Any<ArticleCreateDto>());
    }

    [Fact]
    public async Task ArcDetail_CreateSession_WithoutBreadcrumbs_UsesSlugFallbackNavigation()
    {
        var rendered = CreateRenderedSut();
        var campaignId = Guid.NewGuid();
        var worldId = Guid.NewGuid();
        var created = new ArticleDto
        {
            Id = Guid.NewGuid(),
            Slug = "session-fallback",
            Breadcrumbs = new List<BreadcrumbDto>()
        };

        SetPrivateField(rendered.Instance, "_arc", new ArcDto { Id = rendered.ArcId, Name = "Arc", CampaignId = campaignId });
        SetPrivateField(rendered.Instance, "_campaign", new CampaignDto { Id = campaignId, WorldId = worldId, Name = "Campaign" });
        SetPrivateField(rendered.Instance, "_sessions", new List<ArticleTreeDto>());
        rendered.ArticleApi.CreateArticleAsync(Arg.Any<ArticleCreateDto>()).Returns(created);
        rendered.ArcApi.GetArcAsync(rendered.ArcId).Returns(new ArcDto { Id = rendered.ArcId, Name = "Arc", CampaignId = campaignId });
        rendered.ArticleApi.GetAllArticlesAsync().Returns(new List<ArticleTreeDto>());
        rendered.CampaignApi.GetCampaignAsync(campaignId).Returns(new CampaignDetailDto { Id = campaignId, WorldId = worldId, Name = "Campaign" });
        rendered.WorldApi.GetWorldAsync(worldId).Returns(new WorldDetailDto { Id = worldId, Name = "World", Slug = "world" });

        await InvokePrivateOnRendererAsync(rendered.Cut, "CreateSession");

        Assert.EndsWith("/article/session-fallback", rendered.Navigation.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ArcDetail_NavigateToSession_WhenArticleNull_UsesSlugFallback()
    {
        var rendered = CreateRenderedSut();
        var session = new ArticleTreeDto { Id = Guid.NewGuid(), Slug = "slug-fallback" };
        rendered.ArticleApi.GetArticleDetailAsync(session.Id).Returns((ArticleDto?)null);

        await InvokePrivateAsync(rendered.Instance, "NavigateToSession", session);

        Assert.EndsWith("/article/slug-fallback", rendered.Navigation.Uri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ArcDetail_OnQuestUpdated_SetsSelectedQuest()
    {
        var rendered = CreateRenderedSut();
        var quest = new QuestDto { Id = Guid.NewGuid(), Title = "Quest" };

        await InvokePrivateOnRendererAsync(rendered.Cut, "OnQuestUpdated", quest);

        Assert.Equal(quest.Id, GetPrivateField<QuestDto>(rendered.Instance, "_selectedQuest").Id);
    }

    private RenderedContext CreateRenderedSut()
    {
        var arcApi = Substitute.For<IArcApiService>();
        var campaignApi = Substitute.For<ICampaignApiService>();
        var worldApi = Substitute.For<IWorldApiService>();
        var articleApi = Substitute.For<IArticleApiService>();
        var questApi = Substitute.For<IQuestApiService>();
        var authService = Substitute.For<IAuthService>();
        var treeState = Substitute.For<ITreeStateService>();
        var breadcrumbService = Substitute.For<IBreadcrumbService>();
        var snackbar = Substitute.For<ISnackbar>();
        var dialogService = Substitute.For<IDialogService>();
        var jsRuntime = Substitute.For<IJSRuntime>();
        var summaryApi = Substitute.For<IAISummaryApiService>();

        Services.AddSingleton(arcApi);
        Services.AddSingleton(campaignApi);
        Services.AddSingleton(worldApi);
        Services.AddSingleton(articleApi);
        Services.AddSingleton(questApi);
        Services.AddSingleton(authService);
        Services.AddSingleton(treeState);
        Services.AddSingleton(breadcrumbService);
        Services.AddSingleton(snackbar);
        Services.AddSingleton(dialogService);
        Services.AddSingleton(jsRuntime);
        Services.AddSingleton(summaryApi);

        summaryApi.GetTemplatesAsync().Returns(new List<SummaryTemplateDto>());
        summaryApi.GetEntitySummaryAsync(Arg.Any<string>(), Arg.Any<Guid>()).Returns((EntitySummaryDto?)null);
        questApi.GetArcQuestsAsync(Arg.Any<Guid>()).Returns(new List<QuestDto>());

        var arcId = Guid.NewGuid();
        arcApi.GetArcAsync(arcId).Returns((ArcDto?)null);
        authService.GetCurrentUserAsync().Returns((UserInfo?)null);

        var cut = RenderComponent<ArcDetail>(parameters => parameters.Add(p => p.ArcId, arcId));
        var navigation = Services.GetRequiredService<NavigationManager>();

        return new RenderedContext(cut, cut.Instance, arcApi, campaignApi, worldApi, articleApi, authService, breadcrumbService, treeState, jsRuntime, navigation, arcId);
    }

    private static void InvokePrivate(object instance, string methodName, params object[] args)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        method!.Invoke(instance, args);
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

    private static Task InvokePrivateOnRendererAsync(IRenderedComponent<ArcDetail> cut, string methodName, params object[] args)
    {
        return cut.InvokeAsync(async () =>
        {
            var method = cut.Instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(method);
            var result = method!.Invoke(cut.Instance, args);
            if (result is Task task)
            {
                await task;
            }
        });
    }

    private static bool GetPrivateBoolField(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        return (bool)field!.GetValue(instance)!;
    }

    private static T GetPrivateField<T>(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        return (T)field!.GetValue(instance)!;
    }

    private static void SetPrivateField(object instance, string fieldName, object? value)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        field!.SetValue(instance, value);
    }

    private sealed record RenderedContext(
        IRenderedComponent<ArcDetail> Cut,
        ArcDetail Instance,
        IArcApiService ArcApi,
        ICampaignApiService CampaignApi,
        IWorldApiService WorldApi,
        IArticleApiService ArticleApi,
        IAuthService AuthService,
        IBreadcrumbService BreadcrumbService,
        ITreeStateService TreeState,
        IJSRuntime JsRuntime,
        NavigationManager Navigation,
        Guid ArcId);
}
