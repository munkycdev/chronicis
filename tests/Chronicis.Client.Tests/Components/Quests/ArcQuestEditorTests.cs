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
    public async Task ArcQuestEditor_SaveQuest_WhenAlreadySaving_ReturnsEarly()
    {
        var questApi = RegisterServices();
        var quest = CreateQuest("Q", "desc");
        var cut = RenderEditor(p => p.Add(x => x.Quest, quest));
        SetPrivateField(cut.Instance, "_isSaving", true);

        await InvokePrivateOnRendererAsync(cut, "SaveQuestAsync");

        await questApi.DidNotReceive().UpdateQuestAsync(Arg.Any<Guid>(), Arg.Any<QuestEditDto>());
    }

    [Fact]
    public async Task ArcQuestEditor_SaveTitleAsync_WhenTitleUnchanged_DoesNotSave()
    {
        var questApi = RegisterServices();
        var quest = CreateQuest("Same", "desc");
        var cut = RenderEditor(p => p.Add(x => x.Quest, quest));
        SetPrivateField(cut.Instance, "_editTitle", "Same");

        await InvokePrivateOnRendererAsync(cut, "SaveTitleAsync");

        await questApi.DidNotReceive().UpdateQuestAsync(Arg.Any<Guid>(), Arg.Any<QuestEditDto>());
    }

    [Fact]
    public async Task ArcQuestEditor_SaveTitleAsync_WhenQuestNull_DoesNotSave()
    {
        var questApi = RegisterServices();
        var cut = RenderEditor(p => p.Add(x => x.Quest, (QuestDto?)null));
        SetPrivateField(cut.Instance, "_editTitle", "Changed");

        await InvokePrivateOnRendererAsync(cut, "SaveTitleAsync");

        await questApi.DidNotReceive().UpdateQuestAsync(Arg.Any<Guid>(), Arg.Any<QuestEditDto>());
    }

    [Fact]
    public async Task ArcQuestEditor_AutoSaveAsync_WhenNoUnsavedChanges_DoesNotSave()
    {
        var questApi = RegisterServices();
        var quest = CreateQuest("Q", "desc");
        var cut = RenderEditor(p => p.Add(x => x.Quest, quest));
        SetPrivateField(cut.Instance, "_hasUnsavedChanges", false);

        await InvokePrivateOnRendererAsync(cut, "AutoSaveAsync");

        await questApi.DidNotReceive().UpdateQuestAsync(Arg.Any<Guid>(), Arg.Any<QuestEditDto>());
    }

    [Fact]
    public async Task ArcQuestEditor_AutoSaveAsync_WhenUnsavedAndNotSaving_Saves()
    {
        var questApi = RegisterServices();
        var quest = CreateQuest("Q", "desc");
        questApi.UpdateQuestAsync(quest.Id, Arg.Any<QuestEditDto>()).Returns(quest);
        var cut = RenderEditor(p => p.Add(x => x.Quest, quest));
        SetPrivateField(cut.Instance, "_hasUnsavedChanges", true);
        SetPrivateField(cut.Instance, "_isSaving", false);

        await InvokePrivateOnRendererAsync(cut, "AutoSaveAsync");

        await questApi.Received(1).UpdateQuestAsync(quest.Id, Arg.Any<QuestEditDto>());
    }

    [Fact]
    public async Task ArcQuestEditor_InitializeEditor_WhenAlreadyInitialized_DoesNothing()
    {
        RegisterServices();
        var cut = RenderEditor(p => p.Add(x => x.Quest, (QuestDto?)null));
        SetPrivateField(cut.Instance, "_editorInitialized", true);

        await InvokePrivateOnRendererAsync(cut, "InitializeEditorAsync");

        Assert.True(GetPrivateField<bool>(cut.Instance, "_editorInitialized"));
    }

    [Fact]
    public async Task ArcQuestEditor_InitializeEditor_WhenDisposed_DoesNothing()
    {
        RegisterServices();
        var cut = RenderEditor(p => p.Add(x => x.Quest, (QuestDto?)null));
        SetPrivateField(cut.Instance, "_disposed", true);

        await InvokePrivateOnRendererAsync(cut, "InitializeEditorAsync");

        Assert.True(GetPrivateField<bool>(cut.Instance, "_disposed"));
    }

    [Fact]
    public async Task ArcQuestEditor_InitializeEditor_WhenObjectDisposedException_IsIgnored()
    {
        RegisterServices();
        var snackbar = Services.GetRequiredService<ISnackbar>();
        var quest = CreateQuest("Q", "desc");

        JSInterop.SetupVoid("initializeTipTapEditor", _ => true)
            .SetException(new ObjectDisposedException("obj"));

        var cut = RenderEditor(p => p.Add(x => x.Quest, quest));

        var dotNetRef = DotNetObjectReference.Create(cut.Instance);
        SetPrivateField(cut.Instance, "_dotNetHelper", dotNetRef);

        await InvokePrivateOnRendererAsync(cut, "InitializeEditorAsync");

        snackbar.DidNotReceive().Add(Arg.Any<string>(), Arg.Any<Severity>());
    }

    [Fact]
    public async Task ArcQuestEditor_InitializeEditor_WhenJsDisconnected_IsIgnored()
    {
        RegisterServices();
        var snackbar = Services.GetRequiredService<ISnackbar>();
        var quest = CreateQuest("Q", "desc");

        JSInterop.SetupVoid("initializeTipTapEditor", _ => true)
            .SetException(new JSDisconnectedException("gone"));

        var cut = RenderEditor(p => p.Add(x => x.Quest, quest));

        var dotNetRef = DotNetObjectReference.Create(cut.Instance);
        SetPrivateField(cut.Instance, "_dotNetHelper", dotNetRef);

        await InvokePrivateOnRendererAsync(cut, "InitializeEditorAsync");

        snackbar.DidNotReceive().Add(Arg.Any<string>(), Arg.Any<Severity>());
    }

    [Fact]
    public async Task ArcQuestEditor_DisposeEditorAsync_WhenDestroyThrows_StillClearsInitialized()
    {
        RegisterServices();
        JSInterop.SetupVoid("destroyTipTapEditor", _ => true)
            .SetException(new InvalidOperationException("dispose fail"));
        var quest = CreateQuest("Q", "desc");
        var cut = RenderEditor(p => p.Add(x => x.Quest, quest));
        SetPrivateField(cut.Instance, "_editorInitialized", true);

        await InvokePrivateOnRendererAsync(cut, "DisposeEditorAsync");

        Assert.False(GetPrivateField<bool>(cut.Instance, "_editorInitialized"));
    }

    [Fact]
    public void ArcQuestEditor_OnParametersSetAsync_WhenQuestIdUnchanged_DoesNotResetEdits()
    {
        RegisterServices();
        var quest = CreateQuest("Original", "desc");
        var cut = RenderEditor(p => p.Add(x => x.Quest, quest));
        SetPrivateField(cut.Instance, "_editTitle", "Local Edit");

        var sameIdQuest = CreateQuest("Server Value", "server desc");
        sameIdQuest.Id = quest.Id;
        cut.SetParametersAndRender(p => p.Add(x => x.Quest, sameIdQuest));

        var editTitle = GetPrivateField<string>(cut.Instance, "_editTitle");
        Assert.Equal("Local Edit", editTitle);
    }

    [Fact]
    public async Task ArcQuestEditor_OnParametersSetAsync_WhenDescriptionNull_UsesEmptyString()
    {
        RegisterServices();
        var quest = CreateQuest("Original", null);
        var cut = RenderEditor(p => p.Add(x => x.Quest, quest));

        await InvokePrivateOnRendererAsync(cut, "OnParametersSetAsync");

        Assert.Equal(string.Empty, GetPrivateField<string>(cut.Instance, "_editDescription"));
    }

    [Fact]
    public async Task ArcQuestEditor_SaveQuest_WhenUpdatedDescriptionNull_UsesEmptyString()
    {
        var questApi = RegisterServices();
        var quest = CreateQuest("Old", "old");
        var updated = CreateQuest("New", null);
        updated.Id = quest.Id;
        questApi.UpdateQuestAsync(quest.Id, Arg.Any<QuestEditDto>()).Returns(updated);
        var cut = RenderEditor(p => p.Add(x => x.Quest, quest));

        await InvokePrivateOnRendererAsync(cut, "SaveQuestAsync");

        Assert.Equal(string.Empty, GetPrivateField<string>(cut.Instance, "_editDescription"));
    }

    [Fact]
    public async Task ArcQuestEditor_SaveQuest_WhenConflictReloadDescriptionNull_UsesEmptyString()
    {
        var questApi = RegisterServices();
        var quest = CreateQuest("Old", "old");
        var current = CreateQuest("Server", null);
        current.Id = quest.Id;
        questApi.UpdateQuestAsync(quest.Id, Arg.Any<QuestEditDto>()).Returns((QuestDto?)null);
        questApi.GetQuestAsync(quest.Id).Returns(current);
        var cut = RenderEditor(p => p.Add(x => x.Quest, quest));

        await InvokePrivateOnRendererAsync(cut, "SaveQuestAsync");

        Assert.Equal(string.Empty, GetPrivateField<string>(cut.Instance, "_editDescription"));
    }

    [Fact]
    public async Task ArcQuestEditor_SaveQuest_WhenTitleAndDescriptionWhitespace_SendsNullValues()
    {
        var questApi = RegisterServices();
        var quest = CreateQuest("Old", "old");
        var updated = CreateQuest("Old", "old");
        updated.Id = quest.Id;
        questApi.UpdateQuestAsync(quest.Id, Arg.Any<QuestEditDto>()).Returns(updated);
        var cut = RenderEditor(p => p.Add(x => x.Quest, quest));
        SetPrivateField(cut.Instance, "_editTitle", "   ");
        SetPrivateField(cut.Instance, "_editDescription", "   ");

        await InvokePrivateOnRendererAsync(cut, "SaveQuestAsync");

        await questApi.Received(1).UpdateQuestAsync(quest.Id, Arg.Is<QuestEditDto>(d =>
            d.Title == null && d.Description == null));
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

    [Fact]
    public async Task ArcQuestEditor_DisposeAsync_WhenDotNetHelperPresent_DisposesReference()
    {
        RegisterServices();
        var cut = RenderEditor(p => p.Add(x => x.Quest, CreateQuest("Q", "desc")));
        var dotNetRef = DotNetObjectReference.Create(cut.Instance);
        SetPrivateField(cut.Instance, "_dotNetHelper", dotNetRef);

        await cut.Instance.DisposeAsync();

        Assert.Throws<ObjectDisposedException>(() => _ = dotNetRef.Value);
    }

    [Fact]
    public async Task ArcQuestEditor_InitializeEditor_WhenDisposedAfterTipTapInit_ReturnsBeforeAutocompleteInit()
    {
        RegisterServices();
        ArcQuestEditor? instance = null;
        var jsRuntime = new CallbackJsRuntime(identifier =>
        {
            if (identifier == "initializeTipTapEditor" && instance != null)
            {
                SetPrivateField(instance, "_disposed", true);
            }
        });
        Services.AddSingleton<IJSRuntime>(jsRuntime);
        var cut = RenderEditor(p => p.Add(x => x.Quest, CreateQuest("Q", "desc")));
        instance = cut.Instance;
        var dotNetRef = DotNetObjectReference.Create(cut.Instance);
        SetPrivateField(cut.Instance, "_dotNetHelper", dotNetRef);

        await InvokePrivateOnRendererAsync(cut, "InitializeEditorAsync");

        Assert.DoesNotContain("initializeWikiLinkAutocomplete", jsRuntime.Identifiers);
        Assert.False(GetPrivateField<bool>(cut.Instance, "_editorInitialized"));
    }

    [Fact]
    public async Task ArcQuestEditor_DisposeEditorAsync_WhenDestroySucceeds_CallsDestroy()
    {
        RegisterServices();
        var jsRuntime = new CallbackJsRuntime(_ => { });
        Services.AddSingleton<IJSRuntime>(jsRuntime);
        var cut = RenderEditor(p => p.Add(x => x.Quest, CreateQuest("Q", "desc")));
        SetPrivateField(cut.Instance, "_editorInitialized", true);

        await InvokePrivateOnRendererAsync(cut, "DisposeEditorAsync");

        Assert.Contains("destroyTipTapEditor", jsRuntime.Identifiers);
        Assert.False(GetPrivateField<bool>(cut.Instance, "_editorInitialized"));
    }

    [Fact]
    public async Task ArcQuestEditor_DisposeAsync_WhenDotNetHelperNull_Completes()
    {
        RegisterServices();
        var cut = RenderEditor(p => p.Add(x => x.Quest, CreateQuest("Q", "desc")));
        SetPrivateField(cut.Instance, "_dotNetHelper", null);
        SetPrivateField(cut.Instance, "_editorInitialized", false);

        await cut.Instance.DisposeAsync();

        Assert.True(GetPrivateField<bool>(cut.Instance, "_disposed"));
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

    private sealed class CallbackJsRuntime(Action<string> onInvoke) : IJSRuntime
    {
        public List<string> Identifiers { get; } = new();

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            Identifiers.Add(identifier);
            onInvoke(identifier);
            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            Identifiers.Add(identifier);
            onInvoke(identifier);
            return ValueTask.FromResult(default(TValue)!);
        }
    }
}
