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
public class JoinWorldDialogTests : MudBlazorTestContext
{
    private readonly IWorldApiService _worldApi = Substitute.For<IWorldApiService>();

    public JoinWorldDialogTests()
    {
        Services.AddSingleton(_worldApi);
    }

    [Fact]
    public async Task Submit_EmptyCode_DoesNotCallApi()
    {
        var cut = RenderComponent<JoinWorldDialog>();
        SetField(cut.Instance, "_code", " ");

        await cut.InvokeAsync(() => InvokePrivateAsync(cut.Instance, "Submit"));

        await _worldApi.DidNotReceive().JoinWorldAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task Submit_SuccessResult_ClearsError()
    {
        _worldApi.JoinWorldAsync(Arg.Any<string>()).Returns(new WorldJoinResultDto
        {
            Success = true,
            WorldName = "Realm"
        });
        var cut = RenderComponent<JoinWorldDialog>();
        SetField(cut.Instance, "_code", "  CODE-123  ");

        await cut.InvokeAsync(() => InvokePrivateAsync(cut.Instance, "Submit"));

        await _worldApi.Received(1).JoinWorldAsync("CODE-123");
        Assert.Null(GetField<string?>(cut.Instance, "_error"));
        Assert.False(GetField<bool>(cut.Instance, "_isSubmitting"));
    }

    [Fact]
    public async Task Submit_FailedResult_SetsErrorMessage()
    {
        _worldApi.JoinWorldAsync(Arg.Any<string>()).Returns(new WorldJoinResultDto
        {
            Success = false,
            ErrorMessage = "Invalid code"
        });
        var cut = RenderComponent<JoinWorldDialog>();
        SetField(cut.Instance, "_code", "bad");

        await cut.InvokeAsync(() => InvokePrivateAsync(cut.Instance, "Submit"));

        Assert.Equal("Invalid code", GetField<string?>(cut.Instance, "_error"));
    }

    [Fact]
    public async Task Submit_NullResult_SetsDefaultError()
    {
        _worldApi.JoinWorldAsync(Arg.Any<string>()).Returns((WorldJoinResultDto?)null);
        var cut = RenderComponent<JoinWorldDialog>();
        SetField(cut.Instance, "_code", "bad");

        await cut.InvokeAsync(() => InvokePrivateAsync(cut.Instance, "Submit"));

        Assert.Equal("Failed to join world. Please try again.", GetField<string?>(cut.Instance, "_error"));
    }

    [Fact]
    public async Task Submit_WhenApiThrows_SetsErrorPrefix()
    {
        _worldApi.JoinWorldAsync(Arg.Any<string>())
            .Returns<Task<WorldJoinResultDto?>>(_ => throw new InvalidOperationException("boom"));
        var cut = RenderComponent<JoinWorldDialog>();
        SetField(cut.Instance, "_code", "bad");

        await cut.InvokeAsync(() => InvokePrivateAsync(cut.Instance, "Submit"));

        var error = GetField<string?>(cut.Instance, "_error");
        Assert.NotNull(error);
        Assert.StartsWith("Error:", error, StringComparison.Ordinal);
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
