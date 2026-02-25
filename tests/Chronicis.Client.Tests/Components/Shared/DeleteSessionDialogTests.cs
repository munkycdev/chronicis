using System.Reflection;
using Bunit;
using Chronicis.Client.Components.Shared;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Xunit;

namespace Chronicis.Client.Tests.Components.Shared;

public class DeleteSessionDialogTests : MudBlazorTestContext
{
    private async Task<IRenderedComponent<DeleteSessionDialog>> RenderDialogAsync(string sessionName = "Session 12")
    {
        var provider = RenderComponent<MudDialogProvider>();
        var dialogs = Services.GetRequiredService<IDialogService>();

        var parameters = new DialogParameters<DeleteSessionDialog>
        {
            { x => x.SessionName, sessionName }
        };

        _ = await dialogs.ShowAsync<DeleteSessionDialog>("Delete Session", parameters);
        return provider.FindComponent<DeleteSessionDialog>();
    }

    [Fact]
    public async Task Dialog_ShowsSessionName()
    {
        var cut = await RenderDialogAsync("Dragonfall");
        Assert.Contains("Dragonfall", cut.Markup);
    }

    [Fact]
    public async Task Dialog_DeleteButton_DisabledByDefault()
    {
        var cut = await RenderDialogAsync();
        var deleteBtn = cut.FindAll("button")
            .FirstOrDefault(b => b.TextContent.Contains("Delete Forever", StringComparison.OrdinalIgnoreCase));

        Assert.NotNull(deleteBtn);
        Assert.NotNull(deleteBtn!.GetAttribute("disabled"));
    }

    [Fact]
    public async Task Dialog_CanConfirm_TrueOnlyOnExactMatch()
    {
        var cut = await RenderDialogAsync("Exact");

        SetField(cut.Instance, "_confirmText", "Exac");
        cut.Render();
        Assert.False(GetProperty<bool>(cut.Instance, "CanConfirm"));

        SetField(cut.Instance, "_confirmText", "Exact");
        cut.Render();
        Assert.True(GetProperty<bool>(cut.Instance, "CanConfirm"));
    }

    [Fact]
    public async Task Dialog_Cancel_ClosesDialog()
    {
        var provider = RenderComponent<MudDialogProvider>();
        var dialogs = Services.GetRequiredService<IDialogService>();
        _ = await dialogs.ShowAsync<DeleteSessionDialog>("Delete", new DialogParameters<DeleteSessionDialog> { { x => x.SessionName, "S1" } });
        var dialog = provider.FindComponent<DeleteSessionDialog>();

        await dialog.InvokeAsync(() => InvokePrivate(dialog.Instance, "Cancel"));

        provider.WaitForAssertion(() =>
            Assert.DoesNotContain("Delete Forever", provider.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Dialog_Confirm_ClosesDialog()
    {
        var provider = RenderComponent<MudDialogProvider>();
        var dialogs = Services.GetRequiredService<IDialogService>();
        _ = await dialogs.ShowAsync<DeleteSessionDialog>("Delete", new DialogParameters<DeleteSessionDialog> { { x => x.SessionName, "S2" } });
        var dialog = provider.FindComponent<DeleteSessionDialog>();
        SetField(dialog.Instance, "_confirmText", "S2");

        await dialog.InvokeAsync(() => InvokePrivate(dialog.Instance, "Confirm"));

        provider.WaitForAssertion(() =>
            Assert.DoesNotContain("Delete Forever", provider.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Dialog_CancelAndConfirm_WithoutMudDialog_DoNotThrow()
    {
        var cut = RenderComponent<DeleteSessionDialog>(p => p.Add(x => x.SessionName, "Standalone"));

        var cancelEx = await Record.ExceptionAsync(() => cut.InvokeAsync(() => InvokePrivate(cut.Instance, "Cancel")));
        var confirmEx = await Record.ExceptionAsync(() => cut.InvokeAsync(() => InvokePrivate(cut.Instance, "Confirm")));

        Assert.Null(cancelEx);
        Assert.Null(confirmEx);
    }

    private static void SetField(object target, string fieldName, object? value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field!.SetValue(target, value);
    }

    private static T GetProperty<T>(object target, string propName)
    {
        var prop = target.GetType().GetProperty(propName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(prop);
        return (T)prop!.GetValue(target)!;
    }

    private static object? InvokePrivate(object target, string methodName, params object[] args)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        return method!.Invoke(target, args);
    }
}
