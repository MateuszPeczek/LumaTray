using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LumaTray.Models;

namespace LumaTray.Views;

public partial class HotkeyConfigWindow : Window
{
    private HotkeyBinding _upBinding;
    private HotkeyBinding _downBinding;

    // Which border is currently recording (null = not recording)
    private Border? _activeBorder;
    // Keys currently held during recording
    private readonly HashSet<Key> _heldKeys = [];

    private static readonly SolidColorBrush BorderDefault = new(Color.FromRgb(68, 68, 68));
    private static readonly SolidColorBrush BorderActive = new(Color.FromRgb(0, 120, 212));
    private static readonly SolidColorBrush BtnRecordBg = new(Color.FromRgb(51, 51, 51));
    private static readonly SolidColorBrush BtnStopBg = new(Color.FromRgb(180, 40, 40));

    internal HotkeySettings? Result { get; private set; }

    internal HotkeyConfigWindow(HotkeySettings current)
    {
        InitializeComponent();

        _upBinding = new HotkeyBinding(current.BrightnessUp.Modifiers, current.BrightnessUp.Key);
        _downBinding = new HotkeyBinding(current.BrightnessDown.Modifiers, current.BrightnessDown.Key);

        BrightnessUpLabel.Text = _upBinding.DisplayText;
        BrightnessDownLabel.Text = _downBinding.DisplayText;

        StepSlider.Value = current.StepPercent;
        StepLabel.Text = $"{current.StepPercent}%";
        StepSlider.ValueChanged += (_, e) => StepLabel.Text = $"{(int)e.NewValue}%";
    }

    private void OnRecordClick(object sender, RoutedEventArgs e)
    {
        var button = (Button)sender;
        bool isUp = button == UpRecordBtn;
        var border = isUp ? UpBorder : DownBorder;

        if (_activeBorder == border)
        {
            // Stop recording
            StopRecording(commit: true);
            return;
        }

        // If another border was recording, stop it first (discard)
        if (_activeBorder != null)
            StopRecording(commit: false);

        // Start recording
        _activeBorder = border;
        _heldKeys.Clear();
        border.BorderBrush = BorderActive;
        GetLabel(border).Text = "Press keys...";
        GetLabel(border).Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180));

        button.Content = "Stop";
        button.Background = BtnStopBg;

        // Focus the border so it receives key events
        border.Focus();
    }

    private void StopRecording(bool commit)
    {
        if (_activeBorder == null) return;

        var border = _activeBorder;
        bool isUp = border == UpBorder;
        var button = isUp ? UpRecordBtn : DownRecordBtn;
        var label = GetLabel(border);

        if (commit && _heldKeys.Count > 0)
        {
            // Build binding from captured keys
            var modifiers = ModifierKeys.None;
            Key? mainKey = null;

            foreach (var k in _heldKeys)
            {
                if (IsModifierKey(k))
                    modifiers |= ToModifier(k);
                else
                    mainKey = k; // last non-modifier wins
            }

            if (mainKey == null)
            {
                // Only modifier keys were pressed — show warning
                ShowValidation("A non-modifier key is required (e.g. an arrow key, letter, F-key, or peripheral button). Modifier-only combos are not supported by Windows.");
            }
            else
            {
                ClearValidation();
                var binding = new HotkeyBinding(modifiers, mainKey.Value);
                if (isUp)
                    _upBinding = binding;
                else
                    _downBinding = binding;
            }
        }

        // Reset visual state
        border.BorderBrush = BorderDefault;
        label.Foreground = Brushes.White;
        label.Text = isUp ? _upBinding.DisplayText : _downBinding.DisplayText;
        button.Content = "Record";
        button.Background = BtnRecordBg;

        _activeBorder = null;
        _heldKeys.Clear();
    }

    private void OnHotkeyKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;
        if (_activeBorder == null || (Border)sender != _activeBorder) return;

        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        _heldKeys.Add(key);
        UpdateRecordingDisplay();
    }

    private void OnHotkeyKeyUp(object sender, KeyEventArgs e)
    {
        e.Handled = true;
        // Don't remove keys on release — we want to accumulate all pressed keys
    }

    private void UpdateRecordingDisplay()
    {
        if (_activeBorder == null) return;
        var label = GetLabel(_activeBorder);

        var parts = new List<string>();
        foreach (var k in _heldKeys)
        {
            string name = FormatKey(k);
            if (!parts.Contains(name))
                parts.Add(name);
        }

        label.Text = parts.Count > 0 ? string.Join(" + ", parts) : "Press keys...";
    }

    private TextBlock GetLabel(Border border) =>
        border == UpBorder ? BrightnessUpLabel : BrightnessDownLabel;

    private static bool IsModifierKey(Key key) =>
        key is Key.LeftCtrl or Key.RightCtrl
            or Key.LeftAlt or Key.RightAlt
            or Key.LeftShift or Key.RightShift
            or Key.LWin or Key.RWin;

    private static ModifierKeys ToModifier(Key key) => key switch
    {
        Key.LeftCtrl or Key.RightCtrl => ModifierKeys.Control,
        Key.LeftAlt or Key.RightAlt => ModifierKeys.Alt,
        Key.LeftShift or Key.RightShift => ModifierKeys.Shift,
        Key.LWin or Key.RWin => ModifierKeys.Windows,
        _ => ModifierKeys.None,
    };

    private static string FormatKey(Key key) => key switch
    {
        Key.LeftCtrl or Key.RightCtrl => "Ctrl",
        Key.LeftAlt or Key.RightAlt => "Alt",
        Key.LeftShift or Key.RightShift => "Shift",
        Key.LWin or Key.RWin => "Win",
        _ => key.ToString(),
    };

    private void ShowValidation(string message)
    {
        ValidationLabel.Text = message;
        ValidationLabel.Visibility = Visibility.Visible;
    }

    private void ClearValidation()
    {
        ValidationLabel.Visibility = Visibility.Collapsed;
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        // Stop any active recording before saving
        StopRecording(commit: true);

        // Validate both bindings have a non-modifier key
        if (!_upBinding.HasNonModifierKey || !_downBinding.HasNonModifierKey)
        {
            ShowValidation("Both hotkeys must include at least one non-modifier key (e.g. arrow key, letter, F-key, or peripheral button).");
            return;
        }

        ClearValidation();
        Result = new HotkeySettings
        {
            BrightnessUp = _upBinding,
            BrightnessDown = _downBinding,
            StepPercent = (int)StepSlider.Value,
        };
        Result.Save();
        DialogResult = true;
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        StopRecording(commit: false);
        DialogResult = false;
        Close();
    }
}
