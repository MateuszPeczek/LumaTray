using System.Runtime.InteropServices;

namespace BrightnessControl.Interop;

internal static class NativeMethods
{
    // ── Monitor enumeration ────────────────────────────────────────────────────

    [StructLayout(LayoutKind.Sequential)]
    internal struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    internal struct MONITORINFOEX
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szDevice;
    }

    internal delegate bool MonitorEnumProc(
        IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

    [DllImport("user32.dll")]
    internal static extern bool EnumDisplayMonitors(
        IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    internal static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    internal struct DISPLAY_DEVICE
    {
        public int cb;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceString;
        public uint StateFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceID;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceKey;
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    internal static extern bool EnumDisplayDevices(
        string? lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

    // ── Physical monitor handles ────────────────────────────────────────────────

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct PHYSICAL_MONITOR
    {
        public IntPtr hPhysicalMonitor;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szPhysicalMonitorDescription;
    }

    [DllImport("Dxva2.dll", SetLastError = true)]
    internal static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(
        IntPtr hMonitor,
        out uint pdwNumberOfPhysicalMonitors);

    [DllImport("Dxva2.dll", SetLastError = true)]
    internal static extern bool GetPhysicalMonitorsFromHMONITOR(
        IntPtr hMonitor,
        uint dwPhysicalMonitorArraySize,
        [Out] PHYSICAL_MONITOR[] pPhysicalMonitorArray);

    [DllImport("Dxva2.dll", SetLastError = true)]
    internal static extern bool DestroyPhysicalMonitors(
        uint dwPhysicalMonitorArraySize,
        [In] PHYSICAL_MONITOR[] pPhysicalMonitorArray);

    // ── DDC/CI brightness ──────────────────────────────────────────────────────

    internal const uint MC_CAPS_BRIGHTNESS = 0x00000002;

    [DllImport("Dxva2.dll", SetLastError = true)]
    internal static extern bool GetMonitorCapabilities(
        IntPtr hPhysicalMonitor,
        out uint pdwMonitorCapabilities,
        out uint pdwSupportedColorTemperatures);

    [DllImport("Dxva2.dll", SetLastError = true)]
    internal static extern bool GetMonitorBrightness(
        IntPtr hPhysicalMonitor,
        out uint pdwMinimumBrightness,
        out uint pdwCurrentBrightness,
        out uint pdwMaximumBrightness);

    [DllImport("Dxva2.dll", SetLastError = true)]
    internal static extern bool SetMonitorBrightness(
        IntPtr hPhysicalMonitor,
        uint dwNewBrightness);

    // ── GDI ───────────────────────────────────────────────────────────────────

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    internal static extern bool DestroyIcon(IntPtr handle);
}
