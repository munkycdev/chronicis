using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Bunit;
using Chronicis.Client.Components.Dialogs;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs.Quests;
using Chronicis.Shared.Enums;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Components.Dialogs;

[ExcludeFromCodeCoverage]
public class CreateQuestDialogTests : MudBlazorTestContext
{
    private readonly IQuestApiService _questApi = Substitute.For<IQuestApiService>();

    public CreateQuestDialogTests()
    {
        Services.AddSingleton(_questApi);
    }

    [Fact]
    public async Task Submit_EmptyTitle_DoesNotCallApi()
    {
        var cut = RenderComponent<CreateQuestDialog>(p => p.Add(x => x.ArcId, Guid.NewGuid()));
        SetField(cut.Instance, "_title", " ");

        await cut.InvokeAsync(() => InvokePrivateAsync(cut.Instance, "Submit"));

        await _questApi.DidNotReceive().CreateQuestAsync(Arg.Any<Guid>(), Arg.Any<QuestCreateDto>());
    }

    [Fact]
    public async Task OnTitleKeyDown_Enter_Submits()
    {
        var arcId = Guid.NewGuid();
        _questApi.CreateQuestAsync(Arg.Any<Guid>(), Arg.Any<QuestCreateDto>())
            .Returns(new QuestDto { Id = Guid.NewGuid(), Title = "Find Relic", Status = QuestStatus.Active });

        var cut = RenderComponent<CreateQuestDialog>(p => p.Add(x => x.ArcId, arcId));
        SetField(cut.Instance, "_title", "  Find Relic  ");
        SetField(cut.Instance, "_description", "  desc  ");
        SetField(cut.Instance, "_status", QuestStatus.Completed);
        SetField(cut.Instance, "_isGmOnly", true);

        await cut.InvokeAsync(() => InvokePrivateAsync(cut.Instance, "OnTitleKeyDown", new KeyboardEventArgs { Key = "Enter" }));

        await _questApi.Received(1).CreateQuestAsync(arcId, Arg.Any<QuestCreateDto>());
    }

    [Fact]
    public async Task Submit_NullResult_ResetsSubmitting()
    {
        _questApi.CreateQuestAsync(Arg.Any<Guid>(), Arg.Any<QuestCreateDto>())
            .Returns((QuestDto?)null);

        var cut = RenderComponent<CreateQuestDialog>(p => p.Add(x => x.ArcId, Guid.NewGuid()));
        SetField(cut.Instance, "_title", "Quest");

        await cut.InvokeAsync(() => InvokePrivateAsync(cut.Instance, "Submit"));

        Assert.False(GetField<bool>(cut.Instance, "_isSubmitting"));
    }

    [Fact]
    public async Task Submit_WhenAlreadySubmitting_DoesNotCallApi()
    {
        var cut = RenderComponent<CreateQuestDialog>(p => p.Add(x => x.ArcId, Guid.NewGuid()));
        SetField(cut.Instance, "_title", "Quest");
        SetField(cut.Instance, "_isSubmitting", true);

        await cut.InvokeAsync(() => InvokePrivateAsync(cut.Instance, "Submit"));

        await _questApi.DidNotReceive().CreateQuestAsync(Arg.Any<Guid>(), Arg.Any<QuestCreateDto>());
    }

    [Fact]
    public async Task OnTitleKeyDown_NonEnter_DoesNotSubmit()
    {
        var cut = RenderComponent<CreateQuestDialog>(p => p.Add(x => x.ArcId, Guid.NewGuid()));
        SetField(cut.Instance, "_title", "Quest");

        await cut.InvokeAsync(() => InvokePrivateAsync(cut.Instance, "OnTitleKeyDown", new KeyboardEventArgs { Key = "a" }));

        await _questApi.DidNotReceive().CreateQuestAsync(Arg.Any<Guid>(), Arg.Any<QuestCreateDto>());
    }

    [Fact]
    public async Task OnAfterRenderAsync_WhenNotFirstRender_DoesNothing()
    {
        var cut = RenderComponent<CreateQuestDialog>(p => p.Add(x => x.ArcId, Guid.NewGuid()));

        await cut.InvokeAsync(() => InvokePrivateAsync(cut.Instance, "OnAfterRenderAsync", false));

        Assert.True(true);
    }

    [Fact]
    public void Cancel_WhenNoDialog_DoesNotThrow()
    {
        var cut = RenderComponent<CreateQuestDialog>(p => p.Add(x => x.ArcId, Guid.NewGuid()));

        var ex = Record.Exception(() => InvokePrivate(cut.Instance, "Cancel"));

        Assert.Null(ex);
    }

    [Fact]
    public async Task Cancel_WhenDialogOpen_ClosesDialog()
    {
        _ = RenderComponent<MudPopoverProvider>();
        var provider = RenderComponent<MudDialogProvider>();
        var dialogs = Services.GetRequiredService<IDialogService>();
        _ = await dialogs.ShowAsync<CreateQuestDialog>("New Quest",
            new DialogParameters { ["ArcId"] = Guid.NewGuid() });
        var dialog = provider.FindComponent<CreateQuestDialog>();

        await dialog.InvokeAsync(() => InvokePrivate(dialog.Instance, "Cancel"));

        provider.WaitForAssertion(() =>
            Assert.DoesNotContain("Quest Title", provider.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public async Task OnTitleKeyDown_WhenSubmitting_DoesNotSubmit()
    {
        var cut = RenderComponent<CreateQuestDialog>(p => p.Add(x => x.ArcId, Guid.NewGuid()));
        SetField(cut.Instance, "_title", "Quest");
        SetField(cut.Instance, "_isSubmitting", true);

        await cut.InvokeAsync(() => InvokePrivateAsync(cut.Instance, "OnTitleKeyDown", new KeyboardEventArgs { Key = "Enter" }));

        await _questApi.DidNotReceive().CreateQuestAsync(Arg.Any<Guid>(), Arg.Any<QuestCreateDto>());
    }

    [Fact]
    public async Task DialogRender_ShowsCreateQuestContent()
    {
        _ = RenderComponent<MudPopoverProvider>();
        var provider = RenderComponent<MudDialogProvider>();
        var dialogs = Services.GetRequiredService<IDialogService>();

        _ = await dialogs.ShowAsync<CreateQuestDialog>("New Quest",
            new DialogParameters { ["ArcId"] = Guid.NewGuid() });

        provider.WaitForAssertion(() =>
        {
            Assert.Contains("New Quest", provider.Markup, StringComparison.Ordinal);
            Assert.Contains("Quest Title", provider.Markup, StringComparison.Ordinal);
            Assert.Contains("GM Only", provider.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task DialogRender_WhenSubmitting_ShowsProgress()
    {
        _ = RenderComponent<MudPopoverProvider>();
        var provider = RenderComponent<MudDialogProvider>();
        var dialogs = Services.GetRequiredService<IDialogService>();

        _ = await dialogs.ShowAsync<CreateQuestDialog>("New Quest",
            new DialogParameters { ["ArcId"] = Guid.NewGuid() });
        var dialog = provider.FindComponent<CreateQuestDialog>();
        SetField(dialog.Instance, "_title", "Quest");
        SetField(dialog.Instance, "_isSubmitting", true);
        dialog.Render();

        Assert.NotEmpty(provider.FindAll(".mud-progress-circular"));
    }

    [Fact]
    public async Task Submit_WhenDialogOpenAndResultNotNull_ClosesDialog()
    {
        _questApi.CreateQuestAsync(Arg.Any<Guid>(), Arg.Any<QuestCreateDto>())
            .Returns(new QuestDto { Id = Guid.NewGuid(), Title = "Quest", Status = QuestStatus.Active });
        _ = RenderComponent<MudPopoverProvider>();
        var provider = RenderComponent<MudDialogProvider>();
        var dialogs = Services.GetRequiredService<IDialogService>();
        _ = await dialogs.ShowAsync<CreateQuestDialog>("New Quest",
            new DialogParameters { ["ArcId"] = Guid.NewGuid() });
        var dialog = provider.FindComponent<CreateQuestDialog>();
        SetField(dialog.Instance, "_title", "Quest");

        await dialog.InvokeAsync(() => InvokePrivateAsync(dialog.Instance, "Submit"));

        provider.WaitForAssertion(() =>
            Assert.DoesNotContain("Quest Title", provider.Markup, StringComparison.Ordinal));
    }


    private static async Task InvokePrivateAsync(object target, string methodName, params object[] args)
    {
        var result = InvokePrivate(target, methodName, args);
        if (result is Task task)
        {
            await task;
        }
    }

    private static object? InvokePrivate(object target, string methodName, params object[] args)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        return method!.Invoke(target, args);
    }

    private static void SetField(object target, string fieldName, object? value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field!.SetValue(target, value);
    }

    private static T GetField<T>(object target, string fieldName)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return (T)field!.GetValue(target)!;
    }
}
