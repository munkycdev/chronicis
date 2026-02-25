using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Bunit;
using Chronicis.Client.Components.Quests;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.DTOs.Quests;
using Chronicis.Shared.Enums;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using MudBlazor;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Components.Quests;

[ExcludeFromCodeCoverage]
public class QuestDrawerTests : MudBlazorTestContext
{
    [Fact]
    public async Task QuestDrawer_GetCurrentArticleAsync_WhenNoSelectedNode_ReturnsNull()
    {
        var rendered = CreateRenderedSut();
        rendered.TreeState.SelectedNodeId.Returns((Guid?)null);

        var result = await InvokePrivateOnRendererAsync<ArticleDto?>(rendered.Cut, "GetCurrentArticleAsync");

        Assert.Null(result);
    }

    [Fact]
    public async Task QuestDrawer_GetCurrentArticleAsync_WhenApiThrows_ReturnsNull()
    {
        var rendered = CreateRenderedSut();
        var selectedId = Guid.NewGuid();
        rendered.TreeState.SelectedNodeId.Returns(selectedId);
        rendered.ArticleApi.GetArticleDetailAsync(selectedId)
            .Returns(_ => Task.FromException<ArticleDto?>(new InvalidOperationException("boom")));

        var result = await InvokePrivateOnRendererAsync<ArticleDto?>(rendered.Cut, "GetCurrentArticleAsync");

        Assert.Null(result);
    }

    [Fact]
    public async Task QuestDrawer_LoadQuestsAsync_WhenNoArticle_SetsEmptyState()
    {
        var rendered = CreateRenderedSut();
        rendered.TreeState.SelectedNodeId.Returns((Guid?)null);

        await InvokePrivateOnRendererAsync(rendered.Cut, "LoadQuestsAsync");

        Assert.Contains("No article selected", GetPrivateField<string>(rendered.Cut.Instance, "_emptyStateMessage"));
    }

    [Fact]
    public async Task QuestDrawer_LoadQuestsAsync_WhenNotSessionType_SetsEmptyState()
    {
        var rendered = CreateRenderedSut();
        var articleId = Guid.NewGuid();
        rendered.TreeState.SelectedNodeId.Returns(articleId);
        rendered.ArticleApi.GetArticleDetailAsync(articleId).Returns(new ArticleDto
        {
            Id = articleId,
            Type = ArticleType.WikiArticle,
            WorldId = Guid.NewGuid()
        });

        await InvokePrivateOnRendererAsync(rendered.Cut, "LoadQuestsAsync");

        Assert.Contains("Navigate to a session", GetPrivateField<string>(rendered.Cut.Instance, "_emptyStateMessage"));
    }

    [Fact]
    public async Task QuestDrawer_LoadQuestsAsync_WhenSessionNoArc_SetsEmptyState()
    {
        var rendered = CreateRenderedSut();
        var articleId = Guid.NewGuid();
        rendered.TreeState.SelectedNodeId.Returns(articleId);
        rendered.ArticleApi.GetArticleDetailAsync(articleId).Returns(new ArticleDto
        {
            Id = articleId,
            Type = ArticleType.Session,
            WorldId = Guid.NewGuid(),
            ArcId = null
        });

        await InvokePrivateOnRendererAsync(rendered.Cut, "LoadQuestsAsync");

        Assert.Contains("not associated with an arc", GetPrivateField<string>(rendered.Cut.Instance, "_emptyStateMessage"));
    }

    [Fact]
    public async Task QuestDrawer_LoadQuestsAsync_WhenSession_LoadsAndSelectsFirstQuest()
    {
        var rendered = CreateRenderedSut();
        var articleId = Guid.NewGuid();
        var arcId = Guid.NewGuid();
        var quest = CreateQuest("Quest 1", arcId);

        rendered.TreeState.SelectedNodeId.Returns(articleId);
        rendered.ArticleApi.GetArticleDetailAsync(articleId).Returns(new ArticleDto
        {
            Id = articleId,
            Type = ArticleType.Session,
            WorldId = Guid.NewGuid(),
            ArcId = arcId
        });

        rendered.QuestApi.GetArcQuestsAsync(arcId).Returns(new List<QuestDto> { quest });
        rendered.QuestApi.GetQuestUpdatesAsync(quest.Id, 0, 5).Returns(new PagedResult<QuestUpdateEntryDto>
        {
            Items = new List<QuestUpdateEntryDto>(),
            TotalCount = 0
        });

        await InvokePrivateOnRendererAsync(rendered.Cut, "LoadQuestsAsync");

        Assert.Equal(quest.Id, GetPrivateField<Guid?>(rendered.Cut.Instance, "_selectedQuestId"));
    }

    [Fact]
    public async Task QuestDrawer_LoadQuestsAsync_FiltersOutGmOnlyQuests()
    {
        var rendered = CreateRenderedSut();
        var articleId = Guid.NewGuid();
        var arcId = Guid.NewGuid();
        var playerQuest = CreateQuest("Player Quest", arcId);
        var gmQuest = CreateQuest("GM Quest", arcId);
        gmQuest.IsGmOnly = true;

        rendered.TreeState.SelectedNodeId.Returns(articleId);
        rendered.ArticleApi.GetArticleDetailAsync(articleId).Returns(new ArticleDto
        {
            Id = articleId,
            Type = ArticleType.Session,
            WorldId = Guid.NewGuid(),
            ArcId = arcId
        });
        rendered.QuestApi.GetArcQuestsAsync(arcId).Returns(new List<QuestDto> { playerQuest, gmQuest });
        rendered.QuestApi.GetQuestUpdatesAsync(playerQuest.Id, 0, 5).Returns(new PagedResult<QuestUpdateEntryDto>
        {
            Items = new List<QuestUpdateEntryDto>(),
            TotalCount = 0
        });

        await InvokePrivateOnRendererAsync(rendered.Cut, "LoadQuestsAsync");

        var quests = GetPrivateField<List<QuestDto>>(rendered.Cut.Instance, "_quests");
        Assert.NotNull(quests);
        Assert.Single(quests);
        Assert.Equal(playerQuest.Id, quests![0].Id);
        Assert.DoesNotContain(quests, q => q.IsGmOnly);
    }

    [Fact]
    public async Task QuestDrawer_ResolveSessionIdFromParentAsync_FindsSession()
    {
        var rendered = CreateRenderedSut();
        var sessionId = Guid.NewGuid();
        var noteId = Guid.NewGuid();

        rendered.ArticleApi.GetArticleDetailAsync(noteId).Returns(new ArticleDto
        {
            Id = noteId,
            Type = ArticleType.SessionNote,
            ParentId = Guid.NewGuid(),
            SessionId = sessionId
        });

        var result = await InvokePrivateOnRendererAsync<Guid?>(rendered.Cut, "ResolveSessionIdFromParentAsync", noteId);

        Assert.Equal(sessionId, result);
    }

    [Fact]
    public async Task QuestDrawer_ResolveSessionIdFromParentAsync_WhenApiThrows_ReturnsNull()
    {
        var rendered = CreateRenderedSut();
        var noteId = Guid.NewGuid();
        rendered.ArticleApi.GetArticleDetailAsync(noteId)
            .Returns(_ => Task.FromException<ArticleDto?>(new InvalidOperationException("boom")));

        var result = await InvokePrivateOnRendererAsync<Guid?>(rendered.Cut, "ResolveSessionIdFromParentAsync", noteId);

        Assert.Null(result);
    }

    [Fact]
    public async Task QuestDrawer_ResolveSessionIdFromParentAsync_WhenNoParent_ReturnsNull()
    {
        var rendered = CreateRenderedSut();
        var noteId = Guid.NewGuid();
        rendered.ArticleApi.GetArticleDetailAsync(noteId).Returns(new ArticleDto
        {
            Id = noteId,
            Type = ArticleType.SessionNote,
            ParentId = null
        });

        var result = await InvokePrivateOnRendererAsync<Guid?>(rendered.Cut, "ResolveSessionIdFromParentAsync", noteId);

        Assert.Null(result);
    }

    [Fact]
    public async Task QuestDrawer_LoadQuestsAsync_WhenSessionNoteWithoutSession_DisablesAssociation()
    {
        var rendered = CreateRenderedSut();
        var noteId = Guid.NewGuid();
        var arcId = Guid.NewGuid();
        var parentId = Guid.NewGuid();

        rendered.TreeState.SelectedNodeId.Returns(noteId);
        rendered.ArticleApi.GetArticleDetailAsync(noteId).Returns(new ArticleDto
        {
            Id = noteId,
            Type = ArticleType.SessionNote,
            WorldId = Guid.NewGuid(),
            ArcId = arcId,
            ParentId = parentId
        });
        rendered.ArticleApi.GetArticleDetailAsync(parentId).Returns(new ArticleDto
        {
            Id = parentId,
            Type = ArticleType.WikiArticle,
            ParentId = null
        });
        rendered.QuestApi.GetArcQuestsAsync(arcId).Returns(new List<QuestDto>());

        await InvokePrivateOnRendererAsync(rendered.Cut, "LoadQuestsAsync");

        Assert.False(GetPrivateField<bool>(rendered.Cut.Instance, "_canAssociateSession"));
        Assert.Null(GetPrivateField<Guid?>(rendered.Cut.Instance, "_currentSessionId"));
    }

    [Fact]
    public async Task QuestDrawer_SubmitUpdate_WhenEditorMissing_SetsValidationError()
    {
        var rendered = CreateRenderedSut();
        var quest = CreateQuest("Quest", Guid.NewGuid());

        SetPrivateField(rendered.Cut.Instance, "_selectedQuest", quest);

        await InvokePrivateOnRendererAsync(rendered.Cut, "SubmitUpdate");

        Assert.Equal("Editor not initialized", GetPrivateField<string>(rendered.Cut.Instance, "_validationError"));
    }

    [Fact]
    public async Task QuestDrawer_SubmitUpdate_WhenContentEmpty_SetsValidationError()
    {
        var rendered = CreateRenderedSut();
        var quest = CreateQuest("Quest", Guid.NewGuid());
        var module = Substitute.For<IJSObjectReference>();
        module.InvokeAsync<string>("getEditorContent", Arg.Any<object?[]>())
            .Returns(new ValueTask<string>("<p></p>"));

        SetPrivateField(rendered.Cut.Instance, "_selectedQuest", quest);
        SetPrivateField(rendered.Cut.Instance, "_editorModule", module);

        await InvokePrivateOnRendererAsync(rendered.Cut, "SubmitUpdate");

        Assert.Equal("Update content cannot be empty", GetPrivateField<string>(rendered.Cut.Instance, "_validationError"));
    }

    [Fact]
    public async Task QuestDrawer_SubmitUpdate_WhenApiReturnsNull_SetsValidationError()
    {
        var rendered = CreateRenderedSut();
        var quest = CreateQuest("Quest", Guid.NewGuid());
        var module = Substitute.For<IJSObjectReference>();
        module.InvokeAsync<string>("getEditorContent", Arg.Any<object?[]>())
            .Returns(new ValueTask<string>("<p>update</p>"));

        rendered.QuestApi.AddQuestUpdateAsync(quest.Id, Arg.Any<QuestUpdateCreateDto>()).Returns((QuestUpdateEntryDto?)null);

        SetPrivateField(rendered.Cut.Instance, "_selectedQuest", quest);
        SetPrivateField(rendered.Cut.Instance, "_editorModule", module);

        await InvokePrivateOnRendererAsync(rendered.Cut, "SubmitUpdate");

        Assert.Equal("Failed to add quest update", GetPrivateField<string>(rendered.Cut.Instance, "_validationError"));
    }

    [Fact]
    public async Task QuestDrawer_LoadRecentUpdatesAsync_WhenApiThrows_SetsEmptyList()
    {
        var rendered = CreateRenderedSut();
        var questId = Guid.NewGuid();
        rendered.QuestApi.GetQuestUpdatesAsync(questId, 0, 5)
            .Returns(_ => Task.FromException<PagedResult<QuestUpdateEntryDto>>(new InvalidOperationException("boom")));

        await InvokePrivateOnRendererAsync(rendered.Cut, "LoadRecentUpdatesAsync", questId);

        var updates = GetPrivateField<List<QuestUpdateEntryDto>>(rendered.Cut.Instance, "_recentUpdates");
        Assert.Empty(updates);
    }

    [Fact]
    public async Task QuestDrawer_SelectQuest_WhenSameQuestId_DoesNothing()
    {
        var rendered = CreateRenderedSut();
        var questId = Guid.NewGuid();
        SetPrivateField(rendered.Cut.Instance, "_selectedQuestId", questId);

        await InvokePrivateOnRendererAsync(rendered.Cut, "SelectQuest", questId);

        await rendered.QuestApi.DidNotReceive().GetQuestUpdatesAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>());
    }

    [Fact]
    public async Task QuestDrawer_SelectQuest_WhenEditorInitialized_DisposesOldEditor()
    {
        var rendered = CreateRenderedSut();
        var quest = CreateQuest("Quest", Guid.NewGuid());
        var oldModule = Substitute.For<IJSObjectReference>();
        rendered.QuestApi.GetQuestUpdatesAsync(quest.Id, 0, 5).Returns(new PagedResult<QuestUpdateEntryDto>
        {
            Items = new List<QuestUpdateEntryDto>(),
            TotalCount = 0
        });

        SetPrivateField(rendered.Cut.Instance, "_quests", new List<QuestDto> { quest });
        SetPrivateField(rendered.Cut.Instance, "_editorInitialized", true);
        SetPrivateField(rendered.Cut.Instance, "_editorModule", oldModule);

        await InvokePrivateOnRendererAsync(rendered.Cut, "SelectQuest", quest.Id);

        await oldModule.Received().InvokeVoidAsync("destroyEditor", Arg.Any<object?[]>());
    }

    [Fact]
    public async Task QuestDrawer_LoadQuestsAsync_WhenQuestApiThrows_SetsLoadingError()
    {
        var rendered = CreateRenderedSut();
        var articleId = Guid.NewGuid();
        var arcId = Guid.NewGuid();

        rendered.TreeState.SelectedNodeId.Returns(articleId);
        rendered.ArticleApi.GetArticleDetailAsync(articleId).Returns(new ArticleDto
        {
            Id = articleId,
            Type = ArticleType.Session,
            ArcId = arcId,
            WorldId = Guid.NewGuid()
        });
        rendered.QuestApi.GetArcQuestsAsync(arcId)
            .Returns(_ => Task.FromException<List<QuestDto>>(new InvalidOperationException("load failed")));

        await InvokePrivateOnRendererAsync(rendered.Cut, "LoadQuestsAsync");

        Assert.Equal("load failed", GetPrivateField<string>(rendered.Cut.Instance, "_loadingError"));
        Assert.Null(GetPrivateField<List<QuestDto>?>(rendered.Cut.Instance, "_quests"));
    }

    [Fact]
    public async Task QuestDrawer_LoadQuestsAsync_WhenAlreadyLoadedForArc_DoesNotReloadQuests()
    {
        var rendered = CreateRenderedSut();
        var articleId = Guid.NewGuid();
        var arcId = Guid.NewGuid();
        var firstQuest = CreateQuest("Quest 1", arcId);

        rendered.TreeState.SelectedNodeId.Returns(articleId);
        rendered.ArticleApi.GetArticleDetailAsync(articleId).Returns(new ArticleDto
        {
            Id = articleId,
            Type = ArticleType.Session,
            ArcId = arcId,
            WorldId = Guid.NewGuid()
        });
        SetPrivateField(rendered.Cut.Instance, "_currentArcId", arcId);
        SetPrivateField(rendered.Cut.Instance, "_questsLoadedForArc", true);
        SetPrivateField(rendered.Cut.Instance, "_quests", new List<QuestDto> { firstQuest });
        rendered.QuestApi.GetQuestUpdatesAsync(firstQuest.Id, 0, 5).Returns(new PagedResult<QuestUpdateEntryDto>
        {
            Items = new List<QuestUpdateEntryDto>(),
            TotalCount = 0
        });

        await InvokePrivateOnRendererAsync(rendered.Cut, "LoadQuestsAsync");

        await rendered.QuestApi.DidNotReceive().GetArcQuestsAsync(Arg.Any<Guid>());
        Assert.Equal(firstQuest.Id, GetPrivateField<Guid?>(rendered.Cut.Instance, "_selectedQuestId"));
    }

    [Fact]
    public async Task QuestDrawer_LoadQuestsAsync_WhenQuestAlreadySelected_DoesNotAutoSelectFirst()
    {
        var rendered = CreateRenderedSut();
        var articleId = Guid.NewGuid();
        var arcId = Guid.NewGuid();
        var quest = CreateQuest("Quest 1", arcId);
        var selectedQuestId = Guid.NewGuid();

        rendered.TreeState.SelectedNodeId.Returns(articleId);
        rendered.ArticleApi.GetArticleDetailAsync(articleId).Returns(new ArticleDto
        {
            Id = articleId,
            Type = ArticleType.Session,
            ArcId = arcId,
            WorldId = Guid.NewGuid()
        });
        rendered.QuestApi.GetArcQuestsAsync(arcId).Returns(new List<QuestDto> { quest });
        SetPrivateField(rendered.Cut.Instance, "_currentArcId", arcId);
        SetPrivateField(rendered.Cut.Instance, "_selectedQuestId", selectedQuestId);

        await InvokePrivateOnRendererAsync(rendered.Cut, "LoadQuestsAsync");

        Assert.Equal(selectedQuestId, GetPrivateField<Guid?>(rendered.Cut.Instance, "_selectedQuestId"));
    }

    [Fact]
    public async Task QuestDrawer_HandleQuestItemKeyDown_WithSpace_SelectsQuest()
    {
        var rendered = CreateRenderedSut();
        var quest = CreateQuest("Quest", Guid.NewGuid());
        SetPrivateField(rendered.Cut.Instance, "_quests", new List<QuestDto> { quest });
        rendered.QuestApi.GetQuestUpdatesAsync(quest.Id, 0, 5).Returns(new PagedResult<QuestUpdateEntryDto>
        {
            Items = new List<QuestUpdateEntryDto>(),
            TotalCount = 0
        });

        await InvokePrivateOnRendererAsync(rendered.Cut, "HandleQuestItemKeyDown", new KeyboardEventArgs { Key = " " }, quest.Id);

        Assert.Equal(quest.Id, GetPrivateField<Guid?>(rendered.Cut.Instance, "_selectedQuestId"));
    }

    [Fact]
    public async Task QuestDrawer_HandleQuestItemKeyDown_WithEnter_SelectsQuest()
    {
        var rendered = CreateRenderedSut();
        var quest = CreateQuest("Quest", Guid.NewGuid());
        SetPrivateField(rendered.Cut.Instance, "_quests", new List<QuestDto> { quest });
        rendered.QuestApi.GetQuestUpdatesAsync(quest.Id, 0, 5).Returns(new PagedResult<QuestUpdateEntryDto>
        {
            Items = new List<QuestUpdateEntryDto>(),
            TotalCount = 0
        });

        await InvokePrivateOnRendererAsync(rendered.Cut, "HandleQuestItemKeyDown", new KeyboardEventArgs { Key = "Enter" }, quest.Id);

        Assert.Equal(quest.Id, GetPrivateField<Guid?>(rendered.Cut.Instance, "_selectedQuestId"));
    }

    [Fact]
    public async Task QuestDrawer_HandleQuestItemKeyDown_WithOtherKey_DoesNotSelectQuest()
    {
        var rendered = CreateRenderedSut();
        var quest = CreateQuest("Quest", Guid.NewGuid());
        SetPrivateField(rendered.Cut.Instance, "_quests", new List<QuestDto> { quest });

        await InvokePrivateOnRendererAsync(rendered.Cut, "HandleQuestItemKeyDown", new KeyboardEventArgs { Key = "Escape" }, quest.Id);

        Assert.Null(GetPrivateField<Guid?>(rendered.Cut.Instance, "_selectedQuestId"));
    }

    [Fact]
    public void QuestDrawer_QuestItemClick_InvokesSelectQuestHandler()
    {
        var rendered = CreateRenderedSut();
        var quest = CreateQuest("Quest", Guid.NewGuid());
        SetPrivateField(rendered.Cut.Instance, "_isLoading", false);
        SetPrivateField(rendered.Cut.Instance, "_quests", new List<QuestDto> { quest });
        rendered.Cut.Render();

        var questItem = rendered.Cut.Find(".quest-item");
        questItem.Click();

        Assert.Equal(quest.Id, GetPrivateField<Guid?>(rendered.Cut.Instance, "_selectedQuestId"));
    }

    [Fact]
    public void QuestDrawer_QuestItemKeydown_InvokesKeyboardHandler()
    {
        var rendered = CreateRenderedSut();
        var quest = CreateQuest("Quest", Guid.NewGuid());
        SetPrivateField(rendered.Cut.Instance, "_isLoading", false);
        SetPrivateField(rendered.Cut.Instance, "_quests", new List<QuestDto> { quest });
        rendered.Cut.Render();

        var questItem = rendered.Cut.Find(".quest-item");
        questItem.KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.Equal(quest.Id, GetPrivateField<Guid?>(rendered.Cut.Instance, "_selectedQuestId"));
    }

    [Fact]
    public async Task QuestDrawer_SubmitUpdate_WhenSelectedQuestMissing_DoesNothing()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Cut.Instance, "_selectedQuest", null);

        await InvokePrivateOnRendererAsync(rendered.Cut, "SubmitUpdate");

        Assert.Null(GetPrivateField<string?>(rendered.Cut.Instance, "_validationError"));
    }

    [Fact]
    public async Task QuestDrawer_SubmitUpdate_WhenAlreadySubmitting_DoesNothing()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Cut.Instance, "_selectedQuest", CreateQuest("Quest", Guid.NewGuid()));
        SetPrivateField(rendered.Cut.Instance, "_isSubmitting", true);

        await InvokePrivateOnRendererAsync(rendered.Cut, "SubmitUpdate");

        await rendered.QuestApi.DidNotReceive().AddQuestUpdateAsync(Arg.Any<Guid>(), Arg.Any<QuestUpdateCreateDto>());
    }

    [Fact]
    public async Task QuestDrawer_SubmitUpdate_WhenEditorThrows_SetsError()
    {
        var rendered = CreateRenderedSut();
        var quest = CreateQuest("Quest", Guid.NewGuid());
        var module = Substitute.For<IJSObjectReference>();
        module.InvokeAsync<string>("getEditorContent", Arg.Any<object?[]>())
            .Returns(_ => ValueTask.FromException<string>(new InvalidOperationException("editor boom")));

        SetPrivateField(rendered.Cut.Instance, "_selectedQuest", quest);
        SetPrivateField(rendered.Cut.Instance, "_editorModule", module);

        await InvokePrivateOnRendererAsync(rendered.Cut, "SubmitUpdate");

        Assert.Contains("Error: editor boom", GetPrivateField<string>(rendered.Cut.Instance, "_validationError"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task QuestDrawer_SubmitUpdate_WhenAssociationDisabled_SendsNullSessionId()
    {
        var rendered = CreateRenderedSut();
        var quest = CreateQuest("Quest", Guid.NewGuid());
        var update = new QuestUpdateEntryDto { Id = Guid.NewGuid(), QuestId = quest.Id, Body = "b", CreatedByName = "A" };
        var module = Substitute.For<IJSObjectReference>();
        module.InvokeAsync<string>("getEditorContent", Arg.Any<object?[]>())
            .Returns(new ValueTask<string>("<p>update</p>"));

        rendered.QuestApi.AddQuestUpdateAsync(quest.Id, Arg.Any<QuestUpdateCreateDto>()).Returns(update);
        rendered.QuestApi.GetQuestUpdatesAsync(quest.Id, 0, 5).Returns(new PagedResult<QuestUpdateEntryDto>
        {
            Items = new List<QuestUpdateEntryDto>(),
            TotalCount = 0
        });
        rendered.QuestApi.GetQuestAsync(quest.Id).Returns(quest);

        SetPrivateField(rendered.Cut.Instance, "_selectedQuest", quest);
        SetPrivateField(rendered.Cut.Instance, "_quests", new List<QuestDto> { quest });
        SetPrivateField(rendered.Cut.Instance, "_editorModule", module);
        SetPrivateField(rendered.Cut.Instance, "_associateWithSession", false);
        SetPrivateField(rendered.Cut.Instance, "_canAssociateSession", true);
        SetPrivateField(rendered.Cut.Instance, "_currentSessionId", Guid.NewGuid());

        await InvokePrivateOnRendererAsync(rendered.Cut, "SubmitUpdate");

        await rendered.QuestApi.Received(1).AddQuestUpdateAsync(quest.Id, Arg.Is<QuestUpdateCreateDto>(d => d.SessionId == null));
    }

    [Fact]
    public async Task QuestDrawer_SubmitUpdate_WhenAssociationEnabled_SendsSessionId()
    {
        var rendered = CreateRenderedSut();
        var quest = CreateQuest("Quest", Guid.NewGuid());
        var update = new QuestUpdateEntryDto { Id = Guid.NewGuid(), QuestId = quest.Id, Body = "b", CreatedByName = "A" };
        var module = Substitute.For<IJSObjectReference>();
        var sessionId = Guid.NewGuid();
        module.InvokeAsync<string>("getEditorContent", Arg.Any<object?[]>())
            .Returns(new ValueTask<string>("<p>update</p>"));

        rendered.QuestApi.AddQuestUpdateAsync(quest.Id, Arg.Any<QuestUpdateCreateDto>()).Returns(update);
        rendered.QuestApi.GetQuestUpdatesAsync(quest.Id, 0, 5).Returns(new PagedResult<QuestUpdateEntryDto>
        {
            Items = new List<QuestUpdateEntryDto>(),
            TotalCount = 0
        });
        rendered.QuestApi.GetQuestAsync(quest.Id).Returns(quest);

        SetPrivateField(rendered.Cut.Instance, "_selectedQuest", quest);
        SetPrivateField(rendered.Cut.Instance, "_quests", new List<QuestDto> { quest });
        SetPrivateField(rendered.Cut.Instance, "_editorModule", module);
        SetPrivateField(rendered.Cut.Instance, "_associateWithSession", true);
        SetPrivateField(rendered.Cut.Instance, "_canAssociateSession", true);
        SetPrivateField(rendered.Cut.Instance, "_currentSessionId", sessionId);

        await InvokePrivateOnRendererAsync(rendered.Cut, "SubmitUpdate");

        await rendered.QuestApi.Received(1).AddQuestUpdateAsync(quest.Id, Arg.Is<QuestUpdateCreateDto>(d => d.SessionId == sessionId));
    }

    [Fact]
    public async Task QuestDrawer_HandleAutocompleteSuggestionSelected_InternalInsertsDisplayText()
    {
        var rendered = CreateRenderedSut();
        var module = Substitute.For<IJSObjectReference>();
        SetPrivateField(rendered.Cut.Instance, "_editorModule", module);

        var suggestion = new WikiLinkAutocompleteItem
        {
            IsExternal = false,
            DisplayText = "Session Note"
        };

        await InvokePrivateOnRendererAsync(rendered.Cut, "HandleAutocompleteSuggestionSelected", suggestion);

        await module.Received().InvokeVoidAsync(
            "insertWikiLink",
            Arg.Is<object?[]>(args => args.Length == 2
                && (string?)args[0] == "Session Note"
                && args[1] == null));
    }

    [Fact]
    public async Task QuestDrawer_OnAutocompleteTriggered_DelegatesToService()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Cut.Instance, "_currentWorldId", Guid.Parse("11111111-1111-1111-1111-111111111111"));

        await rendered.Cut.Instance.OnAutocompleteTriggered("abc", 10, 20);

        await rendered.AutocompleteService.Received(1)
            .ShowAsync("abc", 10, 20, Guid.Parse("11111111-1111-1111-1111-111111111111"));
    }

    [Fact]
    public async Task QuestDrawer_OnAutocompleteEnter_WithNoSelection_DoesNothing()
    {
        var rendered = CreateRenderedSut();
        rendered.AutocompleteService.GetSelectedSuggestion().Returns((WikiLinkAutocompleteItem?)null);

        await rendered.Cut.Instance.OnAutocompleteEnter();

        rendered.AutocompleteService.Received(1).GetSelectedSuggestion();
    }

    [Fact]
    public async Task QuestDrawer_OnAutocompleteEnter_WithSelection_InsertsSuggestion()
    {
        var rendered = CreateRenderedSut();
        var module = Substitute.For<IJSObjectReference>();
        SetPrivateField(rendered.Cut.Instance, "_editorModule", module);
        rendered.AutocompleteService.GetSelectedSuggestion().Returns(new WikiLinkAutocompleteItem
        {
            IsExternal = false,
            DisplayText = "Item"
        });

        await rendered.Cut.Instance.OnAutocompleteEnter();

        await module.Received().InvokeVoidAsync("insertWikiLink", Arg.Any<object?[]>());
    }

    [Fact]
    public async Task QuestDrawer_InitializeEditorAsync_WhenJsImportFails_ShowsWarning()
    {
        var rendered = CreateRenderedSut();
        rendered.JSInterop.Mode = JSRuntimeMode.Strict;

        await InvokePrivateOnRendererAsync(rendered.Cut, "InitializeEditorAsync");

        rendered.Snackbar.Received().Add("Failed to initialize editor", Severity.Warning);
    }

    [Fact]
    public async Task QuestDrawer_InitializeEditorAsync_WhenAlreadyInitialized_DoesNothing()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Cut.Instance, "_editorInitialized", true);

        await InvokePrivateOnRendererAsync(rendered.Cut, "InitializeEditorAsync");

        Assert.True(GetPrivateField<bool>(rendered.Cut.Instance, "_editorInitialized"));
    }

    [Fact]
    public async Task QuestDrawer_InitializeEditorAsync_WhenDisposed_DoesNothing()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Cut.Instance, "_disposed", true);

        await InvokePrivateOnRendererAsync(rendered.Cut, "InitializeEditorAsync");

        Assert.True(GetPrivateField<bool>(rendered.Cut.Instance, "_disposed"));
    }

    [Fact]
    public async Task QuestDrawer_FocusFirstQuestAsync_WhenJsThrows_IsIgnored()
    {
        var rendered = CreateRenderedSut();
        rendered.JSInterop.Mode = JSRuntimeMode.Strict;

        var ex = await Record.ExceptionAsync(() => InvokePrivateOnRendererAsync(rendered.Cut, "FocusFirstQuestAsync"));

        Assert.Null(ex);
    }

    [Fact]
    public async Task QuestDrawer_RestoreFocusAsync_WhenJsThrows_IsIgnored()
    {
        var rendered = CreateRenderedSut();
        rendered.JSInterop.Mode = JSRuntimeMode.Strict;

        var ex = await Record.ExceptionAsync(() => InvokePrivateOnRendererAsync(rendered.Cut, "RestoreFocusAsync"));

        Assert.Null(ex);
    }

    [Fact]
    public async Task QuestDrawer_SubmitUpdate_WhenSuccess_ClearsEditorAndShowsSuccess()
    {
        var rendered = CreateRenderedSut();
        var snackbar = rendered.Snackbar;
        var quest = CreateQuest("Quest", Guid.NewGuid());
        var update = new QuestUpdateEntryDto { Id = Guid.NewGuid(), QuestId = quest.Id, Body = "b", CreatedByName = "A" };
        var module = Substitute.For<IJSObjectReference>();

        module.InvokeAsync<string>("getEditorContent", Arg.Any<object?[]>())
            .Returns(new ValueTask<string>("<p>update</p>"));
        rendered.QuestApi.AddQuestUpdateAsync(quest.Id, Arg.Any<QuestUpdateCreateDto>()).Returns(update);
        rendered.QuestApi.GetQuestUpdatesAsync(quest.Id, 0, 5).Returns(new PagedResult<QuestUpdateEntryDto>
        {
            Items = new List<QuestUpdateEntryDto> { update },
            TotalCount = 1
        });
        rendered.QuestApi.GetQuestAsync(quest.Id).Returns(new QuestDto
        {
            Id = quest.Id,
            ArcId = quest.ArcId,
            Title = quest.Title,
            RowVersion = "v2",
            UpdatedAt = DateTime.UtcNow,
            UpdateCount = 1
        });

        SetPrivateField(rendered.Cut.Instance, "_selectedQuest", quest);
        SetPrivateField(rendered.Cut.Instance, "_quests", new List<QuestDto> { quest });
        SetPrivateField(rendered.Cut.Instance, "_editorModule", module);

        await InvokePrivateOnRendererAsync(rendered.Cut, "SubmitUpdate");

        snackbar.Received().Add("Quest update added", Severity.Success);
    }

    [Fact]
    public async Task QuestDrawer_SubmitUpdate_WhenRefreshReturnsNull_StillShowsSuccess()
    {
        var rendered = CreateRenderedSut();
        var quest = CreateQuest("Quest", Guid.NewGuid());
        var update = new QuestUpdateEntryDto { Id = Guid.NewGuid(), QuestId = quest.Id, Body = "b", CreatedByName = "A" };
        var module = Substitute.For<IJSObjectReference>();

        module.InvokeAsync<string>("getEditorContent", Arg.Any<object?[]>())
            .Returns(new ValueTask<string>("<p>update</p>"));
        rendered.QuestApi.AddQuestUpdateAsync(quest.Id, Arg.Any<QuestUpdateCreateDto>()).Returns(update);
        rendered.QuestApi.GetQuestUpdatesAsync(quest.Id, 0, 5).Returns(new PagedResult<QuestUpdateEntryDto>
        {
            Items = new List<QuestUpdateEntryDto> { update },
            TotalCount = 1
        });
        rendered.QuestApi.GetQuestAsync(quest.Id).Returns((QuestDto?)null);

        SetPrivateField(rendered.Cut.Instance, "_selectedQuest", quest);
        SetPrivateField(rendered.Cut.Instance, "_quests", new List<QuestDto> { quest });
        SetPrivateField(rendered.Cut.Instance, "_editorModule", module);

        await InvokePrivateOnRendererAsync(rendered.Cut, "SubmitUpdate");

        rendered.Snackbar.Received().Add("Quest update added", Severity.Success);
    }

    [Fact]
    public async Task QuestDrawer_AutocompleteNavigation_DelegatesToService()
    {
        var rendered = CreateRenderedSut();

        await rendered.Cut.Instance.OnAutocompleteArrowDown();
        await rendered.Cut.Instance.OnAutocompleteArrowUp();
        await rendered.Cut.Instance.OnAutocompleteHidden();

        rendered.AutocompleteService.Received(1).SelectNext();
        rendered.AutocompleteService.Received(1).SelectPrevious();
        rendered.AutocompleteService.Received(1).Hide();
    }

    [Fact]
    public async Task QuestDrawer_HandleAutocompleteSuggestionSelected_ExternalInsertsSourceKey()
    {
        var rendered = CreateRenderedSut();
        var module = Substitute.For<IJSObjectReference>();

        rendered.AutocompleteService.ExternalSourceKey.Returns("srd");
        SetPrivateField(rendered.Cut.Instance, "_editorModule", module);

        var suggestion = new WikiLinkAutocompleteItem
        {
            IsExternal = true,
            ExternalKey = "acid-arrow",
            DisplayText = "Acid Arrow"
        };

        await InvokePrivateOnRendererAsync(rendered.Cut, "HandleAutocompleteSuggestionSelected", suggestion);

        await module.Received().InvokeVoidAsync(
            "insertWikiLink",
            Arg.Is<object?[]>(args => args.Length == 2
                && (string?)args[0] == "srd/acid-arrow"
                && (string?)args[1] == "Acid Arrow"));
    }

    [Fact]
    public async Task QuestDrawer_HandleAutocompleteSuggestionSelected_WhenEditorMissing_DoesNothing()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Cut.Instance, "_editorModule", null);

        var suggestion = new WikiLinkAutocompleteItem
        {
            IsExternal = false,
            DisplayText = "Any"
        };

        await InvokePrivateOnRendererAsync(rendered.Cut, "HandleAutocompleteSuggestionSelected", suggestion);

        rendered.AutocompleteService.DidNotReceive().Hide();
    }

    [Fact]
    public async Task QuestDrawer_HandleAutocompleteSuggestionSelected_WhenEditorThrows_LogsAndDoesNotHide()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Cut.Instance, "_editorModule", new ThrowingJsObjectReference("insertWikiLink"));

        await InvokePrivateOnRendererAsync(rendered.Cut, "HandleAutocompleteSuggestionSelected", new WikiLinkAutocompleteItem
        {
            IsExternal = false,
            DisplayText = "Any"
        });

        rendered.AutocompleteService.DidNotReceive().Hide();
    }

    [Fact]
    public async Task QuestDrawer_DisposeAsync_CanRunTwice()
    {
        var rendered = CreateRenderedSut();

        await rendered.Cut.Instance.DisposeAsync();
        await rendered.Cut.Instance.DisposeAsync();

        Assert.True(GetPrivateField<bool>(rendered.Cut.Instance, "_disposed"));
    }

    [Fact]
    public void QuestDrawer_HandleOpenEvent_LoadsQuests()
    {
        var rendered = CreateRenderedSut();
        var articleId = Guid.NewGuid();
        var arcId = Guid.NewGuid();
        var quest = CreateQuest("Quest 1", arcId);

        rendered.TreeState.SelectedNodeId.Returns(articleId);
        rendered.ArticleApi.GetArticleDetailAsync(articleId).Returns(new ArticleDto
        {
            Id = articleId,
            Type = ArticleType.Session,
            WorldId = Guid.NewGuid(),
            ArcId = arcId
        });
        rendered.QuestApi.GetArcQuestsAsync(arcId).Returns(new List<QuestDto> { quest });
        rendered.QuestApi.GetQuestUpdatesAsync(quest.Id, 0, 5).Returns(new PagedResult<QuestUpdateEntryDto>
        {
            Items = new List<QuestUpdateEntryDto>(),
            TotalCount = 0
        });

        rendered.Cut.SetParametersAndRender(p => p.Add(x => x.IsOpen, true));

        rendered.Cut.WaitForAssertion(() =>
        {
            Assert.Equal(quest.Id, GetPrivateField<Guid?>(rendered.Cut.Instance, "_selectedQuestId"));
        });
    }

    [Fact]
    public void QuestDrawer_HandleCloseEvent_ResetsState()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Cut.Instance, "_selectedQuestId", Guid.NewGuid());
        SetPrivateField(rendered.Cut.Instance, "_selectedQuest", CreateQuest("Quest", Guid.NewGuid()));
        SetPrivateField(rendered.Cut.Instance, "_validationError", "bad");
        SetPrivateField(rendered.Cut.Instance, "_recentUpdates", new List<QuestUpdateEntryDto> { new() });

        rendered.Cut.SetParametersAndRender(p => p.Add(x => x.IsOpen, true));
        rendered.Cut.SetParametersAndRender(p => p.Add(x => x.IsOpen, false));

        rendered.Cut.WaitForAssertion(() =>
        {
            Assert.Null(GetPrivateField<Guid?>(rendered.Cut.Instance, "_selectedQuestId"));
            Assert.Null(GetPrivateField<QuestDto?>(rendered.Cut.Instance, "_selectedQuest"));
            Assert.Null(GetPrivateField<string?>(rendered.Cut.Instance, "_validationError"));
            Assert.Null(GetPrivateField<List<QuestUpdateEntryDto>?>(rendered.Cut.Instance, "_recentUpdates"));
        });
    }

    [Fact]
    public async Task QuestDrawer_CloseDrawer_ClosesServiceAndAttemptsRestoreFocus()
    {
        var rendered = CreateRenderedSut();
        rendered.JSInterop.Mode = JSRuntimeMode.Strict;

        await InvokePrivateOnRendererAsync(rendered.Cut, "CloseDrawer");

        rendered.QuestDrawerService.Received(1).Close();
    }

    [Fact]
    public void QuestDrawer_HandleOpenEvent_WhenNoQuests_SkipsFocusStep()
    {
        var rendered = CreateRenderedSut();
        var articleId = Guid.NewGuid();
        var arcId = Guid.NewGuid();

        rendered.TreeState.SelectedNodeId.Returns(articleId);
        rendered.ArticleApi.GetArticleDetailAsync(articleId).Returns(new ArticleDto
        {
            Id = articleId,
            Type = ArticleType.Session,
            WorldId = Guid.NewGuid(),
            ArcId = arcId
        });
        rendered.QuestApi.GetArcQuestsAsync(arcId).Returns(new List<QuestDto>());

        rendered.Cut.SetParametersAndRender(p => p.Add(x => x.IsOpen, true));

        rendered.Cut.WaitForAssertion(() =>
        {
            Assert.Null(GetPrivateField<Guid?>(rendered.Cut.Instance, "_selectedQuestId"));
        });
    }

    [Fact]
    public void QuestDrawer_HandleOpenEvent_WhenNoQuests_DoesNotThrowWithStrictJs()
    {
        var rendered = CreateRenderedSut();
        var articleId = Guid.NewGuid();
        var arcId = Guid.NewGuid();
        rendered.JSInterop.Mode = JSRuntimeMode.Strict;

        rendered.TreeState.SelectedNodeId.Returns(articleId);
        rendered.ArticleApi.GetArticleDetailAsync(articleId).Returns(new ArticleDto
        {
            Id = articleId,
            Type = ArticleType.Session,
            WorldId = Guid.NewGuid(),
            ArcId = arcId
        });
        rendered.QuestApi.GetArcQuestsAsync(arcId).Returns(new List<QuestDto>());

        var ex = Record.Exception(() => rendered.Cut.SetParametersAndRender(p => p.Add(x => x.IsOpen, true)));

        Assert.Null(ex);
    }

    [Fact]
    public void QuestDrawer_HandleOpenEvent_WhenQuestLoadFails_DoesNotAttemptFocus()
    {
        var rendered = CreateRenderedSut();
        var articleId = Guid.NewGuid();
        var arcId = Guid.NewGuid();
        rendered.JSInterop.Mode = JSRuntimeMode.Strict;

        rendered.TreeState.SelectedNodeId.Returns(articleId);
        rendered.ArticleApi.GetArticleDetailAsync(articleId).Returns(new ArticleDto
        {
            Id = articleId,
            Type = ArticleType.Session,
            WorldId = Guid.NewGuid(),
            ArcId = arcId
        });
        rendered.QuestApi.GetArcQuestsAsync(arcId)
            .Returns(_ => Task.FromException<List<QuestDto>>(new InvalidOperationException("load fail")));

        var ex = Record.Exception(() => rendered.Cut.SetParametersAndRender(p => p.Add(x => x.IsOpen, true)));

        Assert.Null(ex);
        Assert.Null(GetPrivateField<List<QuestDto>?>(rendered.Cut.Instance, "_quests"));
    }

    [Fact]
    public async Task QuestDrawer_LoadQuestsAsync_WhenQuestsNull_DoesNotAutoSelect()
    {
        var rendered = CreateRenderedSut();
        var articleId = Guid.NewGuid();
        var arcId = Guid.NewGuid();
        rendered.TreeState.SelectedNodeId.Returns(articleId);
        rendered.ArticleApi.GetArticleDetailAsync(articleId).Returns(new ArticleDto
        {
            Id = articleId,
            Type = ArticleType.Session,
            WorldId = Guid.NewGuid(),
            ArcId = arcId
        });
        SetPrivateField(rendered.Cut.Instance, "_currentArcId", arcId);
        SetPrivateField(rendered.Cut.Instance, "_quests", null);
        SetPrivateField(rendered.Cut.Instance, "_questsLoadedForArc", true);

        await InvokePrivateOnRendererAsync(rendered.Cut, "LoadQuestsAsync");

        Assert.Null(GetPrivateField<Guid?>(rendered.Cut.Instance, "_selectedQuestId"));
    }

    [Fact]
    public void QuestDrawer_Render_WhenEmptyStateMessage_ShowsMessage()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Cut.Instance, "_emptyStateMessage", "No article selected");
        rendered.Cut.Render();

        Assert.Contains("No article selected", rendered.Cut.Markup);
    }

    [Fact]
    public void QuestDrawer_Render_WhenLoadingError_ShowsErrorAndRetry()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Cut.Instance, "_loadingError", "boom");
        rendered.Cut.Render();

        Assert.Contains("Failed to load quests", rendered.Cut.Markup);
        Assert.Contains("boom", rendered.Cut.Markup);
        Assert.Contains("Retry", rendered.Cut.Markup);
    }

    [Fact]
    public void QuestDrawer_Render_WhenIsLoading_ShowsLoadingText()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Cut.Instance, "_isLoading", true);
        rendered.Cut.Render();

        Assert.Contains("Loading quests...", rendered.Cut.Markup);
    }

    [Fact]
    public void QuestDrawer_Render_WhenSelectedQuestHasDescription_ShowsDescriptionAndKeyboardHandler()
    {
        var rendered = CreateRenderedSut();
        var quest = CreateQuest("Quest", Guid.NewGuid());
        quest.Description = "<p>Desc</p>";
        SetPrivateField(rendered.Cut.Instance, "_quests", new List<QuestDto> { quest });
        SetPrivateField(rendered.Cut.Instance, "_selectedQuestId", quest.Id);
        SetPrivateField(rendered.Cut.Instance, "_selectedQuest", quest);
        rendered.Cut.Render();

        Assert.Contains("Desc", rendered.Cut.Markup);
        Assert.Contains("tabindex=\"0\"", rendered.Cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void QuestDrawer_Render_WhenValidationError_ShowsValidationText()
    {
        var rendered = CreateRenderedSut();
        var quest = CreateQuest("Quest", Guid.NewGuid());
        SetPrivateField(rendered.Cut.Instance, "_quests", new List<QuestDto> { quest });
        SetPrivateField(rendered.Cut.Instance, "_selectedQuestId", quest.Id);
        SetPrivateField(rendered.Cut.Instance, "_selectedQuest", quest);
        SetPrivateField(rendered.Cut.Instance, "_validationError", "Bad input");
        rendered.Cut.Render();

        Assert.Contains("Bad input", rendered.Cut.Markup);
    }

    [Fact]
    public void QuestDrawer_Render_WhenRecentUpdateIncludesSessionTitle_ShowsSessionContext()
    {
        var rendered = CreateRenderedSut();
        var quest = CreateQuest("Quest", Guid.NewGuid());
        SetPrivateField(rendered.Cut.Instance, "_quests", new List<QuestDto> { quest });
        SetPrivateField(rendered.Cut.Instance, "_selectedQuestId", quest.Id);
        SetPrivateField(rendered.Cut.Instance, "_selectedQuest", quest);
        SetPrivateField(rendered.Cut.Instance, "_recentUpdates", new List<QuestUpdateEntryDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                QuestId = quest.Id,
                CreatedByName = "A",
                Body = "<p>x</p>",
                SessionId = Guid.NewGuid(),
                SessionTitle = "Session 1",
                CreatedAt = DateTime.UtcNow
            }
        });
        rendered.Cut.Render();

        Assert.Contains("in Session 1", rendered.Cut.Markup);
    }

    [Fact]
    public async Task QuestDrawer_DisposeEditorAsync_WhenModuleThrows_StillResetsFlag()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Cut.Instance, "_editorInitialized", true);
        SetPrivateField(rendered.Cut.Instance, "_editorModule", new ThrowingJsObjectReference("destroyEditor"));

        await InvokePrivateOnRendererAsync(rendered.Cut, "DisposeEditorAsync");

        Assert.False(GetPrivateField<bool>(rendered.Cut.Instance, "_editorInitialized"));
    }

    [Fact]
    public async Task QuestDrawer_FocusEditorAsync_WhenModuleThrows_IsIgnored()
    {
        var rendered = CreateRenderedSut();
        SetPrivateField(rendered.Cut.Instance, "_editorModule", new ThrowingJsObjectReference("focusEditor"));

        var ex = await Record.ExceptionAsync(() => InvokePrivateOnRendererAsync(rendered.Cut, "FocusEditorAsync"));

        Assert.Null(ex);
    }

    [Fact]
    public async Task QuestDrawer_RestoreFocusAsync_WithDefaultJsRuntime_DoesNotThrow()
    {
        var rendered = CreateRenderedSut();

        var ex = await Record.ExceptionAsync(() => InvokePrivateOnRendererAsync(rendered.Cut, "RestoreFocusAsync"));

        Assert.Null(ex);
    }

    private Rendered CreateRenderedSut()
    {
        var questDrawerService = Substitute.For<IQuestDrawerService>();
        var questApi = Substitute.For<IQuestApiService>();
        var treeState = Substitute.For<ITreeStateService>();
        var autocompleteService = Substitute.For<IWikiLinkAutocompleteService>();
        var appContext = Substitute.For<IAppContextService>();
        var snackbar = Substitute.For<ISnackbar>();
        var logger = Substitute.For<ILogger<QuestDrawer>>();
        var articleApi = Substitute.For<IArticleApiService>();

        questApi.GetQuestUpdatesAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(new PagedResult<QuestUpdateEntryDto> { Items = new List<QuestUpdateEntryDto>(), TotalCount = 0 });
        autocompleteService.Suggestions.Returns(new List<WikiLinkAutocompleteItem>());

        Services.AddSingleton(questDrawerService);
        Services.AddSingleton(questApi);
        Services.AddSingleton(treeState);
        Services.AddSingleton(autocompleteService);
        Services.AddSingleton(appContext);
        Services.AddSingleton(snackbar);
        Services.AddSingleton(logger);
        Services.AddSingleton(articleApi);

        var cut = RenderComponent<QuestDrawer>();
        return new Rendered(cut, questDrawerService, questApi, treeState, autocompleteService, articleApi, snackbar, JSInterop);
    }

    private static QuestDto CreateQuest(string title, Guid arcId) => new()
    {
        Id = Guid.NewGuid(),
        ArcId = arcId,
        Title = title,
        Status = QuestStatus.Active,
        RowVersion = "v1",
        UpdatedAt = DateTime.UtcNow
    };

    private static void SetPrivateField(object instance, string fieldName, object? value)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(instance, value);
    }

    private static T GetPrivateField<T>(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (T)field.GetValue(instance)!;
    }

    private static Task InvokePrivateOnRendererAsync(IRenderedComponent<QuestDrawer> cut, string methodName, params object[] args)
        => InvokePrivateOnRendererAsync<object?>(cut, methodName, args);

    private static Task<T?> InvokePrivateOnRendererAsync<T>(IRenderedComponent<QuestDrawer> cut, string methodName, params object[] args)
    {
        return cut.InvokeAsync(async () =>
        {
            var method = cut.Instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!;
            var result = method.Invoke(cut.Instance, args);

            if (result is Task<T> typedTask)
            {
                return await typedTask;
            }

            if (result is Task task)
            {
                await task;
                return default;
            }

            return (T?)result;
        });
    }

    private sealed record Rendered(
        IRenderedComponent<QuestDrawer> Cut,
        IQuestDrawerService QuestDrawerService,
        IQuestApiService QuestApi,
        ITreeStateService TreeState,
        IWikiLinkAutocompleteService AutocompleteService,
        IArticleApiService ArticleApi,
        ISnackbar Snackbar,
        BunitJSInterop JSInterop);

    private sealed class ThrowingJsObjectReference(string methodToThrow) : IJSObjectReference
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            if (string.Equals(identifier, methodToThrow, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("boom");
            }

            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => InvokeAsync<TValue>(identifier, args);
    }
}
