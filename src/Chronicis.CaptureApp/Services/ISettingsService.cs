using Chronicis.CaptureApp.Models;

namespace Chronicis.CaptureApp.Services;

public interface ISettingsService
{
    AppSettings Load();
    void Save(AppSettings settings);
}
