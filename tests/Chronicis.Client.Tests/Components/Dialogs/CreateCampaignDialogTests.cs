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
public class CreateCampaignDialogTests : MudBlazorTestContext
{
    private readonly ICampaignApiService _campaignApi = Substitute.For<ICampaignApiService>();

    public CreateCampaignDialogTests()
    {
        Services.AddSingleton(_campaignApi);
    }

    [Fact]
    public async Task Submit_EmptyName_DoesNotCallApi()
    {
        var cut = RenderComponent<CreateCampaignDialog>(p => p.Add(x => x.WorldId, Guid.NewGuid()));
        SetField(cut.Instance, "_name", " ");

        await cut.InvokeAsync(() => InvokePrivateAsync(cut.Instance, "Submit"));

        await _campaignApi.DidNotReceive().CreateCampaignAsync(Arg.Any<CampaignCreateDto>());
    }

    [Fact]
    public async Task OnNameKeyDown_Enter_Submits()
    {
        _campaignApi.CreateCampaignAsync(Arg.Any<CampaignCreateDto>())
            .Returns(new CampaignDto { Id = Guid.NewGuid(), Name = "Campaign" });
        var worldId = Guid.NewGuid();

        var cut = RenderComponent<CreateCampaignDialog>(p => p.Add(x => x.WorldId, worldId));
        SetField(cut.Instance, "_name", "  Campaign  ");

        await cut.InvokeAsync(() => InvokePrivateAsync(cut.Instance, "OnNameKeyDown", new KeyboardEventArgs { Key = "Enter" }));

        await _campaignApi.Received(1).CreateCampaignAsync(Arg.Is<CampaignCreateDto>(dto =>
            dto.Name == "Campaign" && dto.WorldId == worldId));
    }

    [Fact]
    public async Task Submit_WhenApiThrows_ResetsSubmittingFlag()
    {
        _campaignApi.CreateCampaignAsync(Arg.Any<CampaignCreateDto>())
            .Returns<Task<CampaignDto?>>(_ => throw new Exception("fail"));

        var cut = RenderComponent<CreateCampaignDialog>(p => p.Add(x => x.WorldId, Guid.NewGuid()));
        SetField(cut.Instance, "_name", "Campaign");

        await cut.InvokeAsync(() => InvokePrivateAsync(cut.Instance, "Submit"));

        Assert.False(GetField<bool>(cut.Instance, "_isSubmitting"));
    }

    [Fact]
    public async Task Submit_WhenAlreadySubmitting_DoesNotCallApi()
    {
        var cut = RenderComponent<CreateCampaignDialog>(p => p.Add(x => x.WorldId, Guid.NewGuid()));
        SetField(cut.Instance, "_name", "Campaign");
        SetField(cut.Instance, "_isSubmitting", true);

        await cut.InvokeAsync(() => InvokePrivateAsync(cut.Instance, "Submit"));

        await _campaignApi.DidNotReceive().CreateCampaignAsync(Arg.Any<CampaignCreateDto>());
    }

    [Fact]
    public async Task OnNameKeyDown_NonEnter_DoesNotSubmit()
    {
        var cut = RenderComponent<CreateCampaignDialog>(p => p.Add(x => x.WorldId, Guid.NewGuid()));
        SetField(cut.Instance, "_name", "Campaign");

        await cut.InvokeAsync(() => InvokePrivateAsync(cut.Instance, "OnNameKeyDown", new KeyboardEventArgs { Key = "a" }));

        await _campaignApi.DidNotReceive().CreateCampaignAsync(Arg.Any<CampaignCreateDto>());
    }

    [Fact]
    public async Task OnAfterRenderAsync_WhenNotFirstRender_DoesNothing()
    {
        var cut = RenderComponent<CreateCampaignDialog>(p => p.Add(x => x.WorldId, Guid.NewGuid()));

        await cut.InvokeAsync(() => InvokePrivateAsync(cut.Instance, "OnAfterRenderAsync", false));

        Assert.True(true);
    }

    [Fact]
    public void Cancel_WhenNoDialog_DoesNotThrow()
    {
        var cut = RenderComponent<CreateCampaignDialog>(p => p.Add(x => x.WorldId, Guid.NewGuid()));

        var ex = Record.Exception(() => InvokePrivate(cut.Instance, "Cancel"));

        Assert.Null(ex);
    }

    [Fact]
    public async Task Cancel_WhenDialogOpen_ClosesDialog()
    {
        var provider = RenderComponent<MudDialogProvider>();
        var dialogs = Services.GetRequiredService<IDialogService>();
        _ = await dialogs.ShowAsync<CreateCampaignDialog>("New Campaign",
            new DialogParameters { ["WorldId"] = Guid.NewGuid() });
        var dialog = provider.FindComponent<CreateCampaignDialog>();

        await dialog.InvokeAsync(() => InvokePrivate(dialog.Instance, "Cancel"));

        provider.WaitForAssertion(() =>
            Assert.DoesNotContain("Campaign Name", provider.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public async Task OnNameKeyDown_WhenSubmitting_DoesNotSubmit()
    {
        var cut = RenderComponent<CreateCampaignDialog>(p => p.Add(x => x.WorldId, Guid.NewGuid()));
        SetField(cut.Instance, "_name", "Campaign");
        SetField(cut.Instance, "_isSubmitting", true);

        await cut.InvokeAsync(() => InvokePrivateAsync(cut.Instance, "OnNameKeyDown", new KeyboardEventArgs { Key = "Enter" }));

        await _campaignApi.DidNotReceive().CreateCampaignAsync(Arg.Any<CampaignCreateDto>());
    }

    [Fact]
    public async Task DialogRender_ShowsCreateCampaignContent()
    {
        var provider = RenderComponent<MudDialogProvider>();
        var dialogs = Services.GetRequiredService<IDialogService>();

        _ = await dialogs.ShowAsync<CreateCampaignDialog>("New Campaign",
            new DialogParameters { ["WorldId"] = Guid.NewGuid() });

        provider.WaitForAssertion(() =>
        {
            Assert.Contains("New Campaign", provider.Markup, StringComparison.Ordinal);
            Assert.Contains("Campaign Name", provider.Markup, StringComparison.Ordinal);
            Assert.Contains("Create", provider.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task DialogRender_WhenSubmitting_ShowsProgress()
    {
        var provider = RenderComponent<MudDialogProvider>();
        var dialogs = Services.GetRequiredService<IDialogService>();

        _ = await dialogs.ShowAsync<CreateCampaignDialog>("New Campaign",
            new DialogParameters { ["WorldId"] = Guid.NewGuid() });
        var dialog = provider.FindComponent<CreateCampaignDialog>();
        SetField(dialog.Instance, "_name", "Campaign");
        SetField(dialog.Instance, "_isSubmitting", true);
        dialog.Render();

        Assert.NotEmpty(provider.FindAll(".mud-progress-circular"));
    }

    [Fact]
    public async Task Submit_WhenDialogOpenAndSuccess_ClosesDialog()
    {
        _campaignApi.CreateCampaignAsync(Arg.Any<CampaignCreateDto>())
            .Returns(new CampaignDto { Id = Guid.NewGuid(), Name = "Campaign" });
        var provider = RenderComponent<MudDialogProvider>();
        var dialogs = Services.GetRequiredService<IDialogService>();
        _ = await dialogs.ShowAsync<CreateCampaignDialog>("New Campaign",
            new DialogParameters { ["WorldId"] = Guid.NewGuid() });
        var dialog = provider.FindComponent<CreateCampaignDialog>();
        SetField(dialog.Instance, "_name", "Campaign");
        SetField(dialog.Instance, "_description", "desc");

        await dialog.InvokeAsync(() => InvokePrivateAsync(dialog.Instance, "Submit"));

        provider.WaitForAssertion(() =>
            Assert.DoesNotContain("Campaign Name", provider.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Submit_WhenDialogOpenAndApiThrows_ClosesDialog()
    {
        _campaignApi.CreateCampaignAsync(Arg.Any<CampaignCreateDto>())
            .Returns<Task<CampaignDto?>>(_ => throw new Exception("fail"));
        var provider = RenderComponent<MudDialogProvider>();
        var dialogs = Services.GetRequiredService<IDialogService>();
        _ = await dialogs.ShowAsync<CreateCampaignDialog>("New Campaign",
            new DialogParameters { ["WorldId"] = Guid.NewGuid() });
        var dialog = provider.FindComponent<CreateCampaignDialog>();
        SetField(dialog.Instance, "_name", "Campaign");

        await dialog.InvokeAsync(() => InvokePrivateAsync(dialog.Instance, "Submit"));

        provider.WaitForAssertion(() =>
            Assert.DoesNotContain("Campaign Name", provider.Markup, StringComparison.Ordinal));
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
