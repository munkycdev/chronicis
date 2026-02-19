using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Bunit;
using Chronicis.Client.Components.Dialogs;
using Chronicis.Client.Components.Quests;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs.Quests;
using Chronicis.Shared.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MudBlazor;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Components.Quests;

[ExcludeFromCodeCoverage]
public class ArcQuestListTests : MudBlazorTestContext
{
    [Fact]
    public void ArcQuestList_NonGm_HidesCreateButton()
    {
        var questApi = RegisterServices(new List<QuestDto>());

        var cut = RenderComponent<ArcQuestList>(p => p
            .Add(x => x.ArcId, Guid.NewGuid())
            .Add(x => x.IsGm, false));

        Assert.DoesNotContain("New Quest", cut.Markup);
        Assert.True(GetQuestLoadCallCount(questApi) >= 1);
    }

    [Fact]
    public void ArcQuestList_Gm_ShowsCreateButton()
    {
        RegisterServices(new List<QuestDto>());

        var cut = RenderComponent<ArcQuestList>(p => p
            .Add(x => x.ArcId, Guid.NewGuid())
            .Add(x => x.IsGm, true));

        Assert.Contains("New Quest", cut.Markup);
    }

    [Fact]
    public void ArcQuestList_WhenLoadFails_ShowsError()
    {
        var questApi = Substitute.For<IQuestApiService>();
        questApi.GetArcQuestsAsync(Arg.Any<Guid>())
            .Returns(_ => Task.FromException<List<QuestDto>>(new InvalidOperationException("boom")));

        Services.AddSingleton(questApi);
        Services.AddSingleton(Substitute.For<IDialogService>());
        Services.AddSingleton(Substitute.For<ISnackbar>());
        Services.AddSingleton(Substitute.For<ILogger<ArcQuestList>>());

        var cut = RenderComponent<ArcQuestList>(p => p
            .Add(x => x.ArcId, Guid.NewGuid())
            .Add(x => x.IsGm, true));

        Assert.Contains("Failed to load quests", cut.Markup);
        Assert.Contains("boom", cut.Markup);
    }

    [Fact]
    public async Task ArcQuestList_CreateQuest_WhenNotGm_ReturnsEarly()
    {
        var questApi = RegisterServices(new List<QuestDto>());
        var cut = RenderComponent<ArcQuestList>(p => p
            .Add(x => x.ArcId, Guid.NewGuid())
            .Add(x => x.IsGm, false));

        await InvokePrivateOnRendererAsync(cut, "CreateQuest");

        Assert.True(GetQuestLoadCallCount(questApi) >= 1);
    }

    [Fact]
    public async Task ArcQuestList_CreateQuest_WhenDialogConfirms_ReloadsAndShowsSuccess()
    {
        var questApi = RegisterServices(new List<QuestDto>());
        var dialogService = Services.GetRequiredService<IDialogService>();
        var snackbar = Services.GetRequiredService<ISnackbar>();

        var dialogRef = Substitute.For<IDialogReference>();
        dialogRef.Result.Returns(Task.FromResult<DialogResult?>(DialogResult.Ok(new object())));
        dialogService.ShowAsync<CreateQuestDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>())
            .Returns(Task.FromResult(dialogRef));

        var cut = RenderComponent<ArcQuestList>(p => p
            .Add(x => x.ArcId, Guid.NewGuid())
            .Add(x => x.IsGm, true));

        await InvokePrivateOnRendererAsync(cut, "CreateQuest");

        Assert.True(GetQuestLoadCallCount(questApi) >= 2);
        snackbar.Received().Add("Quest created successfully", Severity.Success);
    }

    [Fact]
    public async Task ArcQuestList_CreateQuest_WhenDialogCanceled_DoesNotShowSuccess()
    {
        RegisterServices(new List<QuestDto>());
        var dialogService = Services.GetRequiredService<IDialogService>();
        var snackbar = Services.GetRequiredService<ISnackbar>();

        var dialogRef = Substitute.For<IDialogReference>();
        dialogRef.Result.Returns(Task.FromResult<DialogResult?>(DialogResult.Cancel()));
        dialogService.ShowAsync<CreateQuestDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>())
            .Returns(Task.FromResult(dialogRef));

        var cut = RenderComponent<ArcQuestList>(p => p
            .Add(x => x.ArcId, Guid.NewGuid())
            .Add(x => x.IsGm, true));

        await InvokePrivateOnRendererAsync(cut, "CreateQuest");

        snackbar.DidNotReceive().Add("Quest created successfully", Severity.Success);
    }

    [Fact]
    public async Task ArcQuestList_EditQuest_Gm_ShowsInfo()
    {
        RegisterServices(new List<QuestDto>());
        var snackbar = Services.GetRequiredService<ISnackbar>();
        var quest = CreateQuest("Edit Me");

        var cut = RenderComponent<ArcQuestList>(p => p
            .Add(x => x.ArcId, quest.ArcId)
            .Add(x => x.IsGm, true));

        InvokePrivate(cut.Instance, "EditQuest", quest);

        snackbar.Received().Add(Arg.Is<string>(s => s.Contains("Editing quest")), Severity.Info);
    }

    [Fact]
    public void ArcQuestList_EditQuest_NonGm_ReturnsWithoutSnackbar()
    {
        RegisterServices(new List<QuestDto>());
        var snackbar = Services.GetRequiredService<ISnackbar>();
        var quest = CreateQuest("No Edit");

        var cut = RenderComponent<ArcQuestList>(p => p
            .Add(x => x.ArcId, quest.ArcId)
            .Add(x => x.IsGm, false));

        InvokePrivate(cut.Instance, "EditQuest", quest);

        snackbar.DidNotReceive().Add(Arg.Any<string>(), Arg.Any<Severity>());
    }

    [Fact]
    public async Task ArcQuestList_DeleteQuest_ConfirmTrueAndApiSuccess_ShowsSuccess()
    {
        var quest = CreateQuest("Delete Me");
        var questApi = RegisterServices(new List<QuestDto> { quest });
        var dialogService = Services.GetRequiredService<IDialogService>();
        var snackbar = Services.GetRequiredService<ISnackbar>();

        dialogService.ShowMessageBox(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DialogOptions>())
            .Returns(Task.FromResult<bool?>(true));
        questApi.DeleteQuestAsync(quest.Id).Returns(true);

        var cut = RenderComponent<ArcQuestList>(p => p
            .Add(x => x.ArcId, quest.ArcId)
            .Add(x => x.IsGm, true));

        await InvokePrivateOnRendererAsync(cut, "DeleteQuest", quest);

        await questApi.Received().DeleteQuestAsync(quest.Id);
        snackbar.Received().Add("Quest deleted successfully", Severity.Success);
    }

    [Fact]
    public async Task ArcQuestList_DeleteQuest_ConfirmFalse_DoesNotDelete()
    {
        var quest = CreateQuest("Keep Me");
        var questApi = RegisterServices(new List<QuestDto> { quest });
        var dialogService = Services.GetRequiredService<IDialogService>();

        dialogService.ShowMessageBox(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DialogOptions>())
            .Returns(Task.FromResult<bool?>(false));

        var cut = RenderComponent<ArcQuestList>(p => p
            .Add(x => x.ArcId, quest.ArcId)
            .Add(x => x.IsGm, true));

        await InvokePrivateOnRendererAsync(cut, "DeleteQuest", quest);

        await questApi.DidNotReceive().DeleteQuestAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task ArcQuestList_DeleteQuest_WhenApiThrows_ShowsError()
    {
        var quest = CreateQuest("Boom");
        var questApi = RegisterServices(new List<QuestDto> { quest });
        var dialogService = Services.GetRequiredService<IDialogService>();
        var snackbar = Services.GetRequiredService<ISnackbar>();

        dialogService.ShowMessageBox(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DialogOptions>())
            .Returns(Task.FromResult<bool?>(true));
        questApi.DeleteQuestAsync(quest.Id)
            .Returns(_ => Task.FromException<bool>(new InvalidOperationException("explode")));

        var cut = RenderComponent<ArcQuestList>(p => p
            .Add(x => x.ArcId, quest.ArcId)
            .Add(x => x.IsGm, true));

        await InvokePrivateOnRendererAsync(cut, "DeleteQuest", quest);

        snackbar.Received().Add(Arg.Is<string>(s => s.Contains("Error deleting quest")), Severity.Error);
    }

    private IQuestApiService RegisterServices(List<QuestDto> quests)
    {
        var questApi = Substitute.For<IQuestApiService>();
        questApi.GetArcQuestsAsync(Arg.Any<Guid>()).Returns(quests);

        Services.AddSingleton(questApi);
        Services.AddSingleton(Substitute.For<IDialogService>());
        Services.AddSingleton(Substitute.For<ISnackbar>());
        Services.AddSingleton(Substitute.For<ILogger<ArcQuestList>>());

        return questApi;
    }

    private static QuestDto CreateQuest(string title)
    {
        return new QuestDto
        {
            Id = Guid.NewGuid(),
            ArcId = Guid.NewGuid(),
            Title = title,
            Status = QuestStatus.Active,
            UpdatedAt = DateTime.UtcNow,
            RowVersion = "v1"
        };
    }

    private static void InvokePrivate(object instance, string methodName, params object[] args)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        _ = method.Invoke(instance, args);
    }

    private static Task InvokePrivateOnRendererAsync(IRenderedComponent<ArcQuestList> cut, string methodName, params object[] args)
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
        return questApi.ReceivedCalls().Count(c => c.GetMethodInfo().Name == nameof(IQuestApiService.GetArcQuestsAsync));
    }
}
