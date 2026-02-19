using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Bunit;
using Chronicis.Client.Components;
using Chronicis.Client.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Components;

[ExcludeFromCodeCoverage]
public class SearchBoxTests : MudBlazorTestContext
{
    private readonly ITreeStateService _treeState = Substitute.For<ITreeStateService>();

    public SearchBoxTests()
    {
        Services.AddSingleton(_treeState);
    }

    [Fact]
    public async Task OnAdornmentClick_WithText_ClearsSearch()
    {
        var cut = RenderComponent<SearchBox>();
        SetPrivateField(cut.Instance, "_searchText", "acid");

        await cut.InvokeAsync(() => InvokePrivateAsync(cut.Instance, "OnAdornmentClick"));

        _treeState.Received(1).ClearSearch();
        _treeState.DidNotReceive().SetSearchQuery(Arg.Any<string>());
        Assert.Equal(string.Empty, GetPrivateField<string>(cut.Instance, "_searchText"));
    }

    [Fact]
    public async Task OnAdornmentClick_WithoutText_ExecutesSearch()
    {
        var cut = RenderComponent<SearchBox>();
        SetPrivateField(cut.Instance, "_searchText", string.Empty);

        await cut.InvokeAsync(() => InvokePrivateAsync(cut.Instance, "OnAdornmentClick"));

        _treeState.Received(1).SetSearchQuery(string.Empty);
    }

    [Fact]
    public async Task OnKeyDown_Enter_ExecutesSearch()
    {
        var cut = RenderComponent<SearchBox>();
        SetPrivateField(cut.Instance, "_searchText", "wiki");

        await cut.InvokeAsync(() => InvokePrivateAsync(cut.Instance, "OnKeyDown", new KeyboardEventArgs { Key = "Enter" }));

        _treeState.Received(1).SetSearchQuery("wiki");
    }

    [Fact]
    public async Task OnKeyDown_Escape_Clears()
    {
        var cut = RenderComponent<SearchBox>();
        SetPrivateField(cut.Instance, "_searchText", "wiki");

        await cut.InvokeAsync(() => InvokePrivateAsync(cut.Instance, "OnKeyDown", new KeyboardEventArgs { Key = "Escape" }));

        _treeState.Received(1).ClearSearch();
        Assert.Equal(string.Empty, GetPrivateField<string>(cut.Instance, "_searchText"));
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var cut = RenderComponent<SearchBox>();
        cut.Instance.Dispose();
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

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field!.SetValue(target, value);
    }

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return (T)field!.GetValue(target)!;
    }
}
