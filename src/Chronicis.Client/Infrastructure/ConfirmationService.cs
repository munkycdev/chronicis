using Chronicis.Client.Abstractions;
using MudBlazor;

namespace Chronicis.Client.Infrastructure;

/// <summary>
/// Implements <see cref="IConfirmationService"/> using MudBlazor's
/// <see cref="IDialogService.ShowMessageBox"/> for consistent, styled confirmations.
/// </summary>
public sealed class ConfirmationService : IConfirmationService
{
    private readonly IDialogService _dialogService;

    public ConfirmationService(IDialogService dialogService)
    {
        _dialogService = dialogService;
    }

    /// <inheritdoc />
    public async Task<bool> ConfirmAsync(
        string title,
        string message,
        string confirmText = "Confirm",
        string cancelText = "Cancel")
    {
        var result = await _dialogService.ShowMessageBox(
            title,
            message,
            yesText: confirmText,
            cancelText: cancelText);

        return result == true;
    }
}
