using System.Reflection;
using Bunit;
using Chronicis.Client.Components.Shared;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Xunit;

namespace Chronicis.Client.Tests.Components.Shared;

public class DeleteMapDialogTests : MudBlazorTestContext
{
    private async Task<IRenderedComponent<DeleteMapDialog>> RenderDialogAsync(string mapName = "Davokar")
    {
        var provider = RenderComponent<MudDialogProvider>();
        var dialogs = Services.GetRequiredService<IDialogService>();

        var parameters = new DialogParameters<DeleteMapDialog>
        {
            { x => x.MapName, mapName }
        };

        _ = await dialogs.ShowAsync<DeleteMapDialog>("Delete Map", parameters);
        return provider.FindComponent<DeleteMapDialog>();
    }

    [Fact]
    public async Task Dialog_ShowsMapName()
    {
        var cut = await RenderDialogAsync("Davokar");
        Assert.Contains("Davokar", cut.Markup);
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
        _ = await dialogs.ShowAsync<DeleteMapDialog>("Delete", new DialogParameters<DeleteMapDialog> { { x => x.MapName, "M1" } });
        var dialog = provider.FindComponent<DeleteMapDialog>();

        await dialog.InvokeAsync(() => InvokePrivate(dialog.Instance, "Cancel"));

        provider.WaitForAssertion(() =>
            Assert.DoesNotContain("Delete Forever", provider.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Dialog_Confirm_ClosesDialog()
    {
        var provider = RenderComponent<MudDialogProvider>();
        var dialogs = Services.GetRequiredService<IDialogService>();
        _ = await dialogs.ShowAsync<DeleteMapDialog>("Delete", new DialogParameters<DeleteMapDialog> { { x => x.MapName, "M2" } });
        var dialog = provider.FindComponent<DeleteMapDialog>();
        SetField(dialog.Instance, "_confirmText", "M2");

        await dialog.InvokeAsync(() => InvokePrivate(dialog.Instance, "Confirm"));

        provider.WaitForAssertion(() =>
            Assert.DoesNotContain("Delete Forever", provider.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Dialog_CancelAndConfirm_WithoutMudDialog_DoNotThrow()
    {
        var cut = RenderComponent<DeleteMapDialog>(p => p.Add(x => x.MapName, "Standalone"));

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
