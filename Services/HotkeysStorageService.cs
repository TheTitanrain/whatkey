using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using WhatKey.Models;

namespace WhatKey.Services
{
    public class HotkeysStorageService
    {
        public static readonly string DataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WhatKey");

        private readonly string _dataDir;
        private readonly string _dataFile;

        private HotkeysData _data = new HotkeysData();

        public string DataFilePath => _dataFile;
        public AppSettings Settings => _data.Settings;
        public List<AppHotkeys> Apps => _data.Apps;

        public HotkeysStorageService(string dataDir = null)
        {
            _dataDir = string.IsNullOrWhiteSpace(dataDir) ? DataDir : dataDir;
            _dataFile = Path.Combine(_dataDir, "hotkeys.json");
        }

        public HotkeysLoadResult Load()
        {
            if (!File.Exists(_dataFile))
            {
                LoadDefaultsAndSave();
                return HotkeysLoadResult.MissingFile(_dataFile);
            }

            try
            {
                var json = File.ReadAllText(_dataFile);
                _data = JsonConvert.DeserializeObject<HotkeysData>(json) ?? new HotkeysData();
                NormalizeData();
                return HotkeysLoadResult.Ok(_dataFile);
            }
            catch (Exception ex)
            {
                return HotkeysLoadResult.Invalid(_dataFile, ex.Message);
            }
        }

        public string CreateBackupOfDataFile()
        {
            if (!File.Exists(_dataFile))
                throw new FileNotFoundException("hotkeys.json was not found.", _dataFile);

            Directory.CreateDirectory(_dataDir);

            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var backupPath = Path.Combine(_dataDir, string.Format("hotkeys.{0}.json.bak", timestamp));
            File.Copy(_dataFile, backupPath, false);
            return backupPath;
        }

        public void LoadDefaultsAndSave()
        {
            LoadDefaults();
            Save();
        }

        public void Save()
        {
            Directory.CreateDirectory(_dataDir);
            var json = JsonConvert.SerializeObject(_data, Formatting.Indented);
            File.WriteAllText(_dataFile, json);
        }

        public List<HotkeyEntry> GetHotkeysForProcess(string processName)
        {
            if (string.IsNullOrEmpty(processName))
                return GetDefaultHotkeys();

            var lower = processName.ToLower();
            foreach (var app in _data.Apps)
            {
                if (string.Equals(app.ProcessName, lower, StringComparison.OrdinalIgnoreCase))
                    return new List<HotkeyEntry>(app.Hotkeys ?? new ObservableCollection<HotkeyEntry>());
            }

            return GetDefaultHotkeys();
        }

        private List<HotkeyEntry> GetDefaultHotkeys()
        {
            foreach (var app in _data.Apps)
            {
                if (string.Equals(app.ProcessName, "default", StringComparison.OrdinalIgnoreCase))
                    return new List<HotkeyEntry>(app.Hotkeys ?? new ObservableCollection<HotkeyEntry>());
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

            NormalizeData();
        }

        private void NormalizeData()
        {
            if (_data.Settings == null) _data.Settings = new AppSettings();
            if (_data.Apps == null) _data.Apps = new List<AppHotkeys>();

            foreach (var app in _data.Apps)
            {
                if (app.Hotkeys == null)
                    app.Hotkeys = new ObservableCollection<HotkeyEntry>();
            }
        }
    }
}
