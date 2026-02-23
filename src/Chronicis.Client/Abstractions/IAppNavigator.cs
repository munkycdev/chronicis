namespace Chronicis.Client.Abstractions;

/// <summary>
/// Abstracts navigation so ViewModels can trigger navigation without
/// depending on <see cref="Microsoft.AspNetCore.Components.NavigationManager"/> directly.
/// </summary>
public interface IAppNavigator
{
    /// <summary>Navigates to the specified URL.</summary>
    /// <param name="url">The URL to navigate to.</param>
    /// <param name="replace">
    /// When <c>true</c> replaces the current history entry rather than pushing a new one.
    /// </param>
    void NavigateTo(string url, bool replace = false);

    /// <summary>Returns the base URI of the application.</summary>
    string BaseUri { get; }

    /// <summary>Returns the current absolute URI.</summary>
    string Uri { get; }
}
