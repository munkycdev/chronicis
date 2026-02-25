using System.Reflection;
using Chronicis.Client.Abstractions;
using Chronicis.Client.Services;
using Chronicis.Client.ViewModels;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.DTOs.Sessions;
using Chronicis.Shared.Enums;
using Microsoft.Extensions.Logging;
using MudBlazor;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Chronicis.Client.Tests.ViewModels;

public class SessionDetailViewModelTests
{
    private sealed record Sut(
        SessionDetailViewModel Vm,
        ISessionApiService SessionApi,
        IArticleApiService ArticleApi,
        IArcApiService ArcApi,
        ICampaignApiService CampaignApi,
        IWorldApiService WorldApi,
        IAuthService AuthService,
        ITreeStateService TreeState,
        IBreadcrumbService BreadcrumbService,
        IAppNavigator Navigator,
        IUserNotifier Notifier,
        IPageTitleService TitleService);

    private static Sut CreateSut()
    {
        var sessionApi = Substitute.For<ISessionApiService>();
        var articleApi = Substitute.For<IArticleApiService>();
        var arcApi = Substitute.For<IArcApiService>();
        var campaignApi = Substitute.For<ICampaignApiService>();
        var worldApi = Substitute.For<IWorldApiService>();
        var authService = Substitute.For<IAuthService>();
        var treeState = Substitute.For<ITreeStateService>();
        var breadcrumbService = Substitute.For<IBreadcrumbService>();
        var navigator = Substitute.For<IAppNavigator>();
        var notifier = Substitute.For<IUserNotifier>();
        var titleService = Substitute.For<IPageTitleService>();
        var logger = Substitute.For<ILogger<SessionDetailViewModel>>();

        var vm = new SessionDetailViewModel(
            sessionApi,
            articleApi,
            arcApi,
            campaignApi,
            worldApi,
            authService,
            treeState,
            breadcrumbService,
            navigator,
            notifier,
            titleService,
            logger);

        return new Sut(vm, sessionApi, articleApi, arcApi, campaignApi, worldApi, authService, treeState, breadcrumbService, navigator, notifier, titleService);
    }

    private static SessionDto MakeSession(Guid? id = null, Guid? arcId = null, string name = "Session One")
        => new()
        {
            Id = id ?? Guid.NewGuid(),
            ArcId = arcId ?? Guid.NewGuid(),
            Name = name,
            SessionDate = new DateTime(2025, 1, 10),
            PublicNotes = "<p>public</p>",
            PrivateNotes = "<p>private</p>"
        };

    private static ArcDto MakeArc(Guid campaignId) => new()
    {
        Id = Guid.NewGuid(),
        CampaignId = campaignId,
        Name = "Arc A"
    };

    private static CampaignDetailDto MakeCampaign(Guid worldId) => new()
    {
        Id = Guid.NewGuid(),
        WorldId = worldId,
        Name = "Campaign A"
    };

    private static WorldDetailDto MakeWorld(Guid userId, WorldRole role = WorldRole.GM, string email = "gm@example.com") => new()
    {
        Id = Guid.NewGuid(),
        Name = "World A",
        Members = new List<WorldMemberDto>
        {
            new()
            {
                UserId = userId,
                Email = email,
                Role = role
            }
        }
    };

    private static UserInfo MakeUser(string email = "gm@example.com", string displayName = "GM") => new()
    {
        Email = email,
        DisplayName = displayName,
        Auth0UserId = "auth0|test"
    };

    [Fact]
    public async Task LoadAsync_WhenSessionNotFound_NavigatesDashboard_AndClearsLoading()
    {
        var c = CreateSut();
        c.SessionApi.GetSessionAsync(Arg.Any<Guid>()).Returns((SessionDto?)null);

        await c.Vm.LoadAsync(Guid.NewGuid());

        c.Navigator.Received(1).NavigateTo("/dashboard", replace: true);
        Assert.False(c.Vm.IsLoading);
    }

    [Fact]
    public async Task LoadAsync_WhenSuccessful_LoadsGraphAndSetsGmState()
    {
        var c = CreateSut();
        var userId = Guid.NewGuid();
        var session = MakeSession();
        var campaign = MakeCampaign(Guid.NewGuid());
        var arc = MakeArc(campaign.Id);
        arc.Id = session.ArcId;
        var world = MakeWorld(userId, WorldRole.GM);
        world.Id = campaign.WorldId;
        var breadcrumbs = new List<BreadcrumbItem> { new("Dashboard", "/dashboard"), new("Arc", "/arc"), new(session.Name, null, true) };
        var notes = new List<ArticleTreeDto>
        {
            new() { Id = Guid.NewGuid(), SessionId = session.Id, Type = ArticleType.SessionNote, Title = "b", CreatedAt = DateTime.UtcNow.AddMinutes(-1) },
            new() { Id = Guid.NewGuid(), SessionId = session.Id, Type = ArticleType.SessionNote, Title = "a", CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), SessionId = Guid.NewGuid(), Type = ArticleType.SessionNote, Title = "other" },
            new() { Id = Guid.NewGuid(), SessionId = session.Id, Type = ArticleType.WikiArticle, Title = "not session note" }
        };

        c.SessionApi.GetSessionAsync(session.Id).Returns(session);
        c.ArcApi.GetArcAsync(session.ArcId).Returns(arc);
        c.CampaignApi.GetCampaignAsync(arc.CampaignId).Returns(campaign);
        c.WorldApi.GetWorldAsync(campaign.WorldId).Returns(world);
        c.AuthService.GetCurrentUserAsync().Returns(MakeUser(email: world.Members[0].Email, displayName: "Dungeon Master"));
        c.ArticleApi.GetAllArticlesAsync().Returns(notes);
        c.BreadcrumbService.ForArc(arc, campaign, world, currentDisabled: false).Returns(new List<BreadcrumbItem> { new("Dashboard", "/dashboard") });

        await c.Vm.LoadAsync(session.Id);

        Assert.False(c.Vm.IsLoading);
        Assert.Equal(session.Id, c.Vm.Session!.Id);
        Assert.Equal(session.Name, c.Vm.EditName);
        Assert.Equal(session.SessionDate?.Date, c.Vm.EditSessionDate);
        Assert.Equal(session.PublicNotes, c.Vm.EditPublicNotes);
        Assert.Equal(session.PrivateNotes, c.Vm.EditPrivateNotes);
        Assert.True(c.Vm.IsCurrentUserGM);
        Assert.Equal(userId, c.Vm.CurrentUserId);
        Assert.Equal(2, c.Vm.SessionNotes.Count);
        Assert.Equal("a", c.Vm.SessionNotes[0].Title); // sorted by title
        Assert.False(c.Vm.HasUnsavedChanges);
        Assert.True(c.Vm.Breadcrumbs.Any());
        await c.TitleService.Received(1).SetTitleAsync(session.Name);
        c.TreeState.Received(1).ExpandPathToAndSelect(session.Id);
    }

    [Fact]
    public async Task LoadAsync_WhenApiThrows_ShowsError()
    {
        var c = CreateSut();
        c.SessionApi.GetSessionAsync(Arg.Any<Guid>()).ThrowsAsync(new Exception("boom"));

        await c.Vm.LoadAsync(Guid.NewGuid());

        Assert.False(c.Vm.IsLoading);
        c.Notifier.Received(1).Error(Arg.Is<string>(s => s.Contains("Failed to load session", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task LoadAsync_WhenNoWorldOrUser_DoesNotSetGm()
    {
        var c = CreateSut();
        var session = MakeSession();
        c.SessionApi.GetSessionAsync(session.Id).Returns(session);
        c.ArcApi.GetArcAsync(session.ArcId).Returns((ArcDto?)null);
        c.ArticleApi.GetAllArticlesAsync().Returns(new List<ArticleTreeDto>());

        await c.Vm.LoadAsync(session.Id);

        Assert.False(c.Vm.IsCurrentUserGM);
        Assert.Equal(Guid.Empty, c.Vm.CurrentUserId);
    }

    [Fact]
    public async Task EditProperties_WhenGmAndSessionLoaded_UpdateDirtyState()
    {
        var c = CreateSut();
        await LoadMinimalAsGmAsync(c);

        c.Vm.EditName = "Changed";
        Assert.True(c.Vm.HasUnsavedChanges);

        c.Vm.EditName = c.Vm.Session!.Name;
        c.Vm.EditSessionDate = c.Vm.Session.SessionDate?.Date;
        c.Vm.EditPublicNotes = c.Vm.Session.PublicNotes!;
        c.Vm.EditPrivateNotes = c.Vm.Session.PrivateNotes!;
        Assert.False(c.Vm.HasUnsavedChanges);
    }

    [Fact]
    public void EditName_Setter_Covers_NoSession_And_NonGm_Guards()
    {
        var c = CreateSut();

        c.Vm.EditName = "No Session Yet";
        Assert.False(c.Vm.HasUnsavedChanges);

        var sessionField = typeof(SessionDetailViewModel).GetField("_session", BindingFlags.Instance | BindingFlags.NonPublic);
        var isGmField = typeof(SessionDetailViewModel).GetField("_isCurrentUserGm", BindingFlags.Instance | BindingFlags.NonPublic);
        var hasUnsavedField = typeof(SessionDetailViewModel).GetField("_hasUnsavedChanges", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(sessionField);
        Assert.NotNull(isGmField);
        Assert.NotNull(hasUnsavedField);

        sessionField!.SetValue(c.Vm, MakeSession(name: "Original"));
        isGmField!.SetValue(c.Vm, false);
        hasUnsavedField!.SetValue(c.Vm, true);

        c.Vm.EditName = "Changed While Non-Gm";
        Assert.True(c.Vm.HasUnsavedChanges); // setter should not recompute dirty state when not GM

        c.Vm.EditName = "Changed While Non-Gm"; // SetField false branch
        Assert.True(c.Vm.HasUnsavedChanges);
    }

    [Fact]
    public async Task SaveNotesAsync_Guards_ValidateAndReturn()
    {
        var c = CreateSut();

        await c.Vm.SaveNotesAsync();
        await c.SessionApi.DidNotReceive().UpdateSessionNotesAsync(Arg.Any<Guid>(), Arg.Any<SessionUpdateDto>());

        await LoadMinimalAsGmAsync(c);
        c.Vm.EditName = " ";
        await c.Vm.SaveNotesAsync();
        c.Notifier.Received().Error("Session title is required");

        c.Vm.EditName = new string('x', 501);
        await c.Vm.SaveNotesAsync();
        c.Notifier.Received().Error("Session title must be 500 characters or fewer");
    }

    [Fact]
    public async Task SaveNotesAsync_WhenApiReturnsNull_ShowsError()
    {
        var c = CreateSut();
        var session = await LoadMinimalAsGmAsync(c);
        c.SessionApi.UpdateSessionNotesAsync(session.Id, Arg.Any<SessionUpdateDto>()).Returns((SessionDto?)null);

        await c.Vm.SaveNotesAsync();

        c.Notifier.Received(1).Error("Failed to save session notes");
        Assert.False(c.Vm.IsSavingNotes);
    }

    [Fact]
    public async Task SaveNotesAsync_Success_MapsFieldsAndClearsDirty()
    {
        var c = CreateSut();
        var session = await LoadMinimalAsGmAsync(c);
        c.Vm.EditName = " Updated ";
        c.Vm.EditSessionDate = null;
        c.Vm.EditPublicNotes = " ";
        c.Vm.EditPrivateNotes = "<p>x</p>";

        SessionUpdateDto? sent = null;
        c.SessionApi.UpdateSessionNotesAsync(session.Id, Arg.Do<SessionUpdateDto>(x => sent = x))
            .Returns(new SessionDto
            {
                Id = session.Id,
                ArcId = session.ArcId,
                Name = "Updated",
                SessionDate = null,
                PublicNotes = null,
                PrivateNotes = "<p>x</p>"
            });

        await c.Vm.SaveNotesAsync();

        Assert.NotNull(sent);
        Assert.Equal("Updated", sent!.Name);
        Assert.True(sent.ClearSessionDate);
        Assert.Null(sent.PublicNotes);
        Assert.Equal("<p>x</p>", sent.PrivateNotes);
        Assert.Equal("Updated", c.Vm.EditName);
        Assert.False(c.Vm.HasUnsavedChanges);
        Assert.False(c.Vm.IsSavingNotes);
        await c.TreeState.Received(1).RefreshAsync();
        c.Notifier.Received(1).Success("Session saved");
    }

    [Fact]
    public async Task SaveNotesAsync_WhenApiThrows_ShowsError_AndClearsSaving()
    {
        var c = CreateSut();
        var session = await LoadMinimalAsGmAsync(c);
        c.SessionApi.UpdateSessionNotesAsync(session.Id, Arg.Any<SessionUpdateDto>()).ThrowsAsync(new Exception("db"));

        await c.Vm.SaveNotesAsync();

        Assert.False(c.Vm.IsSavingNotes);
        c.Notifier.Received(1).Error(Arg.Is<string>(s => s.Contains("Failed to save session", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task GenerateAiSummaryAsync_Covers_Guards_Failure_Success_Refresh_AndException()
    {
        var c = CreateSut();
        await c.Vm.GenerateAiSummaryAsync(); // guard: no session

        var session = await LoadMinimalAsGmAsync(c);

        c.SessionApi.GenerateAiSummaryAsync(session.Id).Returns((SummaryGenerationDto?)null);
        await c.Vm.GenerateAiSummaryAsync();
        c.Notifier.Received().Error("Failed to generate AI summary");

        c.SessionApi.GenerateAiSummaryAsync(session.Id).Returns(new SummaryGenerationDto { Success = false, ErrorMessage = "bad" });
        await c.Vm.GenerateAiSummaryAsync();
        c.Notifier.Received().Error("bad");

        c.SessionApi.GenerateAiSummaryAsync(session.Id).Returns(new SummaryGenerationDto
        {
            Success = true,
            Summary = "hello",
            GeneratedDate = DateTime.UtcNow
        });
        session.AiSummary = null;
        await c.Vm.GenerateAiSummaryAsync();
        c.Notifier.Received().Success("AI summary generated");

        c.SessionApi.GenerateAiSummaryAsync(session.Id).Returns(new SummaryGenerationDto
        {
            Success = true,
            Summary = "updated",
            GeneratedDate = DateTime.UtcNow
        });
        session.AiSummary = "existing";
        await c.Vm.GenerateAiSummaryAsync();
        c.Notifier.Received().Success("AI summary refreshed");

        c.SessionApi.GenerateAiSummaryAsync(session.Id).ThrowsAsync(new Exception("ai"));
        await c.Vm.GenerateAiSummaryAsync();
        c.Notifier.Received().Error(Arg.Is<string>(s => s.Contains("Failed to generate summary", StringComparison.Ordinal)));
        Assert.False(c.Vm.IsGeneratingSummary);
    }

    [Fact]
    public async Task ClearAiSummaryAsync_Covers_Guards_Failure_Success_AndException()
    {
        var c = CreateSut();
        await c.Vm.ClearAiSummaryAsync(); // guard: no session

        var session = await LoadMinimalAsGmAsync(c);
        session.AiSummary = null;
        await c.Vm.ClearAiSummaryAsync(); // guard: no summary

        session.AiSummary = "exists";
        c.SessionApi.ClearAiSummaryAsync(session.Id).Returns(false);
        await c.Vm.ClearAiSummaryAsync();
        c.Notifier.Received().Error("Failed to delete AI summary");

        session.AiSummary = "exists";
        session.AiSummaryGeneratedAt = DateTime.UtcNow;
        session.AiSummaryGeneratedByUserId = Guid.NewGuid();
        c.SessionApi.ClearAiSummaryAsync(session.Id).Returns(true);
        await c.Vm.ClearAiSummaryAsync();
        Assert.Null(session.AiSummary);
        Assert.Null(session.AiSummaryGeneratedAt);
        Assert.Null(session.AiSummaryGeneratedByUserId);
        c.Notifier.Received().Success("AI summary deleted");

        session.AiSummary = "exists";
        c.SessionApi.ClearAiSummaryAsync(session.Id).ThrowsAsync(new Exception("fail"));
        await c.Vm.ClearAiSummaryAsync();
        c.Notifier.Received().Error(Arg.Is<string>(s => s.Contains("Failed to delete AI summary", StringComparison.Ordinal)));
        Assert.False(c.Vm.IsDeletingSummary);
    }

    [Fact]
    public async Task DeleteSessionAsync_Covers_Guards_Failure_Success_AndException()
    {
        var c = CreateSut();
        await c.Vm.DeleteSessionAsync(); // no session guard

        var session = await LoadMinimalAsGmAsync(c);
        c.SessionApi.DeleteSessionAsync(session.Id).Returns(false);
        await c.Vm.DeleteSessionAsync();
        c.Notifier.Received().Error("Failed to delete session");

        c.SessionApi.DeleteSessionAsync(session.Id).Returns(true);
        await c.Vm.DeleteSessionAsync();
        c.Notifier.Received().Success("Session deleted");
        await c.TreeState.Received().RefreshAsync();
        c.Navigator.Received().NavigateTo(Arg.Is<string>(s => s.StartsWith("/arc/", StringComparison.Ordinal)), replace: true);

        c.SessionApi.DeleteSessionAsync(session.Id).ThrowsAsync(new Exception("fail"));
        await c.Vm.DeleteSessionAsync();
        c.Notifier.Received().Error(Arg.Is<string>(s => s.Contains("Failed to delete session", StringComparison.Ordinal)));
        Assert.False(c.Vm.IsDeleting);
    }

    [Fact]
    public async Task CreateSessionNoteAsync_Covers_Guards_And_NullCreate()
    {
        var c = CreateSut();
        await c.Vm.CreateSessionNoteAsync(); // guard: cannot create

        var session = await LoadMinimalAsGmAsync(c);
        await SetupCreatePrereqsAsync(c, session);

        c.ArticleApi.CreateArticleAsync(Arg.Any<ArticleCreateDto>()).Returns((ArticleDto?)null);
        await c.Vm.CreateSessionNoteAsync();

        c.Notifier.Received().Error("Failed to create session note");
        Assert.False(c.Vm.IsCreatingSessionNote);
    }

    [Fact]
    public async Task CreateSessionNoteAsync_WhenAttachFails_DeletesOrphan_AndShowsError()
    {
        var c = CreateSut();
        var session = await LoadMinimalAsGmAsync(c);
        await SetupCreatePrereqsAsync(c, session, displayName: "Test User");
        var created = new ArticleDto { Id = Guid.NewGuid(), Title = "T", Slug = "t" };

        c.ArticleApi.CreateArticleAsync(Arg.Any<ArticleCreateDto>()).Returns(created);
        c.ArticleApi.MoveArticleAsync(created.Id, null, session.Id).Returns(false);
        c.ArticleApi.DeleteArticleAsync(created.Id).Returns(true);

        await c.Vm.CreateSessionNoteAsync();

        await c.ArticleApi.Received(1).DeleteArticleAsync(created.Id);
        c.Notifier.Received().Error("Failed to attach session note to this session");
    }

    [Fact]
    public async Task CreateSessionNoteAsync_WhenAttachFails_AndCleanupFails_StillShowsError()
    {
        var c = CreateSut();
        var session = await LoadMinimalAsGmAsync(c);
        await SetupCreatePrereqsAsync(c, session);
        var created = new ArticleDto { Id = Guid.NewGuid(), Title = "T", Slug = "t" };

        c.ArticleApi.CreateArticleAsync(Arg.Any<ArticleCreateDto>()).Returns(created);
        c.ArticleApi.MoveArticleAsync(created.Id, null, session.Id).Returns(false);
        c.ArticleApi.DeleteArticleAsync(created.Id).Returns(false);

        await c.Vm.CreateSessionNoteAsync();

        c.Notifier.Received().Error("Failed to attach session note to this session");
    }

    [Fact]
    public async Task CreateSessionNoteAsync_Success_RefreshesAndNavigates()
    {
        var c = CreateSut();
        var session = await LoadMinimalAsGmAsync(c);
        await SetupCreatePrereqsAsync(c, session, displayName: "Alyx");
        var created = new ArticleDto { Id = Guid.NewGuid(), Title = "Alyx's Notes", Slug = "alyx-notes" };

        c.ArticleApi.CreateArticleAsync(Arg.Any<ArticleCreateDto>()).Returns(created);
        c.ArticleApi.MoveArticleAsync(created.Id, null, session.Id).Returns(true);
        c.ArticleApi.GetAllArticlesAsync().Returns(new List<ArticleTreeDto>
        {
            new() { Id = created.Id, SessionId = session.Id, Type = ArticleType.SessionNote, Title = created.Title, Slug = created.Slug }
        });
        c.ArticleApi.GetArticleDetailAsync(created.Id).Returns(new ArticleDto
        {
            Id = created.Id,
            Title = created.Title,
            Slug = created.Slug,
            Breadcrumbs = new List<BreadcrumbDto> { new() { Id = Guid.NewGuid(), Title = "W", Slug = "w" } }
        });
        c.BreadcrumbService.BuildArticleUrl(Arg.Any<List<BreadcrumbDto>>()).Returns("/article/w/alyx-notes");

        await c.Vm.CreateSessionNoteAsync();

        c.Notifier.Received().Success("Session note added");
        c.Navigator.Received().NavigateTo("/article/w/alyx-notes");
        Assert.False(c.Vm.IsCreatingSessionNote);
    }

    [Fact]
    public async Task CreateSessionNoteAsync_Exception_CleansUpAndShowsError()
    {
        var c = CreateSut();
        var session = await LoadMinimalAsGmAsync(c);
        await SetupCreatePrereqsAsync(c, session);
        var created = new ArticleDto { Id = Guid.NewGuid(), Title = "T", Slug = "t" };

        c.ArticleApi.CreateArticleAsync(Arg.Any<ArticleCreateDto>()).Returns(created);
        c.ArticleApi.MoveArticleAsync(created.Id, null, session.Id).ThrowsAsync(new Exception("attach fail"));
        c.ArticleApi.DeleteArticleAsync(created.Id).Returns(true);

        await c.Vm.CreateSessionNoteAsync();

        await c.ArticleApi.Received(1).DeleteArticleAsync(created.Id);
        c.Notifier.Received().Error(Arg.Is<string>(s => s.Contains("Failed to create session note", StringComparison.Ordinal)));
        Assert.False(c.Vm.IsCreatingSessionNote);
    }

    [Fact]
    public async Task OpenSessionNoteAsync_UsesBreadcrumbUrl_OrSlugFallback_AndHandlesErrors()
    {
        var c = CreateSut();
        var note = new ArticleTreeDto { Id = Guid.NewGuid(), Slug = "note-a", Title = "Note A" };
        c.ArticleApi.GetArticleDetailAsync(note.Id).Returns(new ArticleDto
        {
            Id = note.Id,
            Slug = note.Slug,
            Breadcrumbs = new List<BreadcrumbDto> { new() { Id = Guid.NewGuid(), Title = "World", Slug = "world" } }
        });
        c.BreadcrumbService.BuildArticleUrl(Arg.Any<List<BreadcrumbDto>>()).Returns("/article/world/note-a");

        await c.Vm.OpenSessionNoteAsync(note);
        c.Navigator.Received().NavigateTo("/article/world/note-a");

        var note2 = new ArticleTreeDto { Id = Guid.NewGuid(), Slug = "note-b", Title = "Note B" };
        c.ArticleApi.GetArticleDetailAsync(note2.Id).Returns((ArticleDto?)null);
        await c.Vm.OpenSessionNoteAsync(note2);
        c.Navigator.Received().NavigateTo("/article/note-b");

        var note3 = new ArticleTreeDto { Id = Guid.NewGuid(), Slug = "note-c", Title = "Note C" };
        c.ArticleApi.GetArticleDetailAsync(note3.Id).ThrowsAsync(new Exception("boom"));
        await c.Vm.OpenSessionNoteAsync(note3);
        c.Notifier.Received().Error("Failed to navigate to session note");
    }

    [Fact]
    public void PrivateHelpers_CanBeExercisedViaReflection()
    {
        var areSameDate = typeof(SessionDetailViewModel).GetMethod("AreSameDate", BindingFlags.Static | BindingFlags.NonPublic);
        var buildDefaultTitle = typeof(SessionDetailViewModel).GetMethod("BuildDefaultSessionNoteTitle", BindingFlags.Static | BindingFlags.NonPublic);

        Assert.NotNull(areSameDate);
        Assert.NotNull(buildDefaultTitle);

        Assert.True((bool)areSameDate!.Invoke(null, new object?[] { new DateTime(2024, 1, 1, 12, 0, 0), new DateTime(2024, 1, 1, 1, 0, 0) })!);
        Assert.False((bool)areSameDate.Invoke(null, new object?[] { new DateTime(2024, 1, 1), new DateTime(2024, 1, 2) })!);

        Assert.Equal("My Notes", (string)buildDefaultTitle!.Invoke(null, new object?[] { null })!);
        var longName = new string('a', 600);
        var title = (string)buildDefaultTitle.Invoke(null, new object?[] { longName })!;
        Assert.True(title.Length <= 500);
    }

    [Fact]
    public void UpdateDirtyState_CoversNullStringComparisons()
    {
        var c = CreateSut();

        var session = MakeSession();
        session.Name = null!;
        session.PublicNotes = null;
        session.PrivateNotes = null;

        var sessionField = typeof(SessionDetailViewModel).GetField("_session", BindingFlags.Instance | BindingFlags.NonPublic);
        var isGmField = typeof(SessionDetailViewModel).GetField("_isCurrentUserGm", BindingFlags.Instance | BindingFlags.NonPublic);
        var editNameField = typeof(SessionDetailViewModel).GetField("_editName", BindingFlags.Instance | BindingFlags.NonPublic);
        var editPublicField = typeof(SessionDetailViewModel).GetField("_editPublicNotes", BindingFlags.Instance | BindingFlags.NonPublic);
        var editPrivateField = typeof(SessionDetailViewModel).GetField("_editPrivateNotes", BindingFlags.Instance | BindingFlags.NonPublic);
        var editDateField = typeof(SessionDetailViewModel).GetField("_editSessionDate", BindingFlags.Instance | BindingFlags.NonPublic);
        var updateDirtyState = typeof(SessionDetailViewModel).GetMethod("UpdateDirtyState", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(sessionField);
        Assert.NotNull(isGmField);
        Assert.NotNull(editNameField);
        Assert.NotNull(editPublicField);
        Assert.NotNull(editPrivateField);
        Assert.NotNull(editDateField);
        Assert.NotNull(updateDirtyState);

        sessionField!.SetValue(c.Vm, session);
        isGmField!.SetValue(c.Vm, true);
        editNameField!.SetValue(c.Vm, null);
        editPublicField!.SetValue(c.Vm, null);
        editPrivateField!.SetValue(c.Vm, null);
        editDateField!.SetValue(c.Vm, session.SessionDate?.Date);

        updateDirtyState!.Invoke(c.Vm, null);

        Assert.False(c.Vm.HasUnsavedChanges);
    }

    [Fact]
    public async Task PrivateHelpers_UpdateDirtyStateGuard_AndBuildBreadcrumbsNullSession_Covered()
    {
        var c = CreateSut();
        await c.Vm.LoadAsync(Guid.NewGuid()); // no setup -> session remains null

        var buildBreadcrumbs = typeof(SessionDetailViewModel).GetMethod("BuildBreadcrumbs", BindingFlags.Instance | BindingFlags.NonPublic);
        var updateDirtyState = typeof(SessionDetailViewModel).GetMethod("UpdateDirtyState", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(buildBreadcrumbs);
        Assert.NotNull(updateDirtyState);

        var breadcrumbs = (List<BreadcrumbItem>)buildBreadcrumbs!.Invoke(c.Vm, null)!;
        Assert.Single(breadcrumbs);
        Assert.Equal("Dashboard", breadcrumbs[0].Text);

        var sessionField = typeof(SessionDetailViewModel).GetField("_session", BindingFlags.Instance | BindingFlags.NonPublic);
        var isGmField = typeof(SessionDetailViewModel).GetField("_isCurrentUserGm", BindingFlags.Instance | BindingFlags.NonPublic);
        var hasUnsavedField = typeof(SessionDetailViewModel).GetField("_hasUnsavedChanges", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(sessionField);
        Assert.NotNull(isGmField);
        Assert.NotNull(hasUnsavedField);

        hasUnsavedField!.SetValue(c.Vm, true);
        updateDirtyState!.Invoke(c.Vm, null); // Session == null branch
        Assert.False(c.Vm.HasUnsavedChanges);

        sessionField!.SetValue(c.Vm, MakeSession());
        isGmField!.SetValue(c.Vm, false);
        hasUnsavedField.SetValue(c.Vm, true);

        updateDirtyState.Invoke(c.Vm, null);

        Assert.False(c.Vm.HasUnsavedChanges);
    }

    private static async Task<SessionDto> LoadMinimalAsGmAsync(Sut c)
    {
        var userId = Guid.NewGuid();
        var session = MakeSession();
        var campaign = MakeCampaign(Guid.NewGuid());
        var arc = new ArcDto { Id = session.ArcId, CampaignId = campaign.Id, Name = "Arc" };
        var world = MakeWorld(userId, WorldRole.GM);
        world.Id = campaign.WorldId;

        c.SessionApi.GetSessionAsync(session.Id).Returns(session);
        c.ArcApi.GetArcAsync(session.ArcId).Returns(arc);
        c.CampaignApi.GetCampaignAsync(arc.CampaignId).Returns(campaign);
        c.WorldApi.GetWorldAsync(campaign.WorldId).Returns(world);
        c.AuthService.GetCurrentUserAsync().Returns(MakeUser(email: world.Members[0].Email));
        c.ArticleApi.GetAllArticlesAsync().Returns(new List<ArticleTreeDto>());
        c.BreadcrumbService.ForArc(Arg.Any<ArcDto>(), Arg.Any<CampaignDto>(), Arg.Any<WorldDto>(), currentDisabled: false)
            .Returns(new List<BreadcrumbItem> { new("Dashboard", "/dashboard") });

        await c.Vm.LoadAsync(session.Id);
        return session;
    }

    private static async Task SetupCreatePrereqsAsync(Sut c, SessionDto session, string displayName = "GM")
    {
        if (c.Vm.World == null || c.Vm.Campaign == null || c.Vm.Arc == null)
        {
            await LoadMinimalAsGmAsync(c);
        }

        c.AuthService.GetCurrentUserAsync().Returns(MakeUser(displayName: displayName));
    }
}
