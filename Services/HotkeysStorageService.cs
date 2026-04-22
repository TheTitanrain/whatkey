using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using WhatKey.Models;

namespace WhatKey.Services
{
    public class HotkeysStorageService
    {
        private static readonly JsonSerializerOptions _readOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

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
                _data = JsonSerializer.Deserialize<HotkeysData>(json, _readOptions) ?? new HotkeysData();
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
            var previous = _data;
            LoadDefaults();
            try
            {
                Save();
            }
            catch
            {
                _data = previous;
                throw;
            }
        }

        public void Save()
        {
            Directory.CreateDirectory(_dataDir);
            var json = JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_dataFile, json);
        }

        public List<HotkeyGroup> GetGroupsForProcess(string processName)
        {
            if (string.IsNullOrEmpty(processName))
                return GetDefaultGroups();

            var lower = processName.ToLower();
            foreach (var app in _data.Apps)
            {
                foreach (var name in app.ProcessNames)
                {
                    if (string.Equals(name, lower, StringComparison.OrdinalIgnoreCase))
                        return new List<HotkeyGroup>(app.Groups ?? new ObservableCollection<HotkeyGroup>());
                }
            }

            return GetDefaultGroups();
        }

        public List<HotkeyGroup> GetSystemGroups()
        {
            foreach (var app in _data.Apps)
            {
                if (app.ProcessNames.Contains("system", StringComparer.OrdinalIgnoreCase))
                    return new List<HotkeyGroup>(app.Groups ?? new ObservableCollection<HotkeyGroup>());
            }
            return new List<HotkeyGroup>();
        }

        public List<HotkeyEntry> GetHotkeysForProcess(string processName)
        {
            return GetGroupsForProcess(processName)
                .SelectMany(g => g.Hotkeys ?? Enumerable.Empty<HotkeyEntry>())
                .ToList();
        }

        private List<HotkeyGroup> GetDefaultGroups()
        {
            foreach (var app in _data.Apps)
            {
                if (app.ProcessNames.Contains("default", StringComparer.OrdinalIgnoreCase))
                    return new List<HotkeyGroup>(app.Groups ?? new ObservableCollection<HotkeyGroup>());
            }
            return new List<HotkeyGroup>();
        }

        private void LoadDefaults()
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream("WhatKey.Assets.hotkeys.defaults.json"))
            using (var reader = new StreamReader(stream))
            {
                _data = JsonSerializer.Deserialize<HotkeysData>(reader.ReadToEnd(), _readOptions) ?? new HotkeysData();
            }

            NormalizeData();
        }

        private void NormalizeData()
        {
            if (_data.Settings == null) _data.Settings = new AppSettings();
            if (_data.Apps == null) _data.Apps = new List<AppHotkeys>();

            foreach (var app in _data.Apps)
            {
                if (app.ProcessNames == null) app.ProcessNames = new List<string>();
                if (app.ProcessNames.Count == 0 && !string.IsNullOrWhiteSpace(app.ProcessName))
                    app.ProcessNames.Add(app.ProcessName.ToLower());
                app.ProcessNames = app.ProcessNames.Select(p => p.ToLower()).ToList();
                app.ProcessName = null;

                if (app.Groups == null) app.Groups = new ObservableCollection<HotkeyGroup>();
                if (app.Hotkeys != null && app.Hotkeys.Count > 0)
                    app.Groups.Add(new HotkeyGroup { Name = "General", Hotkeys = app.Hotkeys });
                app.Hotkeys = null;
            }
        }
    }
}
