using System.IO;
using Microsoft.Win32;

namespace LumaTray.Services;

internal static class StartupService
{
    private const string AppName = "LumaTray";
    private const string RunKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    private static string ExePath =>
        Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "BrightnessControl.exe");

    internal static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey);
        return key?.GetValue(AppName)?.ToString() == ExePath;
    }

    internal static void SetEnabled(bool enable)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        if (key is null) return;

        if (enable)
            key.SetValue(AppName, ExePath);
        else
            key.DeleteValue(AppName, throwOnMissingValue: false);
    }
}
