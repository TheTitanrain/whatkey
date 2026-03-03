using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Newtonsoft.Json;
using WhatKey.Models;

namespace WhatKey.Services
{
    public class HotkeysStorageService
    {
        private static readonly string DataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WhatKey");

        private static readonly string DataFile = Path.Combine(DataDir, "hotkeys.json");

        private HotkeysData _data = new HotkeysData();

        public AppSettings Settings => _data.Settings;
        public List<AppHotkeys> Apps => _data.Apps;

        public void Load()
        {
            if (!File.Exists(DataFile))
            {
                CreateDefaults();
                Save();
                return;
            }

            try
            {
                var json = File.ReadAllText(DataFile);
                _data = JsonConvert.DeserializeObject<HotkeysData>(json) ?? new HotkeysData();

                // Ensure Settings is not null after deserialization
                if (_data.Settings == null) _data.Settings = new AppSettings();
                if (_data.Apps == null) _data.Apps = new List<AppHotkeys>();
            }
            catch
            {
                CreateDefaults();
            }
        }

        public void Save()
        {
            Directory.CreateDirectory(DataDir);
            var json = JsonConvert.SerializeObject(_data, Formatting.Indented);
            File.WriteAllText(DataFile, json);
        }

        public List<HotkeyEntry> GetHotkeysForProcess(string processName)
        {
            if (string.IsNullOrEmpty(processName))
                return GetDefaultHotkeys();

            var lower = processName.ToLower();
            foreach (var app in _data.Apps)
            {
                if (string.Equals(app.ProcessName, lower, StringComparison.OrdinalIgnoreCase))
                    return new List<HotkeyEntry>(app.Hotkeys);
            }

            return GetDefaultHotkeys();
        }

        private List<HotkeyEntry> GetDefaultHotkeys()
        {
            foreach (var app in _data.Apps)
            {
                if (string.Equals(app.ProcessName, "default", StringComparison.OrdinalIgnoreCase))
                    return new List<HotkeyEntry>(app.Hotkeys);
            }
            return new List<HotkeyEntry>();
        }

        private void CreateDefaults()
        {
            _data = new HotkeysData
            {
                Settings = new AppSettings
                {
                    HoldKey = "LControlKey",
                    HoldDelayMs = 500,
                    ToggleHotkey = "Ctrl+Alt+H"
                },
                Apps = new List<AppHotkeys>
                {
                    new AppHotkeys
                    {
                        ProcessName = "code",
                        Title = "VS Code",
                        Hotkeys = new ObservableCollection<HotkeyEntry>
                        {
                            new HotkeyEntry { Keys = "Ctrl+P", Description = "Quick Open" },
                            new HotkeyEntry { Keys = "Ctrl+Shift+P", Description = "Command Palette" },
                            new HotkeyEntry { Keys = "Ctrl+`", Description = "Toggle Terminal" },
                            new HotkeyEntry { Keys = "Ctrl+B", Description = "Toggle Sidebar" },
                            new HotkeyEntry { Keys = "Ctrl+/", Description = "Toggle Comment" },
                            new HotkeyEntry { Keys = "Alt+Up/Down", Description = "Move Line Up/Down" },
                            new HotkeyEntry { Keys = "Ctrl+D", Description = "Select Next Occurrence" },
                            new HotkeyEntry { Keys = "F5", Description = "Start Debugging" },
                        }
                    },
                    new AppHotkeys
                    {
                        ProcessName = "chrome",
                        Title = "Google Chrome",
                        Hotkeys = new ObservableCollection<HotkeyEntry>
                        {
                            new HotkeyEntry { Keys = "Ctrl+T", Description = "New Tab" },
                            new HotkeyEntry { Keys = "Ctrl+W", Description = "Close Tab" },
                            new HotkeyEntry { Keys = "Ctrl+Tab", Description = "Next Tab" },
                            new HotkeyEntry { Keys = "Ctrl+L", Description = "Focus Address Bar" },
                            new HotkeyEntry { Keys = "Ctrl+Shift+T", Description = "Reopen Closed Tab" },
                            new HotkeyEntry { Keys = "Ctrl+F", Description = "Find in Page" },
                            new HotkeyEntry { Keys = "F5", Description = "Reload Page" },
                        }
                    }
                }
            };
        }
    }
}
