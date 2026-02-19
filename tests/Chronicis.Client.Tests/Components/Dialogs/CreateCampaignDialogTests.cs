using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Bunit;
using Chronicis.Client.Components.Dialogs;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
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

    private static async Task InvokePrivateAsync(object target, string methodName, params object[] args)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        var result = method!.Invoke(target, args);
        if (result is Task task)
        {
            await task;
        }
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
