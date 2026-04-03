# LumaTray

![Build Status](https://github.com/MateuszPeczek/LumaTray/actions/workflows/dotnet-desktop.yml/badge.svg?branch=main)

A lightweight Windows system tray application for controlling the brightness of external monitors connected to a laptop — without touching the physical buttons on the monitor.

## Download

Grab the latest build from the [Releases](https://github.com/MateuszPeczek/LumaTray/releases/latest) page.

## Purpose

Laptop users with external monitors typically have no convenient way to adjust display brightness from the OS. LumaTray sits in the system tray and exposes a clean slider per monitor, updated in real time via the DDC/CI protocol.

## Features

- **System tray icon** — left-click opens the brightness popup, right-click shows the context menu
- **Per-monitor sliders** — one slider per DDC/CI-capable external monitor with live percentage readout
- **Global hotkeys** — adjust brightness of all monitors via customizable keyboard shortcuts (default: `Ctrl+Alt+Up` / `Ctrl+Alt+Down`)
- **Hotkey configuration** — right-click → "Hotkey settings…" to record any key combination using Record/Stop buttons; works with standard keyboards, sim racing wheels (e.g. Moza rotary encoders), Stream Deck, and other peripherals
- **On-screen display** — a compact OSD overlay shows the current brightness level whenever a hotkey is pressed
- **Live slider sync** — popup sliders update in real time when brightness is changed via hotkeys
- **DDC/CI over Win32** — communicates directly with monitor firmware via `Dxva2.dll` (no third-party brightness drivers required)
- **Debounced writes** — slider changes are batched to avoid flooding monitor firmware
- **Run at startup** — optional Windows Registry autostart toggle in the context menu
- **Zero tray clutter** — no installer, no background services, single self-contained executable

## Requirements

- Windows 10/11 (x64)
- .NET 8 runtime
- External monitor with DDC/CI enabled (check your monitor OSD settings)

## How to use

1. Run `LumaTray.exe`
2. A sun icon appears in the system tray
3. Left-click the icon to open the brightness popup
4. Adjust the slider for each detected external monitor
5. Optionally enable **Run at startup** via right-click context menu
6. Right-click → **Hotkey settings…** to configure keyboard shortcuts for brightness control

If no monitors appear, make sure DDC/CI is enabled in your monitor's on-screen display settings.

### Hotkey configuration

1. Open **Hotkey settings…** from the tray context menu (global hotkeys are paused while the window is open)
2. Click **Record** next to "Brightness Up" or "Brightness Down"
3. Press any key combination — keys accumulate as you press them and are shown in real time
4. Click **Stop** to finish recording, or press **Save** to commit in-progress recordings
5. Adjust the **Step (%)** slider to control how much brightness changes per hotkey press (1–25%)
6. Click **Save** to apply

> **Note:** Windows requires at least one non-modifier key (e.g. arrow key, letter, F-key, or peripheral button). Modifier-only combos like `Ctrl + Alt` alone are not supported.

### Using with sim racing wheels / peripherals

Map your device's button or rotary encoder to the configured hotkey in your peripheral's software (e.g. Moza Pit House). The rotary encoder CW/CCW clicks will trigger brightness up/down.

## Tech stack

| Component | Technology |
|---|---|
| Framework | .NET 8 / WPF |
| Target | Windows x64 |
| Tray icon | H.NotifyIcon.Wpf 2.1.0 |
| Brightness API | Win32 `Dxva2.dll` (DDC/CI) |
| Global hotkeys | Win32 `RegisterHotKey` / `UnregisterHotKey` |
| Settings storage | JSON file (`hotkey-settings.json`) |
| Monitor enumeration | Win32 `user32.dll` |
| Startup toggle | Windows Registry (`HKCU\...\Run`) |

## AI-generated codebase

Every line of code in this project was written by AI (Claude). No human-authored code is present. This project serves as an example of a fully AI-generated, functional Windows desktop utility.

## License

MIT
