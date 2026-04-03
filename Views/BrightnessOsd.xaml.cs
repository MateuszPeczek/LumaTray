using System.Windows;
using System.Windows.Threading;

namespace LumaTray.Views;

public partial class BrightnessOsd : Window
{
    private readonly DispatcherTimer _hideTimer;

    internal BrightnessOsd()
    {
        InitializeComponent();

        _hideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
        _hideTimer.Tick += (_, _) =>
        {
            _hideTimer.Stop();
            Hide();
        };

        // Prevent the OSD from stealing focus
        Loaded += (_, _) =>
        {
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            const int GWL_EXSTYLE = -20;
            const int WS_EX_NOACTIVATE = 0x08000000;
            const int WS_EX_TOOLWINDOW = 0x00000080;
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW);
        };
    }

    internal void ShowBrightness(int percent)
    {
        PercentLabel.Text = $"{percent}%";

        // Update fill bar width relative to the track (parent border)
        FillBar.Width = double.NaN; // reset
        Dispatcher.InvokeAsync(() =>
        {
            var trackParent = (FrameworkElement)FillBar.Parent;
            FillBar.Width = trackParent.ActualWidth * Math.Clamp(percent, 0, 100) / 100.0;
        }, DispatcherPriority.Loaded);

        PositionBottomCenter();

        _hideTimer.Stop();
        Show();
        _hideTimer.Start();
    }

    private void PositionBottomCenter()
    {
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Left + (workArea.Width - Width) / 2;
        Top = workArea.Bottom - 80;
    }

    protected override void OnMouseDown(System.Windows.Input.MouseButtonEventArgs e)
    {
        // Clicking the OSD hides it immediately
        _hideTimer.Stop();
        Hide();
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
}
