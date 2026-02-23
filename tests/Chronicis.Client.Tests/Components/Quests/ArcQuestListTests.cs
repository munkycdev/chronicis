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
    public async Task ArcQuestList_CreateQuest_WhenDialogResultNull_DoesNotShowSuccess()
    {
        RegisterServices(new List<QuestDto>());
        var dialogService = Services.GetRequiredService<IDialogService>();
        var snackbar = Services.GetRequiredService<ISnackbar>();

        var dialogRef = Substitute.For<IDialogReference>();
        dialogRef.Result.Returns(Task.FromResult<DialogResult?>(null));
        dialogService.ShowAsync<CreateQuestDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>())
            .Returns(Task.FromResult(dialogRef));

        var cut = RenderComponent<ArcQuestList>(p => p
            .Add(x => x.ArcId, Guid.NewGuid())
            .Add(x => x.IsGm, true));

        await InvokePrivateOnRendererAsync(cut, "CreateQuest");

        snackbar.DidNotReceive().Add("Quest created successfully", Severity.Success);
    }

    [Fact]
    public async Task ArcQuestList_CreateQuest_WhenDialogThrows_ShowsErrorMessage()
    {
        RegisterServices(new List<QuestDto>());
        var dialogService = Services.GetRequiredService<IDialogService>();
        var snackbar = Services.GetRequiredService<ISnackbar>();

        dialogService.ShowAsync<CreateQuestDialog>(Arg.Any<string>(), Arg.Any<DialogParameters>())
            .Returns(_ => Task.FromException<IDialogReference>(new InvalidOperationException("explode")));

        var cut = RenderComponent<ArcQuestList>(p => p
            .Add(x => x.ArcId, Guid.NewGuid())
            .Add(x => x.IsGm, true));

        await InvokePrivateOnRendererAsync(cut, "CreateQuest");

        snackbar.Received().Add(Arg.Is<string>(m => m.Contains("Failed to create quest: explode")), Severity.Error);
    }

    [Fact]
    public async Task ArcQuestList_EditQuest_Gm_InvokesOnEditQuestCallback()
    {
        RegisterServices(new List<QuestDto>());
        var quest = CreateQuest("Edit Me");
        QuestDto? capturedQuest = null;

        var cut = RenderComponent<ArcQuestList>(p => p
            .Add(x => x.ArcId, quest.ArcId)
            .Add(x => x.IsGm, true)
            .Add(x => x.OnEditQuest, (QuestDto q) => { capturedQuest = q; }));

        await cut.InvokeAsync(() => cut.Instance.GetType()
            .GetMethod("EditQuest", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(cut.Instance, new object[] { quest }));

        Assert.Equal(quest, capturedQuest);
    }

    [Fact]
    public async Task ArcQuestList_EditQuest_NonGm_DoesNotInvokeCallback()
    {
        RegisterServices(new List<QuestDto>());
        var quest = CreateQuest("No Edit");
        QuestDto? capturedQuest = null;

        var cut = RenderComponent<ArcQuestList>(p => p
            .Add(x => x.ArcId, quest.ArcId)
            .Add(x => x.IsGm, false)
            .Add(x => x.OnEditQuest, (QuestDto q) => { capturedQuest = q; }));

        await cut.InvokeAsync(() => cut.Instance.GetType()
            .GetMethod("EditQuest", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(cut.Instance, new object[] { quest }));

        Assert.Null(capturedQuest);
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
    public async Task ArcQuestList_DeleteQuest_ConfirmTrueAndApiFailure_ShowsError()
    {
        var quest = CreateQuest("No Delete");
        var questApi = RegisterServices(new List<QuestDto> { quest });
        var dialogService = Services.GetRequiredService<IDialogService>();
        var snackbar = Services.GetRequiredService<ISnackbar>();

        dialogService.ShowMessageBox(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DialogOptions>())
            .Returns(Task.FromResult<bool?>(true));
        questApi.DeleteQuestAsync(quest.Id).Returns(false);

        var cut = RenderComponent<ArcQuestList>(p => p
            .Add(x => x.ArcId, quest.ArcId)
            .Add(x => x.IsGm, true));

        await InvokePrivateOnRendererAsync(cut, "DeleteQuest", quest);

        snackbar.Received().Add("Failed to delete quest", Severity.Error);
    }

    [Fact]
    public async Task ArcQuestList_DeleteQuest_WhenAlreadyDeleting_ReturnsEarly()
    {
        var quest = CreateQuest("Busy");
        var questApi = RegisterServices(new List<QuestDto> { quest });
        var cut = RenderComponent<ArcQuestList>(p => p
            .Add(x => x.ArcId, quest.ArcId)
            .Add(x => x.IsGm, true));
        SetField(cut.Instance, "_isDeleting", true);

        await InvokePrivateOnRendererAsync(cut, "DeleteQuest", quest);

        await questApi.DidNotReceive().DeleteQuestAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task ArcQuestList_DeleteQuest_WhenNotGm_ReturnsEarly()
    {
        var quest = CreateQuest("No Perms");
        var questApi = RegisterServices(new List<QuestDto> { quest });
        var cut = RenderComponent<ArcQuestList>(p => p
            .Add(x => x.ArcId, quest.ArcId)
            .Add(x => x.IsGm, false));

        await InvokePrivateOnRendererAsync(cut, "DeleteQuest", quest);

        await questApi.DidNotReceive().DeleteQuestAsync(Arg.Any<Guid>());
    }

    [Fact]
    public void ArcQuestList_OnParametersSetAsync_WhenArcIdEmpty_DoesNotReload()
    {
        var questApi = RegisterServices(new List<QuestDto>());
        var cut = RenderComponent<ArcQuestList>(p => p
            .Add(x => x.ArcId, Guid.NewGuid())
            .Add(x => x.IsGm, true));
        var before = GetQuestLoadCallCount(questApi);

        cut.SetParametersAndRender(p => p.Add(x => x.ArcId, Guid.Empty));

        Assert.Equal(before, GetQuestLoadCallCount(questApi));
    }

    [Fact]
    public void ArcQuestList_RendersQuestRows_WithGmOnlyAndPluralizedUpdates()
    {
        var quests = new List<QuestDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ArcId = Guid.NewGuid(),
                Title = "Quest One",
                Status = QuestStatus.Active,
                IsGmOnly = true,
                UpdateCount = 2,
                UpdatedAt = DateTime.UtcNow,
                RowVersion = "v1"
            },
            new()
            {
                Id = Guid.NewGuid(),
                ArcId = Guid.NewGuid(),
                Title = "Quest Two",
                Status = QuestStatus.Completed,
                IsGmOnly = false,
                UpdateCount = 1,
                UpdatedAt = DateTime.UtcNow.AddMinutes(-5),
                RowVersion = "v2"
            }
        };
        RegisterServices(quests);

        var cut = RenderComponent<ArcQuestList>(p => p
            .Add(x => x.ArcId, quests[0].ArcId)
            .Add(x => x.IsGm, true));

        Assert.Contains("GM Only", cut.Markup);
        Assert.Contains("2 updates", cut.Markup);
        Assert.Contains("1 update", cut.Markup);
        Assert.NotEmpty(cut.FindAll("button[title='Edit quest']"));
        Assert.NotEmpty(cut.FindAll("button[title='Delete quest']"));
    }

    [Fact]
    public async Task ArcQuestList_EditAndDeleteButtonClick_InvokeHandlers()
    {
        var quest = CreateQuest("Clickable");
        var questApi = RegisterServices(new List<QuestDto> { quest });
        var dialogService = Services.GetRequiredService<IDialogService>();
        dialogService.ShowMessageBox(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DialogOptions>())
            .Returns(Task.FromResult<bool?>(false));

        QuestDto? capturedQuest = null;
        var cut = RenderComponent<ArcQuestList>(p => p
            .Add(x => x.ArcId, quest.ArcId)
            .Add(x => x.IsGm, true)
            .Add(x => x.OnEditQuest, (QuestDto q) => { capturedQuest = q; }));

        var editButton = cut.Find("button[title='Edit quest']");
        var deleteButton = cut.Find("button[title='Delete quest']");

        await cut.InvokeAsync(() => editButton.Click());
        await cut.InvokeAsync(() => deleteButton.Click());

        Assert.Equal(quest.Id, capturedQuest?.Id);
        await questApi.DidNotReceive().DeleteQuestAsync(Arg.Any<Guid>());
    }

    [Fact]
    public void ArcQuestList_EmptyState_ForGmAndNonGm()
    {
        RegisterServices(new List<QuestDto>());

        var gmCut = RenderComponent<ArcQuestList>(p => p
            .Add(x => x.ArcId, Guid.NewGuid())
            .Add(x => x.IsGm, true));
        var playerCut = RenderComponent<ArcQuestList>(p => p
            .Add(x => x.ArcId, Guid.NewGuid())
            .Add(x => x.IsGm, false));

        Assert.Contains("Create your first quest", gmCut.Markup);
        Assert.Contains("hasn't created any quests yet", playerCut.Markup);
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

    private static void SetField(object instance, string fieldName, object? value)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field!.SetValue(instance, value);
    }

    private static int GetQuestLoadCallCount(IQuestApiService questApi)
    {
        return questApi.ReceivedCalls().Count(c => c.GetMethodInfo().Name == nameof(IQuestApiService.GetArcQuestsAsync));
    }
}
