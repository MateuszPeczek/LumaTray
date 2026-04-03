using System.IO;
using System.Text.Json;
using System.Windows.Input;

namespace LumaTray.Models;

internal sealed class HotkeySettings
{
    private static readonly string SettingsPath = Path.Combine(
        AppContext.BaseDirectory, "hotkey-settings.json");

    public HotkeyBinding BrightnessUp { get; set; } = new(ModifierKeys.Control | ModifierKeys.Alt, Key.Up);
    public HotkeyBinding BrightnessDown { get; set; } = new(ModifierKeys.Control | ModifierKeys.Alt, Key.Down);
    public int StepPercent { get; set; } = 10;

    internal static HotkeySettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                string json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<HotkeySettings>(json) ?? new HotkeySettings();
            }
        }
        catch { /* corrupt file — use defaults */ }

        return new HotkeySettings();
    }

    internal void Save()
    {
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SettingsPath, json);
    }
}

internal sealed class HotkeyBinding
{
    public ModifierKeys Modifiers { get; set; }
    public Key Key { get; set; }

    public HotkeyBinding() { }
    public HotkeyBinding(ModifierKeys modifiers, Key key)
    {
        Modifiers = modifiers;
        Key = key;
    }

    [System.Text.Json.Serialization.JsonIgnore]
    public bool HasNonModifierKey =>
        Key is not (Key.LeftCtrl or Key.RightCtrl
            or Key.LeftAlt or Key.RightAlt
            or Key.LeftShift or Key.RightShift
            or Key.LWin or Key.RWin
            or Key.None);

    public string DisplayText =>
        string.IsNullOrEmpty(ModifierText)
            ? Key.ToString()
            : $"{ModifierText} + {Key}";

    private string ModifierText
    {
        get
        {
            var parts = new List<string>();
            if (Modifiers.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
            if (Modifiers.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
            if (Modifiers.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
            if (Modifiers.HasFlag(ModifierKeys.Windows)) parts.Add("Win");
            return string.Join(" + ", parts);
        }
    }
}
