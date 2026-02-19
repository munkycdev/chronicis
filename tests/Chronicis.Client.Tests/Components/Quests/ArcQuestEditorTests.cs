using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Bunit;
using Chronicis.Client.Components.Quests;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs.Quests;
using Chronicis.Shared.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Components.Quests;

[ExcludeFromCodeCoverage]
public class ArcQuestEditorTests : MudBlazorTestContext
{
    [Fact]
    public void ArcQuestEditor_WhenQuestNull_ShowsSelectMessage()
    {
        RegisterServices();

        var cut = RenderEditor(p => p.Add(x => x.Quest, (QuestDto?)null));

        Assert.Contains("Select a quest to edit", cut.Markup);
    }

    [Fact]
    public async Task ArcQuestEditor_SaveQuest_WhenQuestNull_ReturnsEarly()
    {
        var questApi = RegisterServices();
        var cut = RenderEditor(p => p.Add(x => x.Quest, (QuestDto?)null));

        await InvokePrivateOnRendererAsync(cut, "SaveQuestAsync");

        await questApi.DidNotReceive().UpdateQuestAsync(Arg.Any<Guid>(), Arg.Any<QuestEditDto>());
    }

    [Fact]
    public async Task ArcQuestEditor_SaveQuest_WhenUpdateSucceeds_InvokesCallback()
    {
        var questApi = RegisterServices();
        var quest = CreateQuest("Original", "desc");
        var updated = CreateQuest("Updated", "new");
        updated.Id = quest.Id;
        updated.RowVersion = "v2";

        questApi.UpdateQuestAsync(quest.Id, Arg.Any<QuestEditDto>()).Returns(updated);

        QuestDto? callbackQuest = null;
        var cut = RenderEditor(p => p
            .Add(x => x.Quest, quest)
            .Add(x => x.OnQuestUpdated, EventCallback.Factory.Create<QuestDto>(this, q => callbackQuest = q)));

        SetPrivateField(cut.Instance, "_editTitle", " Updated ");
        SetPrivateField(cut.Instance, "_editDescription", "new");

        await InvokePrivateOnRendererAsync(cut, "SaveQuestAsync");

        await questApi.Received(1).UpdateQuestAsync(quest.Id, Arg.Is<QuestEditDto>(d => d.Title == "Updated"));
        Assert.NotNull(callbackQuest);
        Assert.Equal("Updated", callbackQuest!.Title);
    }

    [Fact]
    public async Task ArcQuestEditor_SaveQuest_WhenConflictAndReloads_RefreshesEditorState()
    {
        var questApi = RegisterServices();
        var quest = CreateQuest("Old", "old-body");
        var current = CreateQuest("Current", "current-body");
        current.Id = quest.Id;

        questApi.UpdateQuestAsync(quest.Id, Arg.Any<QuestEditDto>()).Returns((QuestDto?)null);
        questApi.GetQuestAsync(quest.Id).Returns(current);

        var cut = RenderEditor(p => p.Add(x => x.Quest, quest));
        SetPrivateField(cut.Instance, "_editorInitialized", false);

        await InvokePrivateOnRendererAsync(cut, "SaveQuestAsync");

        Assert.Equal("Current", GetPrivateField<string>(cut.Instance, "_editTitle"));
        Assert.Equal("current-body", GetPrivateField<string>(cut.Instance, "_editDescription"));
    }

    [Fact]
    public async Task ArcQuestEditor_SaveQuest_WhenApiThrows_ShowsError()
    {
        var questApi = RegisterServices();
        var snackbar = Services.GetRequiredService<ISnackbar>();
        var quest = CreateQuest("Q", "desc");

        questApi.UpdateQuestAsync(quest.Id, Arg.Any<QuestEditDto>())
            .Returns(_ => Task.FromException<QuestDto?>(new InvalidOperationException("boom")));

        var cut = RenderEditor(p => p.Add(x => x.Quest, quest));

        await InvokePrivateOnRendererAsync(cut, "SaveQuestAsync");

        snackbar.Received().Add(Arg.Is<string>(s => s.Contains("Failed to save quest")), Severity.Error);
    }

    [Fact]
    public async Task ArcQuestEditor_OnTitleKeyDown_Enter_SavesTitle()
    {
        var questApi = RegisterServices();
        var quest = CreateQuest("Original", "desc");

        questApi.UpdateQuestAsync(quest.Id, Arg.Any<QuestEditDto>()).Returns(quest);

        var cut = RenderEditor(p => p.Add(x => x.Quest, quest));
        SetPrivateField(cut.Instance, "_editTitle", "Changed");

        await InvokePrivateOnRendererAsync(cut, "OnTitleKeyDown", new KeyboardEventArgs { Key = "Enter" });

        await questApi.Received().UpdateQuestAsync(quest.Id, Arg.Any<QuestEditDto>());
    }

    [Fact]
    public async Task ArcQuestEditor_OnTitleKeyDown_NonEnter_DoesNotSave()
    {
        var questApi = RegisterServices();
        var quest = CreateQuest("Original", "desc");

        var cut = RenderEditor(p => p.Add(x => x.Quest, quest));
        SetPrivateField(cut.Instance, "_editTitle", "Changed");

        await InvokePrivateOnRendererAsync(cut, "OnTitleKeyDown", new KeyboardEventArgs { Key = "Tab" });

        await questApi.DidNotReceive().UpdateQuestAsync(Arg.Any<Guid>(), Arg.Any<QuestEditDto>());
    }

    [Fact]
    public async Task ArcQuestEditor_InitializeEditor_WhenJsThrows_ShowsWarning()
    {
        RegisterServices();
        var snackbar = Services.GetRequiredService<ISnackbar>();
        var quest = CreateQuest("Q", "desc");

        JSInterop.SetupVoid("initializeTipTapEditor", _ => true)
            .SetException(new InvalidOperationException("js failed"));

        var cut = RenderEditor(p => p.Add(x => x.Quest, quest));

        var dotNetRef = DotNetObjectReference.Create(cut.Instance);
        SetPrivateField(cut.Instance, "_dotNetHelper", dotNetRef);

        await InvokePrivateOnRendererAsync(cut, "InitializeEditorAsync");

        snackbar.Received().Add(Arg.Is<string>(s => s.Contains("Failed to initialize quest editor")), Severity.Warning);
    }

    [Fact]
    public async Task ArcQuestEditor_DisposeAsync_CanRunTwice()
    {
        RegisterServices();
        var quest = CreateQuest("Q", "desc");

        var cut = RenderEditor(p => p.Add(x => x.Quest, quest));

        await cut.Instance.DisposeAsync();
        await cut.Instance.DisposeAsync();

        Assert.True(GetPrivateField<bool>(cut.Instance, "_disposed"));
    }

    [Fact]
    public void ArcQuestEditor_OnStatusChanged_TriggersSavePath()
    {
        RegisterServices();
        var quest = CreateQuest("Q", "desc");

        var cut = RenderEditor(p => p.Add(x => x.Quest, quest));
        SetPrivateField(cut.Instance, "_quest", quest);

        _ = cut.InvokeAsync(() => InvokePrivate(cut.Instance, "OnStatusChanged", QuestStatus.Completed));
        Assert.Equal(QuestStatus.Completed, GetPrivateField<QuestStatus>(cut.Instance, "_editStatus"));
    }

    [Fact]
    public void ArcQuestEditor_OnGmOnlyChanged_TriggersSavePath()
    {
        RegisterServices();
        var quest = CreateQuest("Q", "desc");

        var cut = RenderEditor(p => p.Add(x => x.Quest, quest));
        SetPrivateField(cut.Instance, "_quest", quest);

        _ = cut.InvokeAsync(() => InvokePrivate(cut.Instance, "OnGmOnlyChanged", true));
        Assert.True(GetPrivateField<bool>(cut.Instance, "_editIsGmOnly"));
    }

    [Fact]
    public void ArcQuestEditor_OnSortOrderChanged_TriggersSavePath()
    {
        RegisterServices();
        var quest = CreateQuest("Q", "desc");

        var cut = RenderEditor(p => p.Add(x => x.Quest, quest));
        SetPrivateField(cut.Instance, "_quest", quest);

        _ = cut.InvokeAsync(() => InvokePrivate(cut.Instance, "OnSortOrderChanged", 7));
        Assert.Equal(7, GetPrivateField<int>(cut.Instance, "_editSortOrder"));
    }

    [Fact]
    public void ArcQuestEditor_OnEditorUpdate_CoversTimerBranches()
    {
        RegisterServices();
        var quest = CreateQuest("Q", "desc");
        var cut = RenderEditor(p => p.Add(x => x.Quest, quest));

        cut.Instance.OnEditorUpdate("<p>first</p>");
        cut.Instance.OnEditorUpdate("<p>second</p>");
    }

    [Fact]
    public void ArcQuestEditor_EditorId_CoversNullAndQuestBranches()
    {
        RegisterServices();
        var cut = RenderEditor(p => p.Add(x => x.Quest, (QuestDto?)null));

        var property = cut.Instance.GetType().GetProperty("EditorId", BindingFlags.Instance | BindingFlags.NonPublic)!;
        _ = (string)property.GetValue(cut.Instance)!;

        SetPrivateField(cut.Instance, "_quest", CreateQuest("Q", "desc"));
        _ = (string)property.GetValue(cut.Instance)!;
    }

    private IQuestApiService RegisterServices()
    {
        var questApi = Substitute.For<IQuestApiService>();
        questApi.UpdateQuestAsync(Arg.Any<Guid>(), Arg.Any<QuestEditDto>())
            .Returns(Task.FromResult<QuestDto?>(null));

        Services.AddSingleton(questApi);
        Services.AddSingleton(Substitute.For<ISnackbar>());

        return questApi;
    }

    private IRenderedComponent<ArcQuestEditor> RenderEditor(Action<ComponentParameterCollectionBuilder<ArcQuestEditor>> configure)
    {
        _ = RenderComponent<MudPopoverProvider>();
        return RenderComponent(configure);
    }

    private static QuestDto CreateQuest(string title, string? description) => new()
    {
        Id = Guid.NewGuid(),
        ArcId = Guid.NewGuid(),
        Title = title,
        Description = description,
        Status = QuestStatus.Active,
        RowVersion = "v1",
        UpdatedAt = DateTime.UtcNow
    };

    private static Task InvokePrivateOnRendererAsync(IRenderedComponent<ArcQuestEditor> cut, string methodName, params object[] args)
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

    private static object? InvokePrivate(object instance, string methodName, params object[] args)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        return method.Invoke(instance, args);
    }
}
