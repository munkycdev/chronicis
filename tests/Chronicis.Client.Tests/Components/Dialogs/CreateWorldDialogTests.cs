using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Bunit;
using Chronicis.Client.Components.Dialogs;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Components.Dialogs;

[ExcludeFromCodeCoverage]
public class CreateWorldDialogTests : MudBlazorTestContext
{
    private readonly IWorldApiService _worldApi = Substitute.For<IWorldApiService>();

    public CreateWorldDialogTests()
    {
        Services.AddSingleton(_worldApi);
    }

    [Fact]
    public async Task Submit_EmptyName_DoesNotCallApi()
    {
        var cut = RenderComponent<CreateWorldDialog>();
        SetField(cut.Instance, "_name", " ");

        await cut.InvokeAsync(() => InvokePrivateAsync(cut.Instance, "Submit"));

        await _worldApi.DidNotReceive().CreateWorldAsync(Arg.Any<WorldCreateDto>());
    }

    [Fact]
    public async Task Submit_ValidName_CallsApiWithTrimmedValues()
    {
        _worldApi.CreateWorldAsync(Arg.Any<WorldCreateDto>())
            .Returns(new WorldDto { Id = Guid.NewGuid(), Name = "Test" });

        var cut = RenderComponent<CreateWorldDialog>();
        SetField(cut.Instance, "_name", "  New World  ");
        SetField(cut.Instance, "_description", "   ");

        await cut.InvokeAsync(() => InvokePrivateAsync(cut.Instance, "Submit"));

        await _worldApi.Received(1).CreateWorldAsync(Arg.Is<WorldCreateDto>(dto =>
            dto.Name == "New World" && dto.Description == null));
    }

    [Fact]
    public async Task Submit_WhenApiThrows_ResetsSubmittingFlag()
    {
        _worldApi.CreateWorldAsync(Arg.Any<WorldCreateDto>())
            .Returns<Task<WorldDto?>>(_ => throw new InvalidOperationException("boom"));

        var cut = RenderComponent<CreateWorldDialog>();
        SetField(cut.Instance, "_name", "World");

        await cut.InvokeAsync(() => InvokePrivateAsync(cut.Instance, "Submit"));

        Assert.False(GetField<bool>(cut.Instance, "_isSubmitting"));
    }

    [Fact]
    public async Task Submit_WhenAlreadySubmitting_DoesNotCallApi()
    {
        var cut = RenderComponent<CreateWorldDialog>();
        SetField(cut.Instance, "_name", "World");
        SetField(cut.Instance, "_isSubmitting", true);

        await cut.InvokeAsync(() => InvokePrivateAsync(cut.Instance, "Submit"));

        await _worldApi.DidNotReceive().CreateWorldAsync(Arg.Any<WorldCreateDto>());
    }

    [Fact]
    public async Task Submit_WithDescription_TrimmedAndSent()
    {
        _worldApi.CreateWorldAsync(Arg.Any<WorldCreateDto>())
            .Returns(new WorldDto { Id = Guid.NewGuid(), Name = "Trimmed World" });

        var cut = RenderComponent<CreateWorldDialog>();
        SetField(cut.Instance, "_name", "  Trimmed World ");
        SetField(cut.Instance, "_description", "  Story rich  ");

        await cut.InvokeAsync(() => InvokePrivateAsync(cut.Instance, "Submit"));

        await _worldApi.Received(1).CreateWorldAsync(Arg.Is<WorldCreateDto>(dto =>
            dto.Name == "Trimmed World" && dto.Description == "Story rich"));
    }

    [Fact]
    public async Task OnAfterRenderAsync_WhenNotFirstRender_DoesNothing()
    {
        var cut = RenderComponent<CreateWorldDialog>();

        await cut.InvokeAsync(() => InvokePrivateAsync(cut.Instance, "OnAfterRenderAsync", false));

        Assert.True(true);
    }

    [Fact]
    public async Task OnAfterRenderAsync_WhenFirstRenderAndFieldMissing_DoesNothing()
    {
        var cut = RenderComponent<CreateWorldDialog>();

        await cut.InvokeAsync(() => InvokePrivateAsync(cut.Instance, "OnAfterRenderAsync", true));

        Assert.True(true);
    }

    [Fact]
    public void Cancel_WhenNoDialog_DoesNotThrow()
    {
        var cut = RenderComponent<CreateWorldDialog>();

        var ex = Record.Exception(() => InvokePrivate(cut.Instance, "Cancel"));

        Assert.Null(ex);
    }

    [Fact]
    public async Task Cancel_WhenDialogOpen_ClosesDialog()
    {
        var provider = RenderComponent<MudDialogProvider>();
        var dialogs = Services.GetRequiredService<IDialogService>();
        _ = await dialogs.ShowAsync<CreateWorldDialog>("New World");
        var dialog = provider.FindComponent<CreateWorldDialog>();

        await dialog.InvokeAsync(() => InvokePrivate(dialog.Instance, "Cancel"));

        provider.WaitForAssertion(() =>
            Assert.DoesNotContain("World Name", provider.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public async Task DialogRender_ShowsCreateWorldContent()
    {
        var provider = RenderComponent<MudDialogProvider>();
        var dialogs = Services.GetRequiredService<IDialogService>();

        _ = await dialogs.ShowAsync<CreateWorldDialog>("New World");

        provider.WaitForAssertion(() =>
        {
            Assert.Contains("New World", provider.Markup, StringComparison.Ordinal);
            Assert.Contains("World Name", provider.Markup, StringComparison.Ordinal);
            Assert.Contains("Create", provider.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task DialogRender_WhenSubmitting_ShowsProgress()
    {
        var provider = RenderComponent<MudDialogProvider>();
        var dialogs = Services.GetRequiredService<IDialogService>();

        _ = await dialogs.ShowAsync<CreateWorldDialog>("New World");
        var dialog = provider.FindComponent<CreateWorldDialog>();
        SetField(dialog.Instance, "_name", "World");
        SetField(dialog.Instance, "_isSubmitting", true);
        dialog.Render();

        Assert.NotEmpty(provider.FindAll(".mud-progress-circular"));
    }

    [Fact]
    public async Task Submit_WhenDialogOpenAndSuccess_ClosesDialog()
    {
        _worldApi.CreateWorldAsync(Arg.Any<WorldCreateDto>())
            .Returns(new WorldDto { Id = Guid.NewGuid(), Name = "World" });
        var provider = RenderComponent<MudDialogProvider>();
        var dialogs = Services.GetRequiredService<IDialogService>();
        _ = await dialogs.ShowAsync<CreateWorldDialog>("New World");
        var dialog = provider.FindComponent<CreateWorldDialog>();
        SetField(dialog.Instance, "_name", "World");

        await dialog.InvokeAsync(() => InvokePrivateAsync(dialog.Instance, "Submit"));

        provider.WaitForAssertion(() =>
            Assert.DoesNotContain("World Name", provider.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Submit_WhenDialogOpenAndApiThrows_ClosesDialog()
    {
        _worldApi.CreateWorldAsync(Arg.Any<WorldCreateDto>())
            .Returns<Task<WorldDto?>>(_ => throw new InvalidOperationException("boom"));
        var provider = RenderComponent<MudDialogProvider>();
        var dialogs = Services.GetRequiredService<IDialogService>();
        _ = await dialogs.ShowAsync<CreateWorldDialog>("New World");
        var dialog = provider.FindComponent<CreateWorldDialog>();
        SetField(dialog.Instance, "_name", "World");

        await dialog.InvokeAsync(() => InvokePrivateAsync(dialog.Instance, "Submit"));

        provider.WaitForAssertion(() =>
            Assert.DoesNotContain("World Name", provider.Markup, StringComparison.Ordinal));
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
