using System.Management;
using LumaTray.Models;

namespace LumaTray.Interop;

/// <summary>
/// Enumerates external monitors that support DDC/CI brightness control.
/// Caller is responsible for calling <see cref="Release"/> when done.
/// </summary>
internal static class MonitorEnumerator
{
    /// <summary>
    /// Returns all monitors that support DDC/CI brightness adjustment.
    /// Each <see cref="MonitorInfo"/> holds a live physical monitor handle;
    /// call <see cref="Release"/> when the list is no longer needed.
    /// </summary>
    internal static List<MonitorInfo> GetSupportedMonitors()
    {
        var result = new List<MonitorInfo>();

        NativeMethods.MonitorEnumProc callback = (IntPtr hMonitor, IntPtr hdc, ref NativeMethods.RECT rect, IntPtr data) =>
        {
            TryAddMonitor(hMonitor, result);
            return true;
        };
        NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero);

        return result;
    }

    private static void TryAddMonitor(IntPtr hMonitor, List<MonitorInfo> result)
    {
        if (!NativeMethods.GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, out uint count) || count == 0)
            return;

        var physArray = new NativeMethods.PHYSICAL_MONITOR[count];
        if (!NativeMethods.GetPhysicalMonitorsFromHMONITOR(hMonitor, count, physArray))
            return;

        foreach (var pm in physArray)
        {
            IntPtr h = pm.hPhysicalMonitor;

            // Skip the GetMonitorCapabilities check — many monitors (including
            // those controllable via manufacturer software) don't report
            // MC_CAPS_BRIGHTNESS reliably. Go straight to GetMonitorBrightness.
            bool gotBrightness = NativeMethods.GetMonitorBrightness(h, out uint min, out uint cur, out uint max);
            if (!gotBrightness)
            {
                // Some monitors need a moment on first DDC/CI contact
                Thread.Sleep(50);
                gotBrightness = NativeMethods.GetMonitorBrightness(h, out min, out cur, out max);
            }
            if (!gotBrightness)
                continue;

            string name = GetMonitorFriendlyName(hMonitor)
                ?? pm.szPhysicalMonitorDescription
                ?? "External Monitor";

            result.Add(new MonitorInfo
            {
                Name = name,
                MinBrightness = min,
                CurrentBrightness = cur,
                MaxBrightness = max,
                PhysicalArray = physArray,
                PhysicalHandle = h,
            });

            // Only add one entry per physical monitor array — avoid adding
            // duplicates if multiple physical handles resolve to the same display.
            return;
        }

        // No brightness-capable physical monitor found — release handles
        NativeMethods.DestroyPhysicalMonitors(count, physArray);
    }

    /// <summary>
    /// Resolves the monitor's actual model name (e.g. "AOC 27B2H") via WMI WmiMonitorID,
    /// matched to the correct display by device instance path.
    /// </summary>
    private static string? GetMonitorFriendlyName(IntPtr hMonitor)
    {
        // Step 1: get the adapter name (e.g. "\\.\DISPLAY1") for this HMONITOR
        var mi = new NativeMethods.MONITORINFOEX
        {
            cbSize = System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.MONITORINFOEX>()
        };
        if (!NativeMethods.GetMonitorInfo(hMonitor, ref mi))
            return null;

        // Step 2: get the device interface path for the monitor on that adapter
        // EDD_GET_DEVICE_INTERFACE_NAME = 1 → DeviceID becomes
        // "\\?\DISPLAY#AOC2790#5&17a79d78&0&UID8389904#{guid}"
        var dd = new NativeMethods.DISPLAY_DEVICE
        {
            cb = System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.DISPLAY_DEVICE>()
        };
        const uint EDD_GET_DEVICE_INTERFACE_NAME = 1;
        if (!NativeMethods.EnumDisplayDevices(mi.szDevice, 0, ref dd, EDD_GET_DEVICE_INTERFACE_NAME))
            return null;

        // Step 3: extract the instance key, e.g. "5&17a79d78&0&UID8389904"
        string instanceKey = ExtractInstanceKey(dd.DeviceID);
        if (string.IsNullOrEmpty(instanceKey))
            return null;

        // Step 4: find the matching WMI entry and read UserFriendlyName
        // WmiMonitorID.InstanceName looks like "DISPLAY\AOC2790\5&17a79d78&0&UID8389904_0"
        try
        {
            using var searcher = new ManagementObjectSearcher("root\\wmi", "SELECT * FROM WmiMonitorID");
            foreach (ManagementObject obj in searcher.Get())
            {
                string instanceName = obj["InstanceName"]?.ToString() ?? "";
                if (!instanceName.Contains(instanceKey, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (obj["UserFriendlyName"] is ushort[] chars)
                {
                    string name = new string(chars.TakeWhile(c => c != 0).Select(c => (char)c).ToArray()).Trim();
                    if (!string.IsNullOrEmpty(name))
                        return name;
                }
            }
        }
        catch { /* WMI unavailable — fall through to generic name */ }

        return null;
    }

    private static string ExtractInstanceKey(string devicePath)
    {
        // "\\?\DISPLAY#AOC2790#5&17a79d78&0&UID8389904#{guid}"
        //  parts after removing prefix: ["DISPLAY","AOC2790","5&17a79d78&0&UID8389904","{guid}"]
        if (string.IsNullOrEmpty(devicePath) || !devicePath.StartsWith(@"\\?\"))
            return string.Empty;

        var parts = devicePath[4..].Split('#');
        return parts.Length >= 3 ? parts[2] : string.Empty;
    }

    internal static void SetBrightness(MonitorInfo monitor, uint brightness)
    {
        brightness = Math.Clamp(brightness, monitor.MinBrightness, monitor.MaxBrightness);
        NativeMethods.SetMonitorBrightness(monitor.PhysicalHandle, brightness);
        monitor.CurrentBrightness = brightness;
    }

    internal static void Release(IEnumerable<MonitorInfo> monitors)
    {
        // Group by array reference (multiple MonitorInfo could share an array)
        foreach (var group in monitors.GroupBy(m => m.PhysicalArray, ReferenceEqualityComparer.Instance))
        {
            var arr = group.First().PhysicalArray;
            NativeMethods.DestroyPhysicalMonitors((uint)arr.Length, arr);
        }
    }
}
