using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows;
using System.Windows.Controls;
using LumaTray.Views;
using H.NotifyIcon;
using H.NotifyIcon.Core;

namespace LumaTray.Services;

internal sealed class TrayIconService : IDisposable
{
    private readonly TaskbarIcon _trayIcon;
    private BrightnessPopup? _popup;
    private IntPtr _iconHandle;

    internal TrayIconService()
    {
        _trayIcon = new TaskbarIcon
        {
            Icon = CreateSunIcon(),
            ToolTipText = "LumaTray",
            MenuActivation = PopupActivationMode.RightClick,
            ContextMenu = BuildContextMenu(),
        };
        _trayIcon.TrayLeftMouseDown += OnTrayLeftClick;
        _trayIcon.ForceCreate();
    }

    private void OnTrayLeftClick(object sender, RoutedEventArgs e)
    {
        if (_popup is { IsVisible: true })
        {
            _popup.Hide();
            return;
        }

        _popup ??= new BrightnessPopup();
        _popup.RefreshAndShow();
    }

    private ContextMenu BuildContextMenu()
    {
        var menu = new ContextMenu();

        var startupItem = new MenuItem
        {
            Header = "Run at startup",
            IsCheckable = true,
            IsChecked = StartupService.IsEnabled(),
        };
        startupItem.Click += (_, _) => StartupService.SetEnabled(startupItem.IsChecked);

        var exitItem = new MenuItem { Header = "Exit" };
        exitItem.Click += (_, _) =>
        {
            Dispose();
            Application.Current.Shutdown();
        };

        menu.Items.Add(startupItem);
        menu.Items.Add(new Separator());
        menu.Items.Add(exitItem);
        return menu;
    }

    private Icon CreateSunIcon()
    {
        const int size = 32;
        using var bmp = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            var gold = Color.FromArgb(255, 255, 200, 0);
            using var brush = new SolidBrush(gold);
            using var pen = new Pen(gold, 2.2f);

            // Center circle
            g.FillEllipse(brush, 9, 9, 14, 14);

            // 8 rays
            float cx = 16f, cy = 16f;
            for (int i = 0; i < 8; i++)
            {
                double angle = i * 45 * Math.PI / 180;
                float x1 = cx + (float)(10.5 * Math.Cos(angle));
                float y1 = cy + (float)(10.5 * Math.Sin(angle));
                float x2 = cx + (float)(14.5 * Math.Cos(angle));
                float y2 = cy + (float)(14.5 * Math.Sin(angle));
                g.DrawLine(pen, x1, y1, x2, y2);
            }
        }

        _iconHandle = bmp.GetHicon();
        return Icon.FromHandle(_iconHandle);
    }

    public void Dispose()
    {
        _popup?.Close();
        _trayIcon.Dispose();

        if (_iconHandle != IntPtr.Zero)
        {
            Interop.NativeMethods.DestroyIcon(_iconHandle);
            _iconHandle = IntPtr.Zero;
        }
    }
}
