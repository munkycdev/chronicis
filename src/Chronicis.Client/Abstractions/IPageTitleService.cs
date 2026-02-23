namespace Chronicis.Client.Abstractions;

/// <summary>
/// Abstracts browser page title updates so ViewModels can set the document
/// title without depending on <see cref="Microsoft.JSInterop.IJSRuntime"/> directly.
/// </summary>
public interface IPageTitleService
{
    /// <summary>
    /// Sets the browser tab title, appending the application name suffix.
    /// </summary>
    /// <param name="title">
    /// The page-specific portion of the title (e.g. "Magic").
    /// The implementation appends " - Chronicis" automatically.
    /// </param>
    Task SetTitleAsync(string title);
}
