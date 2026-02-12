namespace Chronicis.CaptureApp.Services;

public interface ISystemTrayService
{
    void Initialize(Form mainForm);
    void SetRecordingState(bool isRecording);
    void ShowBalloonTip(string title, string message);
}
