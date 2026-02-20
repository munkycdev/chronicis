using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Bunit;
using Chronicis.Client.Components.Dialogs;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Components.Dialogs;

[ExcludeFromCodeCoverage]
public class CreateArcDialogTests : MudBlazorTestContext
{
    private readonly IArcApiService _arcApi = Substitute.For<IArcApiService>();

    public CreateArcDialogTests()
    {
        Services.AddSingleton(_arcApi);
    }

    [Fact]
    public async Task Submit_EmptyName_DoesNotCallApi()
    {
        var cut = RenderComponent<CreateArcDialog>(p => p.Add(x => x.CampaignId, Guid.NewGuid()));
        SetField(cut.Instance, "_name", " ");

        await cut.InvokeAsync(() => InvokePrivateAsync(cut.Instance, "Submit"));

        await _arcApi.DidNotReceive().CreateArcAsync(Arg.Any<ArcCreateDto>());
    }

    [Fact]
    public async Task OnNameKeyDown_Enter_Submits()
    {
        _arcApi.CreateArcAsync(Arg.Any<ArcCreateDto>()).Returns(new ArcDto { Id = Guid.NewGuid(), Name = "Act I" });
        var campaignId = Guid.NewGuid();

        var cut = RenderComponent<CreateArcDialog>(p => p.Add(x => x.CampaignId, campaignId));
        SetField(cut.Instance, "_name", "  Act I  ");
        SetField(cut.Instance, "_sortOrder", 3);

        await cut.InvokeAsync(() => InvokePrivateAsync(cut.Instance, "OnNameKeyDown", new KeyboardEventArgs { Key = "Enter" }));

        await _arcApi.Received(1).CreateArcAsync(Arg.Is<ArcCreateDto>(dto =>
            dto.Name == "Act I" && dto.CampaignId == campaignId && dto.SortOrder == 3));
    }

    [Fact]
    public async Task Submit_WhenApiThrows_ResetsSubmittingFlag()
    {
        _arcApi.CreateArcAsync(Arg.Any<ArcCreateDto>())
            .Returns<Task<ArcDto?>>(_ => throw new Exception("fail"));

        var cut = RenderComponent<CreateArcDialog>(p => p.Add(x => x.CampaignId, Guid.NewGuid()));
        SetField(cut.Instance, "_name", "Arc");

        await cut.InvokeAsync(() => InvokePrivateAsync(cut.Instance, "Submit"));

        Assert.False(GetField<bool>(cut.Instance, "_isSubmitting"));
    }

    [Fact]
    public async Task Submit_WhenAlreadySubmitting_DoesNotCallApi()
    {
        var cut = RenderComponent<CreateArcDialog>(p => p.Add(x => x.CampaignId, Guid.NewGuid()));
        SetField(cut.Instance, "_name", "Arc");
        SetField(cut.Instance, "_isSubmitting", true);

        await cut.InvokeAsync(() => InvokePrivateAsync(cut.Instance, "Submit"));

        await _arcApi.DidNotReceive().CreateArcAsync(Arg.Any<ArcCreateDto>());
    }

    [Fact]
    public async Task OnNameKeyDown_NonEnter_DoesNotSubmit()
    {
        var cut = RenderComponent<CreateArcDialog>(p => p.Add(x => x.CampaignId, Guid.NewGuid()));
        SetField(cut.Instance, "_name", "Arc");

        await cut.InvokeAsync(() => InvokePrivateAsync(cut.Instance, "OnNameKeyDown", new KeyboardEventArgs { Key = "a" }));

        await _arcApi.DidNotReceive().CreateArcAsync(Arg.Any<ArcCreateDto>());
    }

    [Fact]
    public async Task OnAfterRenderAsync_WhenNotFirstRender_DoesNothing()
    {
        var cut = RenderComponent<CreateArcDialog>(p => p.Add(x => x.CampaignId, Guid.NewGuid()));

        await cut.InvokeAsync(() => InvokePrivateAsync(cut.Instance, "OnAfterRenderAsync", false));

        Assert.True(true);
    }

    [Fact]
    public void Cancel_WhenNoDialog_DoesNotThrow()
    {
        var cut = RenderComponent<CreateArcDialog>(p => p.Add(x => x.CampaignId, Guid.NewGuid()));

        var ex = Record.Exception(() => InvokePrivate(cut.Instance, "Cancel"));

        Assert.Null(ex);
    }

    [Fact]
    public async Task Cancel_WhenDialogOpen_ClosesDialog()
    {
        var provider = RenderComponent<MudDialogProvider>();
        var dialogs = Services.GetRequiredService<IDialogService>();
        _ = await dialogs.ShowAsync<CreateArcDialog>("New Arc",
            new DialogParameters { ["CampaignId"] = Guid.NewGuid() });
        var dialog = provider.FindComponent<CreateArcDialog>();

        await dialog.InvokeAsync(() => InvokePrivate(dialog.Instance, "Cancel"));

        provider.WaitForAssertion(() =>
            Assert.DoesNotContain("Arc Name", provider.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public async Task OnNameKeyDown_WhenSubmitting_DoesNotSubmit()
    {
        var cut = RenderComponent<CreateArcDialog>(p => p.Add(x => x.CampaignId, Guid.NewGuid()));
        SetField(cut.Instance, "_name", "Arc");
        SetField(cut.Instance, "_isSubmitting", true);

        await cut.InvokeAsync(() => InvokePrivateAsync(cut.Instance, "OnNameKeyDown", new KeyboardEventArgs { Key = "Enter" }));

        await _arcApi.DidNotReceive().CreateArcAsync(Arg.Any<ArcCreateDto>());
    }

    [Fact]
    public async Task DialogRender_ShowsCreateArcContent()
    {
        var provider = RenderComponent<MudDialogProvider>();
        var dialogs = Services.GetRequiredService<IDialogService>();

        _ = await dialogs.ShowAsync<CreateArcDialog>("New Arc",
            new DialogParameters { ["CampaignId"] = Guid.NewGuid() });

        provider.WaitForAssertion(() =>
        {
            Assert.Contains("New Arc", provider.Markup, StringComparison.Ordinal);
            Assert.Contains("Arc Name", provider.Markup, StringComparison.Ordinal);
            Assert.Contains("Sort Order", provider.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task DialogRender_WhenSubmitting_ShowsProgress()
    {
        var provider = RenderComponent<MudDialogProvider>();
        var dialogs = Services.GetRequiredService<IDialogService>();

        _ = await dialogs.ShowAsync<CreateArcDialog>("New Arc",
            new DialogParameters { ["CampaignId"] = Guid.NewGuid() });
        var dialog = provider.FindComponent<CreateArcDialog>();
        SetField(dialog.Instance, "_name", "Arc");
        SetField(dialog.Instance, "_isSubmitting", true);
        dialog.Render();

        Assert.NotEmpty(provider.FindAll(".mud-progress-circular"));
    }

    [Fact]
    public async Task Submit_WhenDialogOpenAndSuccess_ClosesDialog()
    {
        _arcApi.CreateArcAsync(Arg.Any<ArcCreateDto>())
            .Returns(new ArcDto { Id = Guid.NewGuid(), Name = "Arc" });
        var provider = RenderComponent<MudDialogProvider>();
        var dialogs = Services.GetRequiredService<IDialogService>();
        _ = await dialogs.ShowAsync<CreateArcDialog>("New Arc",
            new DialogParameters { ["CampaignId"] = Guid.NewGuid() });
        var dialog = provider.FindComponent<CreateArcDialog>();
        SetField(dialog.Instance, "_name", "Arc");
        SetField(dialog.Instance, "_description", "desc");

        await dialog.InvokeAsync(() => InvokePrivateAsync(dialog.Instance, "Submit"));

        provider.WaitForAssertion(() =>
            Assert.DoesNotContain("Arc Name", provider.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Submit_WhenDialogOpenAndApiThrows_ClosesDialog()
    {
        _arcApi.CreateArcAsync(Arg.Any<ArcCreateDto>())
            .Returns<Task<ArcDto?>>(_ => throw new Exception("fail"));
        var provider = RenderComponent<MudDialogProvider>();
        var dialogs = Services.GetRequiredService<IDialogService>();
        _ = await dialogs.ShowAsync<CreateArcDialog>("New Arc",
            new DialogParameters { ["CampaignId"] = Guid.NewGuid() });
        var dialog = provider.FindComponent<CreateArcDialog>();
        SetField(dialog.Instance, "_name", "Arc");

        await dialog.InvokeAsync(() => InvokePrivateAsync(dialog.Instance, "Submit"));

        provider.WaitForAssertion(() =>
            Assert.DoesNotContain("Arc Name", provider.Markup, StringComparison.Ordinal));
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
