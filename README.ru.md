# WhatKey

Аналог macOS CheatSheet для Windows. Живёт в трее и показывает прозрачный оверлей с горячими клавишами для текущего активного окна.

## Как это работает

Зажмите и удерживайте **Left Ctrl** (~500 мс) — появится оверлей с хоткеями приложения, которое сейчас активно. Отпустите — оверлей исчезнет.

Также можно нажать **Ctrl+Alt+H** для переключения оверлея в фиксированном режиме.

## Возможности

- Оверлей появляется автоматически при удержании клавиши
- Список хоткеев определяется по имени процесса активного окна
- Встроенный редактор для добавления приложений и хоткеев
- Кнопка «Detect App» автоматически определяет процесс целевого окна за 3 секунды
- Настраиваемая триггерная клавиша, задержка и toggle-хоткей
- Данные хранятся в `%APPDATA%\WhatKey\hotkeys.json`
- Запускается в трее, не мешает работе

## Встроенные профили

При первом запуске автоматически создаются профили для 16 приложений:

| Приложение | Процесс |
|---|---|
| VS Code | `code` |
| Google Chrome | `chrome` |
| Firefox | `firefox` |
| Microsoft Edge | `msedge` |
| Windows Explorer | `explorer` |
| Notepad++ | `notepad++` |
| Microsoft Word | `winword` |
| Microsoft Excel | `excel` |
| Windows Terminal | `windowsterminal` |
| Slack | `slack` |
| Telegram | `telegram` |
| VLC Media Player | `vlc` |
| JetBrains Rider | `rider64` |
| IntelliJ IDEA | `idea64` |
| Figma | `figma` |
| Obsidian | `obsidian` |

## Требования

- Windows 7 SP1 и выше
- [.NET Framework 4.8](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net48)

## Сборка

```bash
dotnet build
dotnet build -c Release
```

Бинарник: `bin/Release/net48/WhatKey.exe`

## Настройка хоткеев

Открыть редактор: двойной клик по иконке в трее или «Edit Hotkeys» в контекстном меню.

Данные хранятся в `%APPDATA%\WhatKey\hotkeys.json`. Формат файла:

```json
{
  "settings": {
    "holdKey": "LControlKey",
    "holdDelayMs": 500,
    "toggleHotkey": "Ctrl+Alt+H"
  },
  "apps": [
    {
      "processName": "code",
      "title": "VS Code",
      "hotkeys": [
        { "keys": "Ctrl+P", "description": "Quick Open" },
        { "keys": "Ctrl+Shift+P", "description": "Command Palette" }
      ]
    }
  ]
}
```

`processName` — имя процесса в нижнем регистре (узнать в Диспетчере задач или кнопкой «Detect App» в редакторе).

Доступные значения `holdKey`: `LControlKey`, `RControlKey`, `LShiftKey`, `RShiftKey`, `LMenu`, `RMenu`, `LWin`, `RWin`.

Если при запуске `hotkeys.json` имеет неверный JSON-формат, приложение спрашивает, что делать: восстановить значения по умолчанию (с предварительным backup в timestamped `.bak`), открыть файл для ручного исправления или выйти.

## Стек

- .NET Framework 4.8 + WPF (MVVM)
- [Hardcodet.NotifyIcon.Wpf](https://github.com/hardcodet/wpf-notifyicon) — иконка в трее
- [Newtonsoft.Json](https://www.newtonsoft.com/json) — хранение данных
- Win32 API (P/Invoke) — глобальный хук клавиатуры, определение активного окна

