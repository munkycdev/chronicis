using Chronicis.CaptureApp.Services;
using Chronicis.CaptureApp.UI;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Chronicis.CaptureApp;

static class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // Setup Dependency Injection
        var services = new ServiceCollection();
        ConfigureServices(services);
        var serviceProvider = services.BuildServiceProvider();

        // Run the application
        var mainForm = serviceProvider.GetRequiredService<MainForm>();
        var trayService = serviceProvider.GetRequiredService<ISystemTrayService>();

        Application.Run(mainForm);
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Services
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IAudioSourceProvider, AudioSourceProvider>();
        services.AddSingleton<ITranscriptionService, WhisperTranscriptionService>();
        services.AddSingleton<IAudioCaptureService, AudioCaptureService>();
        services.AddSingleton<ISystemTrayService, SystemTrayService>();

        // UI
        services.AddSingleton<MainForm>();
    }
}