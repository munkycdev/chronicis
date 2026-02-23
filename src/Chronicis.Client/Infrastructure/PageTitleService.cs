using Chronicis.Client.Abstractions;
using Chronicis.Client.Utilities;
using Microsoft.JSInterop;

namespace Chronicis.Client.Infrastructure;

/// <summary>
/// Implements <see cref="IPageTitleService"/> using <see cref="IJSRuntime"/>
/// to set the browser document title.
/// </summary>
public sealed class PageTitleService : IPageTitleService
{
    private readonly IJSRuntime _jsRuntime;

    public PageTitleService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <inheritdoc />
    public Task SetTitleAsync(string title)
    {
        var escaped = JsUtilities.EscapeForJs(title);
        return _jsRuntime.InvokeVoidAsync(
            "eval",
            $"document.title = '{escaped} - Chronicis'").AsTask();
    }
}
