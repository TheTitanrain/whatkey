using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Win32;
using WhatKey.Models;
using WhatKey.Services;
using WhatKey.ViewModels;
using WhatKey.Views;

namespace WhatKey
{
    public partial class App : Application
    {
        private TaskbarIcon _trayIcon;
        private KeyboardHookService _hookService;
        private ActiveWindowService _activeWindowService;
        private HotkeysStorageService _storageService;
        private UpdateService _updateService;
        private OverlayWindow _overlayWindow;
        private EditorWindow _editorWindow;
        private AboutWindow _aboutWindow;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Load data
            _storageService = new HotkeysStorageService();
            var loadResult = _storageService.Load();
            if (!HandleLoadResult(loadResult))
            {
                Shutdown();
                return;
            }

            _activeWindowService = new ActiveWindowService();

            // Setup keyboard hook
            _hookService = new KeyboardHookService(_storageService.Settings);
            _hookService.TriggerShow += OnTriggerShow;
            _hookService.TriggerHide += OnTriggerHide;
            try
            {
                _hookService.Install();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Failed to initialize keyboard hotkey listener.\n\n" + ex.Message,
                    "WhatKey startup error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown();
                return;
            }

            SystemEvents.SessionSwitch += OnSessionSwitch;
            SystemEvents.PowerModeChanged += OnPowerModeChanged;

            // Create windows (but don't show them)
            _overlayWindow = new OverlayWindow();
            var editorViewModel = new EditorViewModel(_storageService, _activeWindowService);
            editorViewModel.SettingsSaved += (sender, settings) =>
            {
                var previousApplied = _hookService.GetAppliedSettingsSnapshot();
                try
                {
                    _hookService.UpdateSettings(settings);
                }
                catch (HotkeyRecoveryException ex)
                {
                    RestoreSettingsSnapshot(_storageService.Settings, previousApplied);
                    if (!TryPersistRollbackSettings(previousApplied, ex))
                        return;
                    MessageBox.Show(
                        "Runtime settings rollback failed. Application will exit to avoid inconsistent hotkey state.\n\n" + ex.Message,
                        "WhatKey runtime error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Shutdown();
                    return;
                }
                catch (Exception ex)
                {
                    RestoreSettingsSnapshot(_storageService.Settings, previousApplied);
                    if (!TryPersistRollbackSettings(previousApplied, ex))
                        return;
                    MessageBox.Show(
                        "Runtime settings were not applied and have been rolled back.\n\n" + ex.Message,
                        "WhatKey settings error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    var applied = _hookService.GetAppliedSettingsSnapshot();
                    RestoreSettingsSnapshot(_storageService.Settings, applied);
                    _storageService.Save();
                }
                catch (Exception ex)
                {
                    try
                    {
                        _hookService.UpdateSettings(previousApplied);
                        RestoreSettingsSnapshot(_storageService.Settings, previousApplied);
                        _storageService.Save();
                    }
                    catch (Exception rollbackEx)
                    {
                        MessageBox.Show(
                            "Runtime settings save failed and rollback did not complete. Application will exit to avoid inconsistent hotkey state.\n\n" +
                            "Save error: " + ex.Message + "\n\n" +
                            "Rollback error: " + rollbackEx.Message,
                            "WhatKey runtime error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        Shutdown();
                        return;
                    }

                    MessageBox.Show(
                        "Runtime settings could not be persisted and were rolled back.\n\n" + ex.Message,
                        "WhatKey settings error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            };
            _editorWindow = new EditorWindow(editorViewModel);
            _editorWindow.Icon = LoadPngIcon();

            // Setup tray
            _updateService = new UpdateService();
            InitializeTray();
            _ = CheckForUpdatesInBackgroundAsync();
        }

        private bool HandleLoadResult(HotkeysLoadResult result)
        {
            if (result.Status != HotkeysLoadStatus.InvalidFormat)
                return true;

            var message =
                "The hotkeys configuration file has an invalid format and cannot be read.\n\n" +
                "Yes: Restore defaults\n" +
                "No: Open hotkeys.json\n" +
                "Cancel: Exit application\n\n" +
                "File: " + result.DataFilePath + "\n" +
                "Error: " + result.ErrorMessage;

            var action = MessageBox.Show(
                message,
                "Invalid hotkeys.json",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning,
                MessageBoxResult.Yes);

            if (action == MessageBoxResult.Yes)
            {
                if (!TryBackupBeforeRestore())
                    return false;

                try
                {
                    _storageService.LoadDefaultsAndSave();
                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "Failed to restore defaults.\n\n" + ex.Message,
                        "Restore failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return false;
                }
            }

            if (action == MessageBoxResult.No)
            {
                TryOpenHotkeysFile(result.DataFilePath);
                return false;
            }

            return false;
        }

        private bool TryBackupBeforeRestore()
        {
            try
            {
                _storageService.CreateBackupOfDataFile();
                return true;
            }
            catch (Exception ex)
            {
                var decision = MessageBox.Show(
                    "Failed to create backup of hotkeys.json.\n\n" +
                    ex.Message + "\n\n" +
                    "Continue restoring defaults without backup?",
                    "Backup failed",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning,
                    MessageBoxResult.No);

                return decision == MessageBoxResult.Yes;
            }
        }

        private void TryOpenHotkeysFile(string path)
        {
            try
            {
                Process.Start(new ProcessStartInfo(path)
                {
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Could not open hotkeys.json.\n\n" + ex.Message,
                    "Open file failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            if (e.Reason == SessionSwitchReason.SessionLock ||
                e.Reason == SessionSwitchReason.RemoteDisconnect ||
                e.Reason == SessionSwitchReason.ConsoleDisconnect)
            {
                _hookService?.ForceResetHoldState();
            }
        }

        private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Suspend)
                _hookService?.ForceResetHoldState();
        }

        private void OnTriggerShow(object sender, EventArgs e)
        {
            var (processName, hwnd) = _activeWindowService.GetActiveWindowInfo();
            var groups = _storageService.GetGroupsForProcess(processName);
            var systemGroups = _storageService.GetSystemGroups();
            _overlayWindow.ShowWithGroups(groups, systemGroups, processName, hwnd);
        }

        private void OnTriggerHide(object sender, EventArgs e)
        {
            _overlayWindow.HideOverlay();
        }

        private void InitializeTray()
        {
            _trayIcon = new TaskbarIcon
            {
                Icon = CreateTrayIcon(),
                ToolTipText = "WhatKey — Hold Ctrl to see hotkeys"
            };

            var menu = new ContextMenu();

            var editItem = new MenuItem { Header = "Edit Hotkeys" };
            editItem.Click += (s, e) => OpenEditor();
            menu.Items.Add(editItem);

            var autostartItem = new MenuItem { Header = "Run at startup", IsCheckable = true };
            menu.Opened += (s, e) => autostartItem.IsChecked = AutostartService.IsEnabled();
            autostartItem.Click += (s, e) =>
            {
                if (AutostartService.IsEnabled())
                    AutostartService.Disable();
                else
                    AutostartService.Enable();
            };
            menu.Items.Add(autostartItem);

            var checkUpdateItem = new MenuItem { Header = "Check for updates" };
            checkUpdateItem.Click += async (s, e) =>
            {
                try
                {
                    UpdateCheckResult result = await _updateService.CheckForUpdateAsync(CurrentVersion);
                    ShowUpdateResult(result);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not check for updates: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            };
            menu.Items.Add(checkUpdateItem);

            menu.Items.Add(new Separator());

            var aboutItem = new MenuItem { Header = "About" };
            aboutItem.Click += (s, e) => OpenAbout();
            menu.Items.Add(aboutItem);

            menu.Items.Add(new Separator());

            var exitItem = new MenuItem { Header = "Exit" };
            exitItem.Click += (s, e) => Shutdown();
            menu.Items.Add(exitItem);

            _trayIcon.ContextMenu = menu;
            _trayIcon.TrayMouseDoubleClick += (s, e) => OpenEditor();
        }

        private void OpenEditor()
        {
            _editorWindow.Show();
            _editorWindow.Activate();
        }

        private void OpenAbout()
        {
            if (_aboutWindow == null)
            {
                _aboutWindow = new AboutWindow();
                _aboutWindow.Icon = LoadPngIcon();
                _aboutWindow.Closed += (s, e) => _aboutWindow = null;
            }
            _aboutWindow.Show();
            _aboutWindow.Activate();
        }

        internal static readonly Version CurrentVersion = GetCurrentVersion();

        private static Version GetCurrentVersion()
        {
            string infoVer = Assembly.GetExecutingAssembly()
                .GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false) is System.Reflection.AssemblyInformationalVersionAttribute[] attrs && attrs.Length > 0
                ? attrs[0].InformationalVersion : "0.0.0";
            int dashIdx = infoVer.IndexOf('-');
            string versionStr = dashIdx >= 0 ? infoVer.Substring(0, dashIdx) : infoVer;
            return Version.TryParse(versionStr, out Version v) ? v : new Version(0, 0, 0);
        }

        internal static void ShowUpdateResult(UpdateCheckResult result)
        {
            if (result.UpdateAvailable)
            {
                var answer = MessageBox.Show(
                    $"Version {result.LatestVersion} is available. Open release page?",
                    "Update available",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (answer == MessageBoxResult.Yes && !string.IsNullOrEmpty(result.ReleaseUrl))
                    Process.Start(new ProcessStartInfo(result.ReleaseUrl) { UseShellExecute = true });
            }
            else
            {
                MessageBox.Show("You're up to date.", "No updates", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async Task CheckForUpdatesInBackgroundAsync()
        {
            try
            {
                UpdateCheckResult result = await _updateService.CheckForUpdateAsync(CurrentVersion);

                if (!result.UpdateAvailable) return;

                string releaseUrl = result.ReleaseUrl;
                RoutedEventHandler handler = null;
                RoutedEventHandler closeHandler = null;
                handler = (s, e) =>
                {
                    _trayIcon.TrayBalloonTipClicked -= handler;
                    _trayIcon.TrayBalloonTipClosed -= closeHandler;
                    if (!string.IsNullOrEmpty(releaseUrl))
                        Process.Start(new ProcessStartInfo(releaseUrl) { UseShellExecute = true });
                };
                closeHandler = (s, e) =>
                {
                    _trayIcon.TrayBalloonTipClicked -= handler;
                    _trayIcon.TrayBalloonTipClosed -= closeHandler;
                };
                _trayIcon.TrayBalloonTipClicked += handler;
                _trayIcon.TrayBalloonTipClosed += closeHandler;
                _trayIcon.ShowBalloonTip("Update available", $"WhatKey {result.LatestVersion} is available. Click to open.", BalloonIcon.Info);
            }
            catch
            {
                // Silent — background update check failure should not disturb the user.
            }
        }

        private static void RestoreSettingsSnapshot(AppSettings target, AppSettings source)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (source == null) throw new ArgumentNullException(nameof(source));

            target.HoldDelayMs = source.HoldDelayMs;
            target.HoldKey = source.HoldKey;
            target.ToggleHotkey = source.ToggleHotkey;
        }

        private bool TryPersistRollbackSettings(AppSettings rollbackSnapshot, Exception applyException)
        {
            try
            {
                _storageService.Save();
                return true;
            }
            catch (Exception saveEx)
            {
                try
                {
                    _hookService.UpdateSettings(rollbackSnapshot);
                }
                catch (Exception runtimeEx)
                {
                    // Best-effort runtime alignment before shutdown.
                    Trace.TraceWarning("Failed to realign runtime state during rollback save failure: {0}", runtimeEx.Message);
                }

                MessageBox.Show(
                    "Runtime settings rollback could not be persisted. Application will exit to avoid inconsistent state.\n\n" +
                    "Apply error: " + applyException.Message + "\n\n" +
                    "Rollback save error: " + saveEx.Message,
                    "WhatKey runtime error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown();
                return false;
            }
        }

        private static System.Drawing.Icon CreateTrayIcon()
        {
            var stream = GetResourceStream(new Uri("pack://application:,,,/Assets/whatkey.png")).Stream;
            using (var original = new System.Drawing.Bitmap(stream))
            using (var bmp = new System.Drawing.Bitmap(original, 16, 16))
            {
                return System.Drawing.Icon.FromHandle(bmp.GetHicon());
            }
        }

        private static System.Windows.Media.ImageSource LoadPngIcon()
        {
            return new BitmapImage(new Uri("pack://application:,,,/Assets/whatkey.png"));
        }

        protected override void OnExit(ExitEventArgs e)
        {
            SystemEvents.SessionSwitch -= OnSessionSwitch;
            SystemEvents.PowerModeChanged -= OnPowerModeChanged;
            _hookService?.Dispose();
            _trayIcon?.Dispose();
            base.OnExit(e);
        }
    }

}
