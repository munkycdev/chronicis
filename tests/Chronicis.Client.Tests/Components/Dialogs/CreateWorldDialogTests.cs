using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Bunit;
using Chronicis.Client.Components.Dialogs;
using Chronicis.Client.Services;
using Chronicis.Shared.DTOs;
using Microsoft.Extensions.DependencyInjection;
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
