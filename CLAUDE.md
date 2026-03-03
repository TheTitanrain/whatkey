# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

```bash
# Build
dotnet build

# Run
dotnet run

# Build Release
dotnet build -c Release
```

Output binary: `bin/Debug/net48/WhatKey.exe` (requires Windows to run — uses Win32 P/Invoke).

There are no automated tests in this project.

## Architecture

WhatKey is a **tray-resident WPF app** targeting .NET Framework 4.8. There is no main window — the entry point is `App.xaml.cs`, which creates all services and windows on startup and keeps them alive for the lifetime of the process.

### Data flow (trigger → overlay)

```
KeyboardHookService (WH_KEYBOARD_LL hook or WM_HOTKEY)
  → TriggerShow / TriggerHide events
    → App.xaml.cs handler
      → ActiveWindowService.GetActiveProcessName()
      → HotkeysStorageService.GetHotkeysForProcess(name)
      → OverlayWindow.ShowWithHotkeys(hotkeys, name)
```

### Services (no DI framework — wired manually in App.xaml.cs)

- **`KeyboardHookService`** — installs a `WH_KEYBOARD_LL` low-level hook for hold-key detection (DispatcherTimer), and registers a system hotkey via `RegisterHotKey` on a message-only `HwndSource` (HWND_MESSAGE parent) for toggle mode. Both paths fire `TriggerShow`/`TriggerHide` events. Must call `Install()` after construction. Must be `Dispose()`d on exit to unregister the hook and hotkey.

- **`ActiveWindowService`** — thin P/Invoke wrapper: `GetForegroundWindow` → `GetWindowThreadProcessId` → `Process.GetProcessById`. Returns process name lowercased.

- **`HotkeysStorageService`** — reads/writes `%APPDATA%\WhatKey\hotkeys.json` via Newtonsoft.Json. Call `Load()` on startup, `Save()` after edits. `GetHotkeysForProcess(name)` matches by `ProcessName` (case-insensitive); falls back to the entry with `ProcessName = "default"` if no match.

### Windows

- **`OverlayWindow`** — `WindowStyle=None`, `AllowsTransparency=True`, `Topmost=True`, `ShowInTaskbar=False`. Call `ShowWithHotkeys()` / `HideOverlay()` — these handle fade animations (150ms `DoubleAnimation` on `OpacityProperty`) and recentering on the primary screen.

- **`EditorWindow`** — two-panel layout (ListBox of apps left, DataGrid of hotkeys right). Closing is intercepted to `Hide()` instead of closing, so the app stays in the tray. Opened via tray menu or double-click. **Detect App** button starts a 3-second DispatcherTimer countdown; after it fires, `GetActiveProcessName()` captures whatever is in the foreground.

- **`AboutWindow`** — `WindowStyle=None`, `AllowsTransparency=True`, `ShowInTaskbar=False`, `ResizeMode=NoResize`. Centered on screen, draggable via title-bar `MouseDown` → `DragMove()`. Unlike `EditorWindow`, it truly closes (not hides) on dismiss — `App.xaml.cs` nulls `_aboutWindow` in the `Closed` handler so a fresh instance is created on next open. Opened via tray context menu "About".

### Models & JSON format

```json
{
  "settings": { "holdKey": "LControlKey", "holdDelayMs": 500, "toggleHotkey": "Ctrl+Alt+H" },
  "apps": [
    { "processName": "code", "title": "VS Code", "hotkeys": [{ "keys": "Ctrl+P", "description": "Quick Open" }] }
  ]
}
```

`AppHotkeys.Hotkeys` is `ObservableCollection<HotkeyEntry>` so the DataGrid updates live.

### Hold-key VK codes (AppSettings.HoldKey values)

`LControlKey` (0xA2), `RControlKey` (0xA3), `LShiftKey` (0xA0), `RShiftKey` (0xA1), `LMenu` (0xA4), `RMenu` (0xA5), `LWin` (0x5B), `RWin` (0x5C).

### Toggle hotkey format (AppSettings.ToggleHotkey)

`"Ctrl+Alt+H"` — parsed by splitting on `+`; supports modifiers `Ctrl`, `Alt`, `Shift`, `Win` and a single letter key.

## Key constraints

- **net48 only** — targets .NET Framework 4.8 for Windows 7 SP1+ compatibility. Do not switch to `net8`/`net9`.
- **No multi-monitor awareness** — overlay centers on primary screen (`SystemParameters.PrimaryScreenWidth/Height`).
- **`_hookProc` delegate must be kept as a field** in `KeyboardHookService` to prevent GC collection while the hook is active.
- **`ShutdownMode=OnExplicitShutdown`** — the app never closes on its own; only the "Exit" tray item calls `Application.Shutdown()`.
