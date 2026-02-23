using Chronicis.Client.Infrastructure;
using MudBlazor;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Infrastructure;

public class ConfirmationServiceTests
{
    private static (ConfirmationService Sut, IDialogService DialogService) CreateSut()
    {
        var dialogService = Substitute.For<IDialogService>();
        return (new ConfirmationService(dialogService), dialogService);
    }

    private static void SetupShowMessageBox(IDialogService dialogService, bool? returnValue) =>
        dialogService.ShowMessageBox(
            Arg.Any<string>(), Arg.Any<string>(),
            yesText: Arg.Any<string>(),
            noText: Arg.Any<string?>(),
            cancelText: Arg.Any<string>(),
            options: Arg.Any<DialogOptions?>())
            .Returns(Task.FromResult<bool?>(returnValue));

    [Fact]
    public async Task ConfirmAsync_WhenUserClicksConfirm_ReturnsTrue()
    {
        var (sut, dialogService) = CreateSut();
        SetupShowMessageBox(dialogService, true);

        var result = await sut.ConfirmAsync("Delete?", "Are you sure?");

        Assert.True(result);
    }

    [Fact]
    public async Task ConfirmAsync_WhenUserClicksCancel_ReturnsFalse()
    {
        var (sut, dialogService) = CreateSut();
        SetupShowMessageBox(dialogService, null);

        var result = await sut.ConfirmAsync("Delete?", "Are you sure?");

        Assert.False(result);
    }

    [Fact]
    public async Task ConfirmAsync_WhenDialogReturnsExplicitFalse_ReturnsFalse()
    {
        var (sut, dialogService) = CreateSut();
        SetupShowMessageBox(dialogService, false);

        var result = await sut.ConfirmAsync("Title", "Message");

        Assert.False(result);
    }

    [Fact]
    public async Task ConfirmAsync_PassesTitleAndMessageToDialogService()
    {
        var (sut, dialogService) = CreateSut();
        SetupShowMessageBox(dialogService, null);

        await sut.ConfirmAsync("My Title", "My Message");

        await dialogService.Received(1).ShowMessageBox(
            "My Title", "My Message",
            yesText: Arg.Any<string>(),
            noText: Arg.Any<string?>(),
            cancelText: Arg.Any<string>(),
            options: Arg.Any<DialogOptions?>());
    }

    [Fact]
    public async Task ConfirmAsync_UsesDefaultButtonLabels_WhenNotProvided()
    {
        var (sut, dialogService) = CreateSut();
        SetupShowMessageBox(dialogService, null);

        await sut.ConfirmAsync("Title", "Message");

        await dialogService.Received(1).ShowMessageBox(
            Arg.Any<string>(), Arg.Any<string>(),
            yesText: "Confirm",
            noText: Arg.Any<string?>(),
            cancelText: "Cancel",
            options: Arg.Any<DialogOptions?>());
    }

    [Fact]
    public async Task ConfirmAsync_UsesCustomButtonLabels_WhenProvided()
    {
        var (sut, dialogService) = CreateSut();
        SetupShowMessageBox(dialogService, null);

        await sut.ConfirmAsync("Title", "Message", confirmText: "Delete", cancelText: "Keep");

        await dialogService.Received(1).ShowMessageBox(
            Arg.Any<string>(), Arg.Any<string>(),
            yesText: "Delete",
            noText: Arg.Any<string?>(),
            cancelText: "Keep",
            options: Arg.Any<DialogOptions?>());
    }
}
