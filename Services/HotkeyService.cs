using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using LumaTray.Interop;
using LumaTray.Models;

namespace LumaTray.Services;

internal sealed class HotkeyService : IDisposable
{
    private const int HotkeyIdUp = 0x1001;
    private const int HotkeyIdDown = 0x1002;

    private HotkeySettings _settings;
    private HwndSource? _hwndSource;
    private IntPtr _hwnd;
    private bool _registered;

    /// <summary>
    /// Invoked on the UI thread after brightness is stepped.
    /// Parameter is the average brightness percent across all monitors.
    /// </summary>
    internal Action<int>? BrightnessChanged { get; set; }

    internal HotkeyService(HotkeySettings settings)
    {
        _settings = settings;
        CreateMessageWindow();
        Register();
    }

    internal void UpdateSettings(HotkeySettings settings)
    {
        Unregister();
        _settings = settings;
        Register();
    }

    private void CreateMessageWindow()
    {
        // Create a hidden message-only window to receive WM_HOTKEY
        var parameters = new HwndSourceParameters("LumaTrayHotkeyWindow")
        {
            Width = 0,
            Height = 0,
            WindowStyle = 0, // not visible
        };
        _hwndSource = new HwndSource(parameters);
        _hwndSource.AddHook(WndProc);
        _hwnd = _hwndSource.Handle;
    }

    private void Register()
    {
        if (_hwnd == IntPtr.Zero) return;

        var upMod = ToNativeModifiers(_settings.BrightnessUp.Modifiers) | NativeMethods.MOD_NOREPEAT;
        var upVk = (uint)KeyInterop.VirtualKeyFromKey(_settings.BrightnessUp.Key);

        var downMod = ToNativeModifiers(_settings.BrightnessDown.Modifiers) | NativeMethods.MOD_NOREPEAT;
        var downVk = (uint)KeyInterop.VirtualKeyFromKey(_settings.BrightnessDown.Key);

        _registered = true;
        if (!NativeMethods.RegisterHotKey(_hwnd, HotkeyIdUp, upMod, upVk))
            _registered = false;
        if (!NativeMethods.RegisterHotKey(_hwnd, HotkeyIdDown, downMod, downVk))
            _registered = false;
    }

    private void Unregister()
    {
        if (!_registered || _hwnd == IntPtr.Zero) return;
        NativeMethods.UnregisterHotKey(_hwnd, HotkeyIdUp);
        NativeMethods.UnregisterHotKey(_hwnd, HotkeyIdDown);
        _registered = false;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY)
        {
            int id = wParam.ToInt32();
            if (id == HotkeyIdUp)
            {
                int pct = StepBrightness(_settings.StepPercent);
                BrightnessChanged?.Invoke(pct);
                handled = true;
            }
            else if (id == HotkeyIdDown)
            {
                int pct = StepBrightness(-_settings.StepPercent);
                BrightnessChanged?.Invoke(pct);
                handled = true;
            }
        }
        return IntPtr.Zero;
    }

    private static int StepBrightness(int stepPercent)
    {
        var monitors = MonitorEnumerator.GetSupportedMonitors();
        int avgPercent = 0;
        try
        {
            foreach (var monitor in monitors)
            {
                uint range = monitor.MaxBrightness - monitor.MinBrightness;
                int delta = (int)(range * stepPercent / 100.0);
                if (delta == 0) delta = stepPercent > 0 ? 1 : -1;

                long newVal = (long)monitor.CurrentBrightness + delta;
                uint clamped = (uint)Math.Clamp(newVal, monitor.MinBrightness, monitor.MaxBrightness);
                MonitorEnumerator.SetBrightness(monitor, clamped);
            }

            if (monitors.Count > 0)
                avgPercent = (int)monitors.Average(m => m.BrightnessPercent);
        }
        finally
        {
            MonitorEnumerator.Release(monitors);
        }

        return avgPercent;
    }

    private static uint ToNativeModifiers(ModifierKeys mods)
    {
        uint native = 0;
        if (mods.HasFlag(ModifierKeys.Alt)) native |= NativeMethods.MOD_ALT;
        if (mods.HasFlag(ModifierKeys.Control)) native |= NativeMethods.MOD_CONTROL;
        if (mods.HasFlag(ModifierKeys.Shift)) native |= NativeMethods.MOD_SHIFT;
        if (mods.HasFlag(ModifierKeys.Windows)) native |= NativeMethods.MOD_WIN;
        return native;
    }

    public void Dispose()
    {
        Unregister();
        _hwndSource?.RemoveHook(WndProc);
        _hwndSource?.Dispose();
        _hwndSource = null;
    }
}
