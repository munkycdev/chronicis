using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Bunit;
using Chronicis.Client.Components.Quests;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Chronicis.Shared.DTOs.Quests;
using Chronicis.Shared.Enums;
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
    public async Task QuestDrawer_ResolveSessionIdFromParentAsync_FindsSession()
    {
        var rendered = CreateRenderedSut();
        var sessionId = Guid.NewGuid();
        var noteId = Guid.NewGuid();

        rendered.ArticleApi.GetArticleDetailAsync(noteId).Returns(new ArticleDto
        {
            Id = noteId,
            Type = ArticleType.SessionNote,
            ParentId = sessionId
        });
        rendered.ArticleApi.GetArticleDetailAsync(sessionId).Returns(new ArticleDto
        {
            Id = sessionId,
            Type = ArticleType.Session
        });

        var result = await InvokePrivateOnRendererAsync<Guid?>(rendered.Cut, "ResolveSessionIdFromParentAsync", noteId);

        Assert.Equal(sessionId, result);
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
    public async Task QuestDrawer_DisposeAsync_CanRunTwice()
    {
        var rendered = CreateRenderedSut();

        await rendered.Cut.Instance.DisposeAsync();
        await rendered.Cut.Instance.DisposeAsync();

        Assert.True(GetPrivateField<bool>(rendered.Cut.Instance, "_disposed"));
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
        return new Rendered(cut, questApi, treeState, autocompleteService, articleApi, snackbar);
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
        IQuestApiService QuestApi,
        ITreeStateService TreeState,
        IWikiLinkAutocompleteService AutocompleteService,
        IArticleApiService ArticleApi,
        ISnackbar Snackbar);
}
