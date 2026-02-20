using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Bunit;
using Chronicis.Client.Components.Quests;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs.Quests;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Components.Quests;

[ExcludeFromCodeCoverage]
public class ArcQuestTimelineTests : MudBlazorTestContext
{
    [Fact]
    public void ArcQuestTimeline_WhenQuestNull_ShowsSelectMessage()
    {
        RegisterServices();

        var cut = RenderComponent<ArcQuestTimeline>(p => p
            .Add(x => x.Quest, (QuestDto?)null));

        Assert.Contains("Select a quest to view updates", cut.Markup);
    }

    [Fact]
    public void ArcQuestTimeline_LoadsUpdates_WhenQuestChanges()
    {
        var questApi = RegisterServices();
        var quest = CreateQuest();

        var cut = RenderComponent<ArcQuestTimeline>(p => p
            .Add(x => x.Quest, quest));

        questApi.Received(1).GetQuestUpdatesAsync(quest.Id, 0, 20);
        Assert.Contains("Quest Updates", cut.Markup);
    }

    [Fact]
    public void ArcQuestTimeline_ShowsLoadMore_WhenHasMore()
    {
        var questApi = RegisterServices();
        var quest = CreateQuest();

        questApi.GetQuestUpdatesAsync(quest.Id, 0, 20).Returns(new PagedResult<QuestUpdateEntryDto>
        {
            Items = new List<QuestUpdateEntryDto> { CreateUpdate(quest.Id) },
            TotalCount = 2
        });

        var cut = RenderComponent<ArcQuestTimeline>(p => p.Add(x => x.Quest, quest));

        Assert.Contains("Load More", cut.Markup);
    }

    [Fact]
    public void ArcQuestTimeline_ShowsUpdateCountText_WhenQuestHasUpdates()
    {
        RegisterServices();
        var quest = CreateQuest();
        quest.UpdateCount = 2;

        var cut = RenderComponent<ArcQuestTimeline>(p => p.Add(x => x.Quest, quest));

        Assert.Contains("2 updates", cut.Markup);
    }

    [Fact]
    public void ArcQuestTimeline_ShowsSingularUpdateCount_WhenQuestHasOneUpdate()
    {
        RegisterServices();
        var quest = CreateQuest();
        quest.UpdateCount = 1;

        var cut = RenderComponent<ArcQuestTimeline>(p => p.Add(x => x.Quest, quest));

        Assert.Contains("1 update", cut.Markup);
    }

    [Fact]
    public async Task ArcQuestTimeline_LoadMore_AppendsData()
    {
        var questApi = RegisterServices();
        var quest = CreateQuest();

        questApi.GetQuestUpdatesAsync(quest.Id, 0, 20).Returns(new PagedResult<QuestUpdateEntryDto>
        {
            Items = new List<QuestUpdateEntryDto> { CreateUpdate(quest.Id, "first") },
            TotalCount = 3
        });

        questApi.GetQuestUpdatesAsync(quest.Id, 1, 20).Returns(new PagedResult<QuestUpdateEntryDto>
        {
            Items = new List<QuestUpdateEntryDto> { CreateUpdate(quest.Id, "second") },
            TotalCount = 3
        });

        var cut = RenderComponent<ArcQuestTimeline>(p => p.Add(x => x.Quest, quest));

        await InvokePrivateOnRendererAsync(cut, "LoadMoreAsync");

        Assert.Contains("first", cut.Markup);
        Assert.Contains("second", cut.Markup);
    }

    [Fact]
    public async Task ArcQuestTimeline_LoadUpdates_WhenApiThrows_ShowsError()
    {
        var questApi = Substitute.For<IQuestApiService>();
        var snackbar = Substitute.For<ISnackbar>();

        var quest = CreateQuest();
        questApi.GetQuestUpdatesAsync(quest.Id, Arg.Any<int>(), Arg.Any<int>())
            .Returns(_ => Task.FromException<PagedResult<QuestUpdateEntryDto>>(new InvalidOperationException("boom")));

        Services.AddSingleton(questApi);
        Services.AddSingleton(Substitute.For<IDialogService>());
        Services.AddSingleton(snackbar);

        var cut = RenderComponent<ArcQuestTimeline>(p => p.Add(x => x.Quest, quest));
        await InvokePrivateOnRendererAsync(cut, "LoadUpdatesAsync");

        snackbar.Received().Add(Arg.Is<string>(s => s.Contains("Failed to load quest updates")), Severity.Error);
    }

    [Fact]
    public async Task ArcQuestTimeline_LoadMore_WhenApiThrows_ShowsError()
    {
        var questApi = RegisterServices();
        var snackbar = Services.GetRequiredService<ISnackbar>();
        var quest = CreateQuest();

        questApi.GetQuestUpdatesAsync(quest.Id, 0, 20).Returns(new PagedResult<QuestUpdateEntryDto>
        {
            Items = new List<QuestUpdateEntryDto> { CreateUpdate(quest.Id, "first") },
            TotalCount = 2
        });
        questApi.GetQuestUpdatesAsync(quest.Id, 1, 20)
            .Returns(_ => Task.FromException<PagedResult<QuestUpdateEntryDto>>(new InvalidOperationException("load-more-fail")));

        var cut = RenderComponent<ArcQuestTimeline>(p => p.Add(x => x.Quest, quest));
        await InvokePrivateOnRendererAsync(cut, "LoadMoreAsync");

        snackbar.Received().Add(Arg.Is<string>(s => s.Contains("Failed to load more updates")), Severity.Error);
    }

    [Fact]
    public async Task ArcQuestTimeline_DeleteUpdate_WhenConfirmFalse_DoesNothing()
    {
        var questApi = RegisterServices();
        var dialog = Services.GetRequiredService<IDialogService>();
        var quest = CreateQuest();
        var update = CreateUpdate(quest.Id);

        dialog.ShowMessageBox(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DialogOptions>())
            .Returns(Task.FromResult<bool?>(false));

        var cut = RenderComponent<ArcQuestTimeline>(p => p.Add(x => x.Quest, quest));

        await InvokePrivateOnRendererAsync(cut, "DeleteUpdate", update);

        await questApi.DidNotReceive().DeleteQuestUpdateAsync(Arg.Any<Guid>(), Arg.Any<Guid>());
    }

    [Fact]
    public async Task ArcQuestTimeline_DeleteUpdate_WhenSuccess_RemovesAndShowsSuccess()
    {
        var questApi = RegisterServices();
        var dialog = Services.GetRequiredService<IDialogService>();
        var snackbar = Services.GetRequiredService<ISnackbar>();

        var quest = CreateQuest();
        var update = CreateUpdate(quest.Id);

        questApi.GetQuestUpdatesAsync(quest.Id, 0, 20).Returns(new PagedResult<QuestUpdateEntryDto>
        {
            Items = new List<QuestUpdateEntryDto> { update },
            TotalCount = 1
        });

        dialog.ShowMessageBox(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DialogOptions>())
            .Returns(Task.FromResult<bool?>(true));
        questApi.DeleteQuestUpdateAsync(quest.Id, update.Id).Returns(true);

        var cut = RenderComponent<ArcQuestTimeline>(p => p
            .Add(x => x.Quest, quest)
            .Add(x => x.IsGm, true));

        await InvokePrivateOnRendererAsync(cut, "DeleteUpdate", update);

        snackbar.Received().Add("Quest update deleted", Severity.Success);
    }

    [Fact]
    public async Task ArcQuestTimeline_DeleteUpdate_WhenQuestIsNull_ReturnsEarly()
    {
        var questApi = RegisterServices();
        var update = CreateUpdate(Guid.NewGuid());
        var cut = RenderComponent<ArcQuestTimeline>(p => p.Add(x => x.Quest, (QuestDto?)null));

        await InvokePrivateOnRendererAsync(cut, "DeleteUpdate", update);

        await questApi.DidNotReceive().DeleteQuestUpdateAsync(Arg.Any<Guid>(), Arg.Any<Guid>());
    }

    [Fact]
    public async Task ArcQuestTimeline_LoadUpdates_WhenQuestIsNull_ReturnsEarly()
    {
        var questApi = RegisterServices();
        var cut = RenderComponent<ArcQuestTimeline>(p => p.Add(x => x.Quest, (QuestDto?)null));

        await InvokePrivateOnRendererAsync(cut, "LoadUpdatesAsync");

        await questApi.DidNotReceive().GetQuestUpdatesAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>());
    }

    [Fact]
    public async Task ArcQuestTimeline_LoadMore_WhenAlreadyLoadingMore_ReturnsEarly()
    {
        var questApi = RegisterServices();
        var quest = CreateQuest();
        var cut = RenderComponent<ArcQuestTimeline>(p => p.Add(x => x.Quest, quest));
        SetField(cut.Instance, "_isLoadingMore", true);
        var beforeCalls = GetQuestLoadCallCount(questApi);

        await InvokePrivateOnRendererAsync(cut, "LoadMoreAsync");

        Assert.Equal(beforeCalls, GetQuestLoadCallCount(questApi));
    }

    [Fact]
    public async Task ArcQuestTimeline_LoadMore_WhenQuestIsNull_ReturnsEarly()
    {
        var questApi = RegisterServices();
        var cut = RenderComponent<ArcQuestTimeline>(p => p.Add(x => x.Quest, (QuestDto?)null));
        var beforeCalls = GetQuestLoadCallCount(questApi);

        await InvokePrivateOnRendererAsync(cut, "LoadMoreAsync");

        Assert.Equal(beforeCalls, GetQuestLoadCallCount(questApi));
    }

    [Fact]
    public async Task ArcQuestTimeline_OnParametersSetAsync_WhenBothQuestIdsNull_DoesNotLoad()
    {
        var questApi = RegisterServices();
        var cut = RenderComponent<ArcQuestTimeline>(p => p.Add(x => x.Quest, (QuestDto?)null));
        var beforeCalls = GetQuestLoadCallCount(questApi);

        await InvokePrivateOnRendererAsync(cut, "OnParametersSetAsync");

        Assert.Equal(beforeCalls, GetQuestLoadCallCount(questApi));
    }

    [Fact]
    public void ArcQuestTimeline_OnParametersSetAsync_WhenQuestIdUnchanged_DoesNotLoadAgain()
    {
        var questApi = RegisterServices();
        var quest = CreateQuest();
        var cut = RenderComponent<ArcQuestTimeline>(p => p.Add(x => x.Quest, quest));
        var beforeCalls = GetQuestLoadCallCount(questApi);
        var sameQuestDifferentObject = new QuestDto
        {
            Id = quest.Id,
            ArcId = quest.ArcId,
            Title = "Updated title",
            RowVersion = "v2"
        };

        cut.SetParametersAndRender(p => p.Add(x => x.Quest, sameQuestDifferentObject));

        Assert.Equal(beforeCalls, GetQuestLoadCallCount(questApi));
    }

    [Fact]
    public async Task ArcQuestTimeline_DeleteUpdate_WhenFailure_ShowsError()
    {
        var questApi = RegisterServices();
        var dialog = Services.GetRequiredService<IDialogService>();
        var snackbar = Services.GetRequiredService<ISnackbar>();

        var quest = CreateQuest();
        var update = CreateUpdate(quest.Id);

        dialog.ShowMessageBox(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DialogOptions>())
            .Returns(Task.FromResult<bool?>(true));
        questApi.DeleteQuestUpdateAsync(quest.Id, update.Id).Returns(false);

        var cut = RenderComponent<ArcQuestTimeline>(p => p.Add(x => x.Quest, quest));

        await InvokePrivateOnRendererAsync(cut, "DeleteUpdate", update);

        snackbar.Received().Add("Failed to delete quest update", Severity.Error);
    }

    [Theory]
    [InlineData(true, false, true)]
    [InlineData(false, true, true)]
    [InlineData(false, false, false)]
    public void ArcQuestTimeline_CanDeleteUpdate_Branches(bool isGm, bool isOwner, bool expected)
    {
        RegisterServices();
        var currentUserId = Guid.NewGuid();
        var creator = isOwner ? currentUserId : Guid.NewGuid();
        var update = new QuestUpdateEntryDto { CreatedBy = creator };

        var cut = RenderComponent<ArcQuestTimeline>(p => p
            .Add(x => x.IsGm, isGm)
            .Add(x => x.CurrentUserId, currentUserId));

        var result = (bool)InvokePrivate(cut.Instance, "CanDeleteUpdate", update)!;

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ArcQuestTimeline_RendersAvatarAndSessionChip()
    {
        var questApi = RegisterServices();
        var quest = CreateQuest();
        questApi.GetQuestUpdatesAsync(quest.Id, 0, 20).Returns(new PagedResult<QuestUpdateEntryDto>
        {
            Items = new List<QuestUpdateEntryDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    QuestId = quest.Id,
                    Body = "body",
                    CreatedBy = Guid.NewGuid(),
                    CreatedByName = "Author",
                    CreatedByAvatarUrl = "https://example.com/avatar.png",
                    SessionId = Guid.NewGuid(),
                    SessionTitle = "Session 12",
                    CreatedAt = DateTime.UtcNow
                }
            },
            TotalCount = 1
        });

        var cut = RenderComponent<ArcQuestTimeline>(p => p.Add(x => x.Quest, quest));

        Assert.Contains("avatar.png", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Session: Session 12", cut.Markup);
    }

    [Fact]
    public async Task ArcQuestTimeline_DeleteButtonClick_InvokesDeleteHandler()
    {
        var questApi = RegisterServices();
        var dialog = Services.GetRequiredService<IDialogService>();
        var quest = CreateQuest();
        var update = CreateUpdate(quest.Id);
        var currentUserId = update.CreatedBy;

        questApi.GetQuestUpdatesAsync(quest.Id, 0, 20).Returns(new PagedResult<QuestUpdateEntryDto>
        {
            Items = new List<QuestUpdateEntryDto> { update },
            TotalCount = 1
        });
        dialog.ShowMessageBox(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DialogOptions>())
            .Returns(Task.FromResult<bool?>(false));

        var cut = RenderComponent<ArcQuestTimeline>(p => p
            .Add(x => x.Quest, quest)
            .Add(x => x.IsGm, false)
            .Add(x => x.CurrentUserId, currentUserId));

        var deleteButton = cut.Find("button.mud-icon-button");
        await cut.InvokeAsync(() => deleteButton.Click());

        await questApi.DidNotReceive().DeleteQuestUpdateAsync(Arg.Any<Guid>(), Arg.Any<Guid>());
    }

    private IQuestApiService RegisterServices()
    {
        var questApi = Substitute.For<IQuestApiService>();
        questApi.GetQuestUpdatesAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(new PagedResult<QuestUpdateEntryDto>
            {
                Items = new List<QuestUpdateEntryDto>(),
                TotalCount = 0
            });

        Services.AddSingleton(questApi);
        Services.AddSingleton(Substitute.For<IDialogService>());
        Services.AddSingleton(Substitute.For<ISnackbar>());

        return questApi;
    }

    private static QuestDto CreateQuest() => new()
    {
        Id = Guid.NewGuid(),
        ArcId = Guid.NewGuid(),
        Title = "Quest",
        RowVersion = "v1"
    };

    private static QuestUpdateEntryDto CreateUpdate(Guid questId, string body = "body") => new()
    {
        Id = Guid.NewGuid(),
        QuestId = questId,
        Body = body,
        CreatedBy = Guid.NewGuid(),
        CreatedByName = "Author",
        CreatedAt = DateTime.UtcNow
    };

    private static object? InvokePrivate(object instance, string methodName, params object[] args)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        return method.Invoke(instance, args);
    }

    private static Task InvokePrivateOnRendererAsync(IRenderedComponent<ArcQuestTimeline> cut, string methodName, params object[] args)
    {
        return cut.InvokeAsync(async () =>
        {
            var method = cut.Instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!;
            var result = method.Invoke(cut.Instance, args);

            if (result is Task task)
            {
                await task;
            }
        });
    }

    private static int GetQuestLoadCallCount(IQuestApiService questApi)
    {
        return questApi.ReceivedCalls().Count(c => c.GetMethodInfo().Name == nameof(IQuestApiService.GetQuestUpdatesAsync));
    }

    private static void SetField(object instance, string fieldName, object? value)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field!.SetValue(instance, value);
    }
}
