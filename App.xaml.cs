using System.Windows;
using BrightnessControl.Services;

namespace BrightnessControl;

public partial class App : Application
{
    private TrayIconService? _trayService;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _trayService = new TrayIconService();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayService?.Dispose();
        base.OnExit(e);
    }
}
