using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Threading;
using WhatKey.Models;
using WhatKey.Services;

namespace WhatKey.ViewModels
{
    public class EditorViewModel : BaseViewModel
    {
        private readonly HotkeysStorageService _storage;
        private readonly ActiveWindowService _activeWindow;

        private ObservableCollection<AppHotkeys> _apps;
        private AppHotkeys _selectedApp;
        private HotkeyEntry _selectedHotkey;
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
            set => SetField(ref _selectedApp, value);
        }

        public HotkeyEntry SelectedHotkey
        {
            get => _selectedHotkey;
            set => SetField(ref _selectedHotkey, value);
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
        public ICommand AddHotkeyCommand { get; }
        public ICommand RemoveHotkeyCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand OpenFolderCommand { get; }

        public event EventHandler<AppSettings> SettingsSaved;

        public EditorViewModel(HotkeysStorageService storage, ActiveWindowService activeWindow)
        {
            _storage = storage;
            _activeWindow = activeWindow;

            Apps = new ObservableCollection<AppHotkeys>(_storage.Apps);
            Settings = _storage.Settings;

            AddAppCommand = new RelayCommand(AddApp);
            RemoveAppCommand = new RelayCommand(RemoveApp, () => SelectedApp != null);
            DetectAppCommand = new RelayCommand(StartDetect, () => !IsDetecting);
            AddHotkeyCommand = new RelayCommand(AddHotkey, () => SelectedApp != null);
            RemoveHotkeyCommand = new RelayCommand(RemoveHotkey, () => SelectedApp != null && SelectedHotkey != null);
            SaveCommand = new RelayCommand(Save);
            OpenFolderCommand = new RelayCommand(OpenFolder);
        }

        private void AddApp()
        {
            var app = new AppHotkeys
            {
                ProcessName = "newapp",
                Title = "New Application"
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
                if (string.Equals(app.ProcessName, processName, StringComparison.OrdinalIgnoreCase))
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
                ProcessName = processName,
                Title = processName
            };
            Apps.Add(newApp);
            _storage.Apps.Add(newApp);
            SelectedApp = newApp;

            DetectButtonText = "Detect";
            IsDetecting = false;
        }

        private void AddHotkey()
        {
            if (SelectedApp == null) return;

            var entry = new HotkeyEntry { Keys = "Ctrl+?", Description = "New hotkey" };
            SelectedApp.Hotkeys.Add(entry);
            SelectedHotkey = entry;
        }

        private void RemoveHotkey()
        {
            if (SelectedApp == null || SelectedHotkey == null) return;
            SelectedApp.Hotkeys.Remove(SelectedHotkey);
            SelectedHotkey = null;
        }

        private void OpenFolder()
        {
            Process.Start("explorer.exe", HotkeysStorageService.DataDir);
        }

        private void Save()
        {
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

        private static System.Collections.Generic.List<AppHotkeys> CloneApps(System.Collections.Generic.IEnumerable<AppHotkeys> source)
        {
            var result = new System.Collections.Generic.List<AppHotkeys>();
            if (source == null)
                return result;

            foreach (var app in source)
            {
                var clone = new AppHotkeys
                {
                    ProcessName = app?.ProcessName,
                    Title = app?.Title
                };

                if (app?.Hotkeys != null)
                {
                    foreach (var hotkey in app.Hotkeys)
                    {
                        clone.Hotkeys.Add(new HotkeyEntry
                        {
                            Keys = hotkey?.Keys,
                            Description = hotkey?.Description
                        });
                    }
                }

                result.Add(clone);
            }

            return result;
        }
    }
}
