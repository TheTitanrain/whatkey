using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;
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
            _hookService.Install();

            // Create windows (but don't show them)
            _overlayWindow = new OverlayWindow();
            _editorWindow = new EditorWindow(
                new EditorViewModel(_storageService, _activeWindowService));

            // Setup tray
            InitializeTray();
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

        private void OnTriggerShow(object sender, EventArgs e)
        {
            var processName = _activeWindowService.GetActiveProcessName();
            var hotkeys = _storageService.GetHotkeysForProcess(processName);
            _overlayWindow.ShowWithHotkeys(hotkeys, processName);
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

            menu.Items.Add(new Separator());

            var aboutItem = new MenuItem { Header = "About" };
            aboutItem.Click += (s, e) => OpenAbout();
            menu.Items.Add(aboutItem);

            menu.Items.Add(new Separator());

            var exitItem = new MenuItem { Header = "Exit" };
            exitItem.Click += (s, e) => ExitApp();
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
                _aboutWindow.Closed += (s, e) => _aboutWindow = null;
            }
            _aboutWindow.Show();
            _aboutWindow.Activate();
        }

        private void ExitApp()
        {
            _hookService?.Dispose();
            _trayIcon?.Dispose();
            Shutdown();
        }

        private static System.Drawing.Icon CreateTrayIcon()
        {
            var bmp = new System.Drawing.Bitmap(16, 16);
            using (var g = System.Drawing.Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(System.Drawing.Color.Transparent);

                // Draw keyboard body
                var bodyColor = System.Drawing.Color.FromArgb(137, 180, 250); // blue
                g.FillRoundedRectangle(
                    new System.Drawing.SolidBrush(bodyColor),
                    1, 3, 14, 10, 2);

                // Draw key squares (white)
                var keyBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(30, 30, 46));
                g.FillRectangle(keyBrush, 2, 5, 3, 2);
                g.FillRectangle(keyBrush, 6, 5, 3, 2);
                g.FillRectangle(keyBrush, 10, 5, 3, 2);
                g.FillRectangle(keyBrush, 2, 8, 3, 2);
                g.FillRectangle(keyBrush, 6, 8, 5, 2);
                g.FillRectangle(keyBrush, 10, 8, 3, 2);
            }

            var hIcon = bmp.GetHicon();
            return System.Drawing.Icon.FromHandle(hIcon);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _hookService?.Dispose();
            _trayIcon?.Dispose();
            base.OnExit(e);
        }
    }

    // Extension method for rounded rectangle
    internal static class GraphicsExtensions
    {
        public static void FillRoundedRectangle(
            this System.Drawing.Graphics g,
            System.Drawing.Brush brush,
            float x, float y, float width, float height, float radius)
        {
            float d = radius * 2;
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(x, y, d, d, 180, 90);
            path.AddArc(x + width - d, y, d, d, 270, 90);
            path.AddArc(x + width - d, y + height - d, d, d, 0, 90);
            path.AddArc(x, y + height - d, d, d, 90, 90);
            path.CloseFigure();
            g.FillPath(brush, path);
        }
    }
}
