# WhatKey

Аналог macOS CheatSheet для Windows. Живёт в трее и показывает прозрачный оверлей с горячими клавишами для текущего активного окна.

## Как это работает

Зажмите и удерживайте **Left Ctrl** (~500 мс) — появится оверлей с хоткеями приложения, которое сейчас активно. Отпустите — оверлей исчезнет.

Также можно нажать **Ctrl+Alt+H** для переключения оверлея в фиксированном режиме.

## Возможности

- Оверлей появляется автоматически при удержании клавиши
- Оверлей скрывается при нажатии кнопки мыши во время удержания триггерной клавиши
- Состояние оверлея сбрасывается при блокировке экрана и сне системы — застрявший оверлей после разблокировки или пробуждения невозможен
- Список хоткеев определяется по имени процесса активного окна
- Один профиль может соответствовать нескольким именам процессов (например, 32- и 64-битные версии одного приложения)
- Длинные списки хоткеев разбиваются на до трёх колонок для компактного отображения
- Встроенный редактор для добавления приложений и хоткеев
- Кнопка «Detect App» автоматически определяет процесс целевого окна за 3 секунды
- Настраиваемая триггерная клавиша, задержка и toggle-хоткей
- Данные хранятся в `%APPDATA%\WhatKey\hotkeys.json`
- Проверяет наличие обновлений при запуске и уведомляет через всплывающее уведомление в трее; ручная проверка доступна в окне «О программе» и меню трея
- Запускается в трее, не мешает работе

## Встроенные профили

При первом запуске автоматически создаются профили для 18 приложений:

| Приложение | Процесс(ы) |
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

`processNames` — массив имён процессов в нижнем регистре, которые будут соответствовать этому профилю. Имя процесса можно узнать в Диспетчере задач или кнопкой «Detect App» в редакторе. В редакторе поле принимает список через запятую (например, `totalcmd, totalcmd64`).

Старые файлы с одиночным `"processName": "..."` автоматически мигрируются при загрузке.

Доступные значения `holdKey`: `LControlKey`, `RControlKey`, `LShiftKey`, `RShiftKey`, `LMenu`, `RMenu`, `LWin`, `RWin`.

Если при запуске `hotkeys.json` имеет неверный JSON-формат, приложение спрашивает, что делать: восстановить значения по умолчанию (с предварительным backup в timestamped `.bak`), открыть файл для ручного исправления или выйти.

## Стек

- .NET Framework 4.8 + WPF (MVVM)
- [Hardcodet.NotifyIcon.Wpf](https://github.com/hardcodet/wpf-notifyicon) — иконка в трее
- [Newtonsoft.Json](https://www.newtonsoft.com/json) — хранение данных
- Win32 API (P/Invoke) — глобальный хук клавиатуры, определение активного окна

