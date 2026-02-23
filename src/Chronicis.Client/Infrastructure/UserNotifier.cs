using Chronicis.Client.Abstractions;
using MudBlazor;

namespace Chronicis.Client.Infrastructure;

/// <summary>
/// Wraps <see cref="ISnackbar"/> to implement <see cref="IUserNotifier"/>.
/// </summary>
public sealed class UserNotifier : IUserNotifier
{
    private readonly ISnackbar _snackbar;

    public UserNotifier(ISnackbar snackbar)
    {
        _snackbar = snackbar;
    }

    /// <inheritdoc />
    public void Success(string message) =>
        _snackbar.Add(message, Severity.Success);

    /// <inheritdoc />
    public void Error(string message) =>
        _snackbar.Add(message, Severity.Error);

    /// <inheritdoc />
    public void Warning(string message) =>
        _snackbar.Add(message, Severity.Warning);

    /// <inheritdoc />
    public void Info(string message) =>
        _snackbar.Add(message, Severity.Info);
}
