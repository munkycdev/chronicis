using Chronicis.Client.Infrastructure;
using Microsoft.JSInterop;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Infrastructure;

public class PageTitleServiceTests
{
    private static (PageTitleService Sut, IJSRuntime JsRuntime) CreateSut()
    {
        var jsRuntime = Substitute.For<IJSRuntime>();
        jsRuntime.InvokeAsync<Microsoft.JSInterop.Infrastructure.IJSVoidResult>(
                Arg.Any<string>(), Arg.Any<object?[]?>())
            .Returns(callInfo => new ValueTask<Microsoft.JSInterop.Infrastructure.IJSVoidResult>(
                Substitute.For<Microsoft.JSInterop.Infrastructure.IJSVoidResult>()));
        return (new PageTitleService(jsRuntime), jsRuntime);
    }

    [Fact]
    public async Task SetTitleAsync_InvokesEvalWithCorrectTitle()
    {
        var (sut, jsRuntime) = CreateSut();

        await sut.SetTitleAsync("Magic");

        await jsRuntime.Received(1).InvokeAsync<Microsoft.JSInterop.Infrastructure.IJSVoidResult>(
            "eval",
            Arg.Is<object?[]?>(args => args != null
                && args.Length == 1
                && args[0] != null
                && args[0]!.ToString()!.Contains("Magic - Chronicis")));
    }

    [Fact]
    public async Task SetTitleAsync_EscapesSingleQuotesInTitle()
    {
        var (sut, jsRuntime) = CreateSut();

        await sut.SetTitleAsync("Elminster's Tower");

        await jsRuntime.Received(1).InvokeAsync<Microsoft.JSInterop.Infrastructure.IJSVoidResult>(
            "eval",
            Arg.Is<object?[]?>(args => args != null
                && args.Length == 1
                && args[0] != null
                && args[0]!.ToString()!.Contains(@"Elminster\'s Tower")));
    }

    [Fact]
    public async Task SetTitleAsync_HandlesEmptyTitle()
    {
        var (sut, jsRuntime) = CreateSut();

        await sut.SetTitleAsync(string.Empty);

        await jsRuntime.Received(1).InvokeAsync<Microsoft.JSInterop.Infrastructure.IJSVoidResult>(
            "eval",
            Arg.Is<object?[]?>(args => args != null
                && args.Length == 1
                && args[0] != null
                && args[0]!.ToString()!.Contains(" - Chronicis")));
    }
}
