using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LumaTray.Models;

namespace LumaTray.Views;

public partial class HotkeyConfigWindow : Window
{
    private HotkeyBinding _upBinding;
    private HotkeyBinding _downBinding;

    internal HotkeySettings? Result { get; private set; }

    internal HotkeyConfigWindow(HotkeySettings current)
    {
        InitializeComponent();

        _upBinding = new HotkeyBinding(current.BrightnessUp.Modifiers, current.BrightnessUp.Key);
        _downBinding = new HotkeyBinding(current.BrightnessDown.Modifiers, current.BrightnessDown.Key);

        BrightnessUpBox.Text = _upBinding.DisplayText;
        BrightnessDownBox.Text = _downBinding.DisplayText;

        StepSlider.Value = current.StepPercent;
        StepLabel.Text = $"{current.StepPercent}%";
        StepSlider.ValueChanged += (_, e) => StepLabel.Text = $"{(int)e.NewValue}%";
    }

    private void OnHotkeyKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;

        // Ignore lone modifier presses — wait for the actual key
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key is Key.LeftCtrl or Key.RightCtrl
            or Key.LeftAlt or Key.RightAlt
            or Key.LeftShift or Key.RightShift
            or Key.LWin or Key.RWin)
            return;

        var modifiers = Keyboard.Modifiers;
        var box = (TextBox)sender;
        var binding = new HotkeyBinding(modifiers, key);

        if (box == BrightnessUpBox)
        {
            _upBinding = binding;
            BrightnessUpBox.Text = binding.DisplayText;
        }
        else
        {
            _downBinding = binding;
            BrightnessDownBox.Text = binding.DisplayText;
        }
    }

    private void OnHotkeyBoxFocus(object sender, RoutedEventArgs e)
    {
        var box = (TextBox)sender;
        box.Text = "Press a key combination...";
        box.BorderBrush = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(0, 120, 212));
    }

    private void OnHotkeyBoxLostFocus(object sender, RoutedEventArgs e)
    {
        var box = (TextBox)sender;
        box.BorderBrush = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(68, 68, 68));

        // Restore display text in case user clicked away without pressing a key
        if (box == BrightnessUpBox)
            box.Text = _upBinding.DisplayText;
        else
            box.Text = _downBinding.DisplayText;
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
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
        DialogResult = false;
        Close();
    }
}
