using MudBlazor;

namespace Chronicis.Client.Abstractions;

/// <summary>
/// Abstracts user-facing notifications so ViewModels can surface feedback
/// without depending on <see cref="ISnackbar"/> directly.
/// </summary>
public interface IUserNotifier
{
    /// <summary>Shows a success notification.</summary>
    void Success(string message);

    /// <summary>Shows an error notification.</summary>
    void Error(string message);

    /// <summary>Shows a warning notification.</summary>
    void Warning(string message);

    /// <summary>Shows an informational notification.</summary>
    void Info(string message);
}
