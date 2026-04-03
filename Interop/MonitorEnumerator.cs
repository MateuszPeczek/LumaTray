using LumaTray.Models;
using Microsoft.Win32;

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
    /// Resolves the monitor's actual model name (e.g. "AOC 27B2H") from the EDID
    /// stored in the registry, matched to the correct display by device instance path.
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
        var dd = new NativeMethods.DISPLAY_DEVICE
        {
            cb = System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.DISPLAY_DEVICE>()
        };
        const uint EDD_GET_DEVICE_INTERFACE_NAME = 1;
        if (!NativeMethods.EnumDisplayDevices(mi.szDevice, 0, ref dd, EDD_GET_DEVICE_INTERFACE_NAME))
            return null;

        // Step 3: extract model and instance key from device path
        // e.g. "\\?\DISPLAY#AOC2790#5&17a79d78&0&UID8389904#{guid}"
        var (model, instanceKey) = ExtractDevicePathParts(dd.DeviceID);
        if (string.IsNullOrEmpty(model) || string.IsNullOrEmpty(instanceKey))
            return null;

        // Step 4: read the EDID blob from the registry and parse the friendly name
        try
        {
            using var displayKey = Registry.LocalMachine.OpenSubKey(
                $@"SYSTEM\CurrentControlSet\Enum\DISPLAY\{model}\{instanceKey}\Device Parameters");
            if (displayKey?.GetValue("EDID") is byte[] edid)
                return ParseEdidFriendlyName(edid);
        }
        catch { /* registry unavailable — fall through */ }

        return null;
    }

    private static (string model, string instanceKey) ExtractDevicePathParts(string devicePath)
    {
        // "\\?\DISPLAY#AOC2790#5&17a79d78&0&UID8389904#{guid}"
        if (string.IsNullOrEmpty(devicePath) || !devicePath.StartsWith(@"\\?\"))
            return (string.Empty, string.Empty);

        var parts = devicePath[4..].Split('#');
        if (parts.Length < 3)
            return (string.Empty, string.Empty);

        return (parts[1], parts[2]);
    }

    /// <summary>
    /// Parses the monitor friendly name from an EDID blob.
    /// The name is in a descriptor block tagged 0x000000FC.
    /// </summary>
    private static string? ParseEdidFriendlyName(byte[] edid)
    {
        // EDID has 4 descriptor blocks starting at byte 54, each 18 bytes long
        for (int i = 54; i + 18 <= edid.Length; i += 18)
        {
            // Monitor name descriptor: bytes 0-2 = 0x00, byte 3 = 0xFC
            if (edid[i] == 0 && edid[i + 1] == 0 && edid[i + 2] == 0 && edid[i + 3] == 0xFC)
            {
                // Name is in bytes 5-17 (13 chars), padded with 0x0A (newline) or spaces
                var nameBytes = new byte[13];
                Array.Copy(edid, i + 5, nameBytes, 0, 13);
                var name = System.Text.Encoding.ASCII.GetString(nameBytes)
                    .TrimEnd('\n', '\r', ' ', '\0');
                return string.IsNullOrWhiteSpace(name) ? null : name;
            }
        }
        return null;
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
