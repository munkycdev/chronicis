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
