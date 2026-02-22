using System.Reflection;
using Bunit;
using Chronicis.Client.Components.Admin;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Xunit;

namespace Chronicis.Client.Tests.Components.Admin;

public class DeleteWorldDialogTests : MudBlazorTestContext
{
    // Renders via IDialogService to pick up the MudDialog cascading parameter correctly
    private async Task<IRenderedComponent<DeleteWorldDialog>> RenderDialogAsync(string worldName = "My World")
    {
        var provider = RenderComponent<MudDialogProvider>();
        var dialogs = Services.GetRequiredService<IDialogService>();

        var parameters = new DialogParameters<DeleteWorldDialog>
        {
            { x => x.WorldName, worldName }
        };

        _ = await dialogs.ShowAsync<DeleteWorldDialog>($"Delete \"{worldName}\"", parameters);
        return provider.FindComponent<DeleteWorldDialog>();
    }

    [Fact]
    public async Task Dialog_ShowsWorldName_InConfirmationText()
    {
        var cut = await RenderDialogAsync("Forgotten Realms");

        Assert.Contains("Forgotten Realms", cut.Markup);
    }

    [Fact]
    public async Task Dialog_DeleteButton_DisabledByDefault()
    {
        var cut = await RenderDialogAsync("My World");

        var deleteBtn = cut.FindAll("button")
            .FirstOrDefault(b => b.TextContent.Contains("Delete Forever", StringComparison.OrdinalIgnoreCase));

        Assert.NotNull(deleteBtn);
        Assert.NotNull(deleteBtn!.GetAttribute("disabled"));
    }

    [Fact]
    public async Task Dialog_CanConfirm_TrueWhenConfirmTextMatchesExactly()
    {
        var cut = await RenderDialogAsync("My World");

        // Drive state via field reflection (MudBlazor input events are unreliable in bUnit)
        SetField(cut.Instance, "_confirmText", "My World");
        cut.Render();

        var canConfirm = GetProperty<bool>(cut.Instance, "CanConfirm");
        Assert.True(canConfirm);
    }

    [Fact]
    public async Task Dialog_CanConfirm_FalseForPartialInput()
    {
        var cut = await RenderDialogAsync("My World");

        SetField(cut.Instance, "_confirmText", "My");
        cut.Render();

        var canConfirm = GetProperty<bool>(cut.Instance, "CanConfirm");
        Assert.False(canConfirm);
    }

    [Fact]
    public async Task Dialog_Cancel_ClosesDialog()
    {
        var provider = RenderComponent<MudDialogProvider>();
        var dialogs = Services.GetRequiredService<IDialogService>();
        var parameters = new DialogParameters<DeleteWorldDialog>
        {
            { x => x.WorldName, "Test World" }
        };

        _ = await dialogs.ShowAsync<DeleteWorldDialog>("Delete", parameters);
        var dialog = provider.FindComponent<DeleteWorldDialog>();

        await dialog.InvokeAsync(() => InvokePrivate(dialog.Instance, "Cancel"));

        provider.WaitForAssertion(() =>
            Assert.DoesNotContain("Delete Forever", provider.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Dialog_Confirm_ClosesDialog_WhenConfirmTextMatches()
    {
        var provider = RenderComponent<MudDialogProvider>();
        var dialogs = Services.GetRequiredService<IDialogService>();
        var parameters = new DialogParameters<DeleteWorldDialog>
        {
            { x => x.WorldName, "Exact World" }
        };

        _ = await dialogs.ShowAsync<DeleteWorldDialog>("Delete", parameters);
        var dialog = provider.FindComponent<DeleteWorldDialog>();

        SetField(dialog.Instance, "_confirmText", "Exact World");

        await dialog.InvokeAsync(() => InvokePrivate(dialog.Instance, "Confirm"));

        provider.WaitForAssertion(() =>
            Assert.DoesNotContain("Delete Forever", provider.Markup, StringComparison.Ordinal));
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
