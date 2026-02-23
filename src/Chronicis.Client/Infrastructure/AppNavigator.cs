using Chronicis.Client.Abstractions;
using Microsoft.AspNetCore.Components;

namespace Chronicis.Client.Infrastructure;

/// <summary>
/// Wraps <see cref="NavigationManager"/> to implement <see cref="IAppNavigator"/>.
/// </summary>
public sealed class AppNavigator : IAppNavigator
{
    private readonly NavigationManager _navigation;

    public AppNavigator(NavigationManager navigation)
    {
        _navigation = navigation;
    }

    /// <inheritdoc />
    public string BaseUri => _navigation.BaseUri;

    /// <inheritdoc />
    public string Uri => _navigation.Uri;

    /// <inheritdoc />
    public void NavigateTo(string url, bool replace = false) =>
        _navigation.NavigateTo(url, replace);
}
