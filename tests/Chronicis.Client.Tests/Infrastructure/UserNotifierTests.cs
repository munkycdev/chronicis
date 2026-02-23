using Chronicis.Client.Infrastructure;
using MudBlazor;
using NSubstitute;
using Xunit;

namespace Chronicis.Client.Tests.Infrastructure;

public class UserNotifierTests
{
    private static (UserNotifier Sut, ISnackbar Snackbar) CreateSut()
    {
        var snackbar = Substitute.For<ISnackbar>();
        return (new UserNotifier(snackbar), snackbar);
    }

    [Fact]
    public void Success_AddsSuccessSeveritySnackbar()
    {
        var (sut, snackbar) = CreateSut();
        sut.Success("All good");
        snackbar.Received(1).Add("All good", Severity.Success, Arg.Any<Action<SnackbarOptions>>());
    }

    [Fact]
    public void Error_AddsErrorSeveritySnackbar()
    {
        var (sut, snackbar) = CreateSut();
        sut.Error("Something broke");
        snackbar.Received(1).Add("Something broke", Severity.Error, Arg.Any<Action<SnackbarOptions>>());
    }

    [Fact]
    public void Warning_AddsWarningSeveritySnackbar()
    {
        var (sut, snackbar) = CreateSut();
        sut.Warning("Watch out");
        snackbar.Received(1).Add("Watch out", Severity.Warning, Arg.Any<Action<SnackbarOptions>>());
    }

    [Fact]
    public void Info_AddsInfoSeveritySnackbar()
    {
        var (sut, snackbar) = CreateSut();
        sut.Info("FYI");
        snackbar.Received(1).Add("FYI", Severity.Info, Arg.Any<Action<SnackbarOptions>>());
    }
}
