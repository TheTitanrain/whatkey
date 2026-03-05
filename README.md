# WhatKey

Russian version: [README.ru.md](README.ru.md)

A Windows equivalent of macOS CheatSheet. It runs in the system tray and shows a transparent hotkey overlay for the currently active window.

## How it works

Press and hold **Left Ctrl** for about 500 ms to show the hotkeys for the active app. Release the key to hide the overlay.

You can also press **Ctrl+Alt+H** to toggle the overlay in pinned mode.

## Features

- Overlay appears automatically while the trigger key is held
- Hotkey list is selected by the active window process name
- One app entry can match multiple process names (e.g. 32-bit and 64-bit variants of the same app)
- Long hotkey lists are split into up to three columns to keep the overlay compact
- Built-in editor for apps and hotkeys
- `Detect App` button auto-detects the target process within 3 seconds
- Configurable trigger key, hold delay, and toggle hotkey
- Data is stored in `%APPDATA%\WhatKey\hotkeys.json`
- Runs in the tray and stays out of the way

## Built-in profiles

On first launch, profiles for 18 apps are created automatically:

| Application | Process(es) |
|---|---|
| VS Code | `code` |
| Google Chrome | `chrome` |
| Firefox | `firefox` |
| Microsoft Edge | `msedge` |
| Windows Explorer | `explorer` |
| Notepad++ | `notepad++` |
| Microsoft Word | `winword` |
| Microsoft Excel | `excel` |
| Microsoft PowerPoint | `powerpnt` |
| Adobe Photoshop | `photoshop` |
| Windows Terminal | `windowsterminal` |
| Slack | `slack` |
| Telegram | `telegram` |
| VLC Media Player | `vlc` |
| IntelliJ IDEA | `idea64` |
| Figma | `figma` |
| Obsidian | `obsidian` |
| Total Commander | `totalcmd`, `totalcmd64` |

## Requirements

- Windows 7 SP1 or later
- [.NET Framework 4.8](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48)

## Build

```bash
dotnet build
dotnet build -c Release
```

Binary: `bin/Release/net48/WhatKey.exe`

## Hotkey configuration

Open the editor by double-clicking the tray icon or using `Edit Hotkeys` in the context menu.

Data is stored in `%APPDATA%\WhatKey\hotkeys.json`. File format:

```json
{
  "settings": {
    "holdKey": "LControlKey",
    "holdDelayMs": 500,
    "toggleHotkey": "Ctrl+Alt+H"
  },
  "apps": [
    {
      "processNames": ["code"],
      "title": "VS Code",
      "hotkeys": [
        { "keys": "Ctrl+P", "description": "Quick Open" },
        { "keys": "Ctrl+Shift+P", "description": "Command Palette" }
      ]
    },
    {
      "processNames": ["totalcmd", "totalcmd64"],
      "title": "Total Commander",
      "hotkeys": [
        { "keys": "F5", "description": "Copy File(s)" }
      ]
    }
  ]
}
```

`processNames` is an array of lowercase process names that will all match this entry. Use `Detect App` in the editor or check Task Manager to find the process name. In the editor the field accepts a comma-separated list (e.g. `totalcmd, totalcmd64`).

Old files with a single `"processName": "..."` string are migrated automatically on load.

Available values for `holdKey`: `LControlKey`, `RControlKey`, `LShiftKey`, `RShiftKey`, `LMenu`, `RMenu`, `LWin`, `RWin`.

When you change `holdKey`, `holdDelayMs`, or `toggleHotkey` in the editor and click Save, the running app applies these values immediately. Restart is not required.

If `hotkeys.json` has invalid JSON format on startup, the app asks what to do: restore defaults (with a timestamped `.bak` backup first), open the file for manual fix, or exit.

## Tech stack

- .NET Framework 4.8 + WPF (MVVM)
- [Hardcodet.NotifyIcon.Wpf](https://github.com/hardcodet/wpf-notifyicon) for the tray icon
- [Newtonsoft.Json](https://www.newtonsoft.com/json) for data storage
- Win32 API (P/Invoke) for global keyboard hook and active window detection


