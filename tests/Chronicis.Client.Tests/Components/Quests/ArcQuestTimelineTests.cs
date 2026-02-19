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
}
