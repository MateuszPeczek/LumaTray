using BrightnessControl.Interop;

namespace BrightnessControl.Models;

internal sealed class MonitorInfo
{
    public required string Name { get; init; }
    public required uint MinBrightness { get; init; }
    public required uint MaxBrightness { get; init; }
    public uint CurrentBrightness { get; set; }

    // The physical handle and backing array are kept together so we can
    // call DestroyPhysicalMonitors (which takes the full array) correctly.
    internal required NativeMethods.PHYSICAL_MONITOR[] PhysicalArray { get; init; }
    internal required IntPtr PhysicalHandle { get; init; }

    public int BrightnessPercent =>
        MaxBrightness == MinBrightness
            ? 0
            : (int)((CurrentBrightness - MinBrightness) * 100u / (MaxBrightness - MinBrightness));
}
