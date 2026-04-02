using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using LumaTray.Interop;
using LumaTray.Models;

namespace LumaTray.Views;

public partial class BrightnessPopup : Window
{
    private List<MonitorInfo> _monitors = [];
    private DispatcherTimer? _debounce;

    public BrightnessPopup()
    {
        InitializeComponent();
        Deactivated += (_, _) => Hide();
    }

    public void RefreshAndShow()
    {
        // Release previous handles before acquiring new ones
        MonitorEnumerator.Release(_monitors);
        _monitors = MonitorEnumerator.GetSupportedMonitors();

        BuildControls();
        PositionAboveTray();

        Show();
        Activate();
    }

    private void BuildControls()
    {
        MonitorsPanel.Children.Clear();

        if (_monitors.Count == 0)
        {
            MonitorsPanel.Children.Add(new TextBlock
            {
                Text = "No DDC/CI monitors detected.\nEnsure DDC/CI is enabled in your monitor OSD.",
                Foreground = new SolidColorBrush(Color.FromRgb(160, 160, 160)),
                FontSize = 12,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4, 0, 4),
            });
            return;
        }

        for (int i = 0; i < _monitors.Count; i++)
        {
            if (i > 0)
                MonitorsPanel.Children.Add(new Border { Height = 14 });

            AddMonitorRow(_monitors[i]);
        }
    }

    private void AddMonitorRow(MonitorInfo monitor)
    {
        // Monitor name label
        MonitorsPanel.Children.Add(new TextBlock
        {
            Text = monitor.Name,
            Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
            FontSize = 11,
            Margin = new Thickness(0, 0, 0, 8),
            TextTrimming = TextTrimming.CharacterEllipsis,
        });

        // Row: sun icon · slider · percentage
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // Sun icon (☀ unicode)
        var icon = new TextBlock
        {
            Text = "☀",
            Foreground = new SolidColorBrush(Color.FromRgb(255, 200, 0)),
            FontSize = 14,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0),
        };

        var percentLabel = new TextBlock
        {
            Foreground = Brushes.White,
            FontSize = 12,
            Width = 36,
            TextAlignment = TextAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
        };

        void UpdateLabel(uint value) =>
            percentLabel.Text = $"{monitor.BrightnessPercent}%";

        var slider = new Slider
        {
            Minimum = monitor.MinBrightness,
            Maximum = monitor.MaxBrightness,
            Value = monitor.CurrentBrightness,
            SmallChange = 1,
            LargeChange = Math.Max(1, (monitor.MaxBrightness - monitor.MinBrightness) / 10),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 10, 0),
        };

        UpdateLabel(monitor.CurrentBrightness);

        // Debounce DDC/CI writes — sending every ValueChanged event would
        // flood the monitor firmware and cause visible lag.
        var captured = monitor;
        slider.ValueChanged += (_, args) =>
        {
            captured.CurrentBrightness = (uint)args.NewValue;
            percentLabel.Text = $"{captured.BrightnessPercent}%";

            _debounce?.Stop();
            _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(80) };
            _debounce.Tick += (_, _) =>
            {
                _debounce.Stop();
                MonitorEnumerator.SetBrightness(captured, (uint)slider.Value);
            };
            _debounce.Start();
        };

        Grid.SetColumn(icon, 0);
        Grid.SetColumn(slider, 1);
        Grid.SetColumn(percentLabel, 2);
        row.Children.Add(icon);
        row.Children.Add(slider);
        row.Children.Add(percentLabel);

        MonitorsPanel.Children.Add(row);
    }

    private void PositionAboveTray()
    {
        // Show offscreen to let WPF measure content size
        Left = -9999;
        Top = -9999;
        Show();
        UpdateLayout();
        Hide();

        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - ActualWidth - 12;
        Top = workArea.Bottom - ActualHeight - 12;
    }

    protected override void OnClosed(EventArgs e)
    {
        MonitorEnumerator.Release(_monitors);
        _monitors.Clear();
        base.OnClosed(e);
    }
}
