using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
                LoadDefaults();
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
                LoadDefaults();
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

        private void LoadDefaults()
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream("WhatKey.Assets.hotkeys.defaults.json"))
            using (var reader = new StreamReader(stream))
            {
                _data = JsonConvert.DeserializeObject<HotkeysData>(reader.ReadToEnd()) ?? new HotkeysData();
            }
            Save();
        }
    }
}
