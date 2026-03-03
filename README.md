# WhatKey

Аналог CheatSheet для Windows. Приложение живёт в трее и показывает прозрачный оверлей с горячими клавишами для текущего активного окна.

## Как это работает

Зажмите и удерживайте **Left Ctrl** (~500 мс) — появится оверлей с хоткеями приложения, которое сейчас активно. Отпустите — оверлей исчезнет.

Также можно нажать **Ctrl+Alt+H** для переключения оверлея в фиксированном режиме.

![overlay mockup](https://placehold.co/500x300/1e1e2e/cdd6f4?text=WhatKey+Overlay)

## Возможности

- Оверлей появляется автоматически при удержании клавиши
- Список хоткеев определяется по имени процесса активного окна
- Встроенный редактор для добавления приложений и хоткеев
- Кнопка «Detect» в редакторе автоматически определяет процесс целевого окна
- Настраиваемая триггерная клавиша, задержка и toggle-хоткей
- Данные хранятся в `%APPDATA%\WhatKey\hotkeys.json`
- Запускается в трее, не мешает работе

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

При первом запуске создаётся файл `%APPDATA%\WhatKey\hotkeys.json` с примерами для VS Code и Chrome.

Открыть редактор: двойной клик по иконке в трее или «Edit Hotkeys» в контекстном меню.

Формат файла:

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

`processName` — имя процесса в нижнем регистре (можно узнать в Диспетчере задач или кнопкой «Detect» в редакторе).

Доступные значения `holdKey`: `LControlKey`, `RControlKey`, `LShiftKey`, `RShiftKey`, `LMenu`, `RMenu`, `LWin`, `RWin`.

## Стек

- .NET Framework 4.8 + WPF (MVVM)
- [Hardcodet.NotifyIcon.Wpf](https://github.com/hardcodet/wpf-notifyicon) — иконка в трее
- [Newtonsoft.Json](https://www.newtonsoft.com/json) — хранение данных
- Win32 API (P/Invoke) — глобальный хук клавиатуры, определение активного окна
