namespace Chronicis.Client.Abstractions;

/// <summary>
/// Abstracts simple yes/no confirmation prompts so ViewModels can request
/// confirmation without depending on MudBlazor dialog infrastructure directly.
/// </summary>
public interface IConfirmationService
{
    /// <summary>
    /// Presents a confirmation dialog to the user.
    /// </summary>
    /// <param name="title">The dialog title.</param>
    /// <param name="message">The confirmation message body.</param>
    /// <param name="confirmText">Label for the confirm button. Defaults to "Confirm".</param>
    /// <param name="cancelText">Label for the cancel button. Defaults to "Cancel".</param>
    /// <returns>
    /// <c>true</c> if the user confirmed; <c>false</c> if the user cancelled.
    /// </returns>
    Task<bool> ConfirmAsync(
        string title,
        string message,
        string confirmText = "Confirm",
        string cancelText = "Cancel");
}
