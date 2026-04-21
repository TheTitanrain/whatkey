using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;
using WhatKey.Models;
using WhatKey.Services;

namespace WhatKey.ViewModels
{
    public class EditorViewModel : BaseViewModel
    {
        private readonly HotkeysStorageService _storage;
        private readonly IActiveWindowService _activeWindow;

        private ObservableCollection<AppHotkeys> _apps;
        private AppHotkeys _selectedApp;
        private string _processNamesRaw;
        private HotkeyEntry _selectedHotkey;
        private ObservableCollection<HotkeyGroup> _groups;
        private HotkeyGroup _selectedGroup;
        private AppSettings _settings;
        private string _detectButtonText = "Detect";
        private bool _isDetecting;

        private DispatcherTimer _detectTimer;
        private int _detectCountdown;

        public ObservableCollection<AppHotkeys> Apps
        {
            get => _apps;
            set => SetField(ref _apps, value);
        }

        public AppHotkeys SelectedApp
        {
            get => _selectedApp;
            set
            {
                FlushProcessNamesText();
                if (SetField(ref _selectedApp, value))
                {
                    _processNamesRaw = value != null ? string.Join(", ", value.ProcessNames) : "";
                    OnPropertyChanged(nameof(ProcessNamesText));
                    Groups = value?.Groups;
                    SelectedGroup = _groups?.FirstOrDefault();
                    SelectedHotkey = null;
                }
            }
        }

        public string ProcessNamesText
        {
            get => _processNamesRaw ?? string.Join(", ", _selectedApp?.ProcessNames ?? Enumerable.Empty<string>());
            set
            {
                _processNamesRaw = value ?? "";
                OnPropertyChanged();
            }
        }

        private void FlushProcessNamesText()
        {
            if (_selectedApp == null || _processNamesRaw == null) return;
            _selectedApp.ProcessNames = _processNamesRaw
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim().ToLower())
                .Where(p => p.Length > 0)
                .ToList();
        }

        public HotkeyEntry SelectedHotkey
        {
            get => _selectedHotkey;
            set => SetField(ref _selectedHotkey, value);
        }

        public ObservableCollection<HotkeyGroup> Groups
        {
            get => _groups;
            set => SetField(ref _groups, value);
        }

        public HotkeyGroup SelectedGroup
        {
            get => _selectedGroup;
            set => SetField(ref _selectedGroup, value);
        }

        public AppSettings Settings
        {
            get => _settings;
            set => SetField(ref _settings, value);
        }

        public string DetectButtonText
        {
            get => _detectButtonText;
            set => SetField(ref _detectButtonText, value);
        }

        public bool IsDetecting
        {
            get => _isDetecting;
            set => SetField(ref _isDetecting, value);
        }

        public ICommand AddAppCommand { get; }
        public ICommand RemoveAppCommand { get; }
        public ICommand DetectAppCommand { get; }
        public ICommand AddGroupCommand { get; }
        public ICommand RemoveGroupCommand { get; }
        public ICommand AddHotkeyCommand { get; }
        public ICommand RemoveHotkeyCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand OpenFolderCommand { get; }
        public ICommand RestoreDefaultsCommand { get; }

        public event EventHandler<AppSettings> SettingsSaved;

        public EditorViewModel(HotkeysStorageService storage, IActiveWindowService activeWindow)
        {
            _storage = storage;
            _activeWindow = activeWindow;

            Apps = new ObservableCollection<AppHotkeys>(_storage.Apps);
            Settings = _storage.Settings;

            AddAppCommand = new RelayCommand(AddApp);
            RemoveAppCommand = new RelayCommand(RemoveApp, () => SelectedApp != null);
            DetectAppCommand = new RelayCommand(StartDetect, () => !IsDetecting);
            AddGroupCommand = new RelayCommand(AddGroup, () => SelectedApp != null);
            RemoveGroupCommand = new RelayCommand(RemoveGroup, () => SelectedApp != null && SelectedGroup != null);
            AddHotkeyCommand = new RelayCommand(AddHotkey, () => SelectedApp != null && SelectedGroup != null);
            RemoveHotkeyCommand = new RelayCommand(RemoveHotkey, () => SelectedApp != null && SelectedHotkey != null && SelectedGroup != null);
            SaveCommand = new RelayCommand(Save);
            OpenFolderCommand = new RelayCommand(OpenFolder);
            RestoreDefaultsCommand = new RelayCommand(RestoreDefaults);
        }

        private void AddApp()
        {
            var app = new AppHotkeys
            {
                ProcessNames = new List<string> { "newapp" },
                Title = "New Application",
                Groups = new ObservableCollection<HotkeyGroup> { new HotkeyGroup { Name = "General" } },
                Hotkeys = null
            };
            Apps.Add(app);
            _storage.Apps.Add(app);
            SelectedApp = app;
        }

        private void RemoveApp()
        {
            if (SelectedApp == null) return;
            _storage.Apps.Remove(SelectedApp);
            Apps.Remove(SelectedApp);
            SelectedApp = null;
        }

        private void StartDetect()
        {
            if (_isDetecting) return;

            IsDetecting = true;
            _detectCountdown = 3;
            DetectButtonText = "Switching... 3";

            _detectTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _detectTimer.Tick += DetectTimerTick;
            _detectTimer.Start();
        }

        private void DetectTimerTick(object sender, EventArgs e)
        {
            _detectCountdown--;
            if (_detectCountdown > 0)
            {
                DetectButtonText = $"Switching... {_detectCountdown}";
                return;
            }

            _detectTimer.Stop();
            _detectTimer = null;

            var processName = _activeWindow.GetActiveProcessName();

            // Check if we already have this app
            foreach (var app in Apps)
            {
                if (app.ProcessNames.Any(n => string.Equals(n, processName, StringComparison.OrdinalIgnoreCase)))
                {
                    SelectedApp = app;
                    DetectButtonText = "Detect";
                    IsDetecting = false;
                    return;
                }
            }

            // Create new entry
            var newApp = new AppHotkeys
            {
                ProcessNames = new List<string> { processName },
                Title = processName
            };
            Apps.Add(newApp);
            _storage.Apps.Add(newApp);
            SelectedApp = newApp;

            DetectButtonText = "Detect";
            IsDetecting = false;
        }

        private void AddGroup()
        {
            if (SelectedApp == null) return;
            var group = new HotkeyGroup { Name = "New Group" };
            SelectedApp.Groups.Add(group);
            SelectedGroup = group;
        }

        private void RemoveGroup()
        {
            if (SelectedApp == null || SelectedGroup == null) return;
            SelectedApp.Groups.Remove(SelectedGroup);
            SelectedGroup = SelectedApp.Groups.FirstOrDefault();
        }

        private void AddHotkey()
        {
            if (SelectedApp == null || SelectedGroup == null) return;

            var entry = new HotkeyEntry { Keys = "Ctrl+?", Description = "New hotkey" };
            SelectedGroup.Hotkeys.Add(entry);
            SelectedHotkey = entry;
        }

        private void RemoveHotkey()
        {
            if (SelectedApp == null || SelectedHotkey == null || SelectedGroup == null) return;
            SelectedGroup.Hotkeys.Remove(SelectedHotkey);
            SelectedHotkey = null;
        }

        private void OpenFolder()
        {
            Process.Start("explorer.exe", HotkeysStorageService.DataDir);
        }

        private void RestoreDefaults()
        {
            var result = System.Windows.MessageBox.Show(
                "This will replace all hotkeys and settings with the built-in defaults.\nContinue?",
                "Restore Defaults",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result != System.Windows.MessageBoxResult.Yes)
                return;

            var previousApps = CloneApps(_storage.Apps);

            try
            {
                _storage.LoadDefaultsAndSave();

                Apps = new ObservableCollection<AppHotkeys>(_storage.Apps);
                Settings = _storage.Settings;

                SettingsSaved?.Invoke(this, _storage.Settings);
            }
            catch (Exception ex)
            {
                _storage.Apps.Clear();
                foreach (var app in previousApps)
                    _storage.Apps.Add(app);

                Apps = new ObservableCollection<AppHotkeys>(_storage.Apps);
                Settings = _storage.Settings;

                System.Windows.MessageBox.Show(
                    "Restore Defaults failed:\n" + ex.Message,
                    "Restore Defaults",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        private void Save()
        {
            FlushProcessNamesText();
            var previousApps = CloneApps(_storage.Apps);
            var newApps = CloneApps(Apps);

            _storage.Apps.Clear();
            foreach (var app in newApps)
                _storage.Apps.Add(app);

            try
            {
                if (SettingsSaved != null)
                {
                    SettingsSaved(this, _storage.Settings);
                    return;
                }

                _storage.Save();
            }
            catch
            {
                _storage.Apps.Clear();
                foreach (var app in previousApps)
                    _storage.Apps.Add(app);
                throw;
            }
        }

        private static List<AppHotkeys> CloneApps(IEnumerable<AppHotkeys> source)
        {
            var result = new List<AppHotkeys>();
            if (source == null)
                return result;

            foreach (var app in source)
            {
                var clone = new AppHotkeys
                {
                    ProcessNames = new List<string>(app?.ProcessNames ?? new List<string>()),
                    Title = app?.Title,
                    Hotkeys = null
                };

                if (app?.Groups != null)
                {
                    clone.Groups = new ObservableCollection<HotkeyGroup>();
                    foreach (var group in app.Groups)
                    {
                        var groupClone = new HotkeyGroup { Name = group.Name };
                        if (group.Hotkeys != null)
                        {
                            foreach (var hotkey in group.Hotkeys)
                            {
                                groupClone.Hotkeys.Add(new HotkeyEntry
                                {
                                    Keys = hotkey?.Keys,
                                    Description = hotkey?.Description
                                });
                            }
                        }
                        clone.Groups.Add(groupClone);
                    }
                }

                result.Add(clone);
            }

            return result;
        }
    }
}
