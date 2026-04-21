using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using WhatKey.Models;

namespace WhatKey.Services
{
    public class HotkeyRecoveryException : InvalidOperationException
    {
        public HotkeyRecoveryException(string message)
            : base(message)
        {
        }
    }

    public class KeyboardHookService : IDisposable
    {
        // Hook constants
        private const int WH_KEYBOARD_LL = 13;
        private const int WH_MOUSE_LL = 14;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;
        private const int WM_HOTKEY = 0x0312;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_MBUTTONDOWN = 0x0207;
        private const int WM_XBUTTONDOWN = 0x020B;

        // Hotkey modifier constants
        private const int MOD_ALT = 0x0001;
        private const int MOD_CONTROL = 0x0002;
        private const int MOD_SHIFT = 0x0004;
        private const int MOD_WIN = 0x0008;
        private const int MOD_NOREPEAT = 0x4000;
        private const int ERROR_HOTKEY_NOT_REGISTERED = 1419;

        // VK codes
        private const uint VK_ESCAPE = 0x1B;

        private const int TOGGLE_HOTKEY_ID = 9001;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll", EntryPoint = "RegisterHotKey", SetLastError = true)]
        private static extern bool RegisterHotKeyNative(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll", EntryPoint = "UnregisterHotKey", SetLastError = true)]
        private static extern bool UnregisterHotKeyNative(IntPtr hWnd, int id);

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private IntPtr _hookId = IntPtr.Zero;
        private readonly LowLevelKeyboardProc _hookProc; // keep alive to prevent GC
        private IntPtr _mouseHookId = IntPtr.Zero;
        private readonly LowLevelMouseProc _mouseHookProc; // keep alive to prevent GC
        private Func<IntPtr> _installMouseHook;
        private DispatcherTimer _holdTimer;
        private HwndSource _hwndSource;
        private IntPtr _hotkeyWindowHandle = IntPtr.Zero;
        private readonly AppSettings _settings;
        private uint _holdVkCode;
        private int _appliedHoldDelayMs;
        private string _appliedHoldKey;
        private string _appliedToggleHotkey;
        private Func<IntPtr, int, int, int, bool> _registerHotKey;
        private Func<IntPtr, int, bool> _unregisterHotKey;
        private Func<int> _getLastWin32Error;
        private Func<IntPtr> _installKeyboardHook;

        private bool _isHoldKeyDown;
        private bool _isOverlayVisible;

        public event EventHandler TriggerShow;
        public event EventHandler TriggerHide;

        public KeyboardHookService(AppSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            _settings = settings;
            _hookProc = HookCallback;
            _appliedHoldDelayMs = NormalizeHoldDelayMs(settings.HoldDelayMs);
            _appliedHoldKey = NormalizeHoldKey(settings.HoldKey);
            _appliedToggleHotkey = NormalizeStartupToggleHotkey(settings.ToggleHotkey);

            _settings.HoldDelayMs = _appliedHoldDelayMs;
            _settings.HoldKey = _appliedHoldKey;
            _settings.ToggleHotkey = _appliedToggleHotkey;

            _holdVkCode = GetHoldKeyVkCode(_appliedHoldKey);
            _registerHotKey = RegisterHotKeyNative;
            _unregisterHotKey = UnregisterHotKeyNative;
            _getLastWin32Error = Marshal.GetLastWin32Error;
            _installKeyboardHook = InstallKeyboardHookNative;
            _mouseHookProc = MouseHookCallback;
            _installMouseHook = InstallMouseHookNative;

            _holdTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(_appliedHoldDelayMs)
            };
            _holdTimer.Tick += OnHoldTimerTick;
        }

        public void Install()
        {
            // Message-only window to receive WM_HOTKEY
            var parameters = new HwndSourceParameters("WhatKeyHotkeyWindow")
            {
                WindowStyle = 0x00800000, // WS_BORDER (minimal, needed for message-only)
                ExtendedWindowStyle = 0,
                PositionX = -100,
                PositionY = -100,
                Width = 1,
                Height = 1,
                ParentWindow = new IntPtr(-3) // HWND_MESSAGE
            };

            _hwndSource = new HwndSource(parameters);
            _hotkeyWindowHandle = _hwndSource.Handle;
            _hwndSource.AddHook(WndProc);

            _hookId = _installKeyboardHook();
            if (_hookId == IntPtr.Zero)
            {
                var error = _getLastWin32Error();
                throw new InvalidOperationException(
                    $"Failed to install keyboard hook. Win32 error: {error}.");
            }

            _mouseHookId = _installMouseHook();
            if (_mouseHookId == IntPtr.Zero)
                Trace.TraceWarning("Failed to install mouse hook. Win32 error: {0}.", _getLastWin32Error());

            if (!RegisterToggleHotkey(_appliedToggleHotkey))
                throw new InvalidOperationException("Failed to register toggle hotkey.");
        }

        public AppSettings GetAppliedSettingsSnapshot()
        {
            return new AppSettings
            {
                HoldDelayMs = _appliedHoldDelayMs,
                HoldKey = _appliedHoldKey,
                ToggleHotkey = _appliedToggleHotkey
            };
        }

        public void UpdateSettings(AppSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            if (_holdTimer != null && !_holdTimer.Dispatcher.CheckAccess())
            {
                _holdTimer.Dispatcher.Invoke(() => UpdateSettingsCore(settings));
                return;
            }

            UpdateSettingsCore(settings);
        }

        private void UpdateSettingsCore(AppSettings settings)
        {
            int normalizedDelayMs = NormalizeHoldDelayMs(settings.HoldDelayMs);
            var normalizedHoldKey = NormalizeHoldKey(settings.HoldKey);
            var normalizedToggleHotkey = NormalizeToggleHotkey(settings.ToggleHotkey);
            var oldDelayMs = _appliedHoldDelayMs;
            var oldHoldKey = _appliedHoldKey;
            var oldToggleHotkey = _appliedToggleHotkey;
            var oldHoldVkCode = _holdVkCode;

            if (_hotkeyWindowHandle != IntPtr.Zero && !TryUnregisterToggleHotkey())
                throw new InvalidOperationException("Failed to unregister current toggle hotkey.");

            _settings.HoldDelayMs = normalizedDelayMs;
            _settings.HoldKey = normalizedHoldKey;
            _settings.ToggleHotkey = normalizedToggleHotkey;
            _holdVkCode = GetHoldKeyVkCode(_settings.HoldKey);
            _holdTimer.Interval = TimeSpan.FromMilliseconds(_settings.HoldDelayMs);
            ResetHoldState();

            if (!RegisterToggleHotkey(_settings.ToggleHotkey))
            {
                Trace.TraceWarning("Failed to register updated toggle hotkey, rolling back to previous value.");
                _settings.HoldDelayMs = oldDelayMs;
                _settings.HoldKey = oldHoldKey;
                _settings.ToggleHotkey = oldToggleHotkey;
                _holdVkCode = oldHoldVkCode;
                _holdTimer.Interval = TimeSpan.FromMilliseconds(_settings.HoldDelayMs);
                ResetHoldState();

                if (_hotkeyWindowHandle != IntPtr.Zero && !RegisterToggleHotkey(_settings.ToggleHotkey))
                    throw new HotkeyRecoveryException("Failed to restore previous toggle hotkey after update rollback.");

                throw new InvalidOperationException("Failed to register updated toggle hotkey.");
            }

            _appliedHoldDelayMs = _settings.HoldDelayMs;
            _appliedHoldKey = _settings.HoldKey;
            _appliedToggleHotkey = _settings.ToggleHotkey;
        }

        public void NotifyOverlayHidden()
        {
            _isOverlayVisible = false;
        }

        private bool RegisterToggleHotkey(string hotkey)
        {
            if (_hotkeyWindowHandle == IntPtr.Zero) return true;

            if (!TryParseToggleHotkey(hotkey, out int mods, out int vk))
            {
                Trace.TraceWarning("RegisterHotKey skipped due to invalid hotkey format. hotkey='{0}'", hotkey);
                return false;
            }

            if (vk == 0) return true;

            var registerResult = _registerHotKey(_hotkeyWindowHandle, TOGGLE_HOTKEY_ID, mods | MOD_NOREPEAT, vk);
            if (!registerResult)
            {
                var error = _getLastWin32Error();
                Trace.TraceWarning("RegisterHotKey failed. hotkey='{0}', error={1}", hotkey, error);
            }

            return registerResult;
        }

        private bool TryUnregisterToggleHotkey()
        {
            var unregisterResult = _unregisterHotKey(_hotkeyWindowHandle, TOGGLE_HOTKEY_ID);
            if (!unregisterResult)
            {
                var error = _getLastWin32Error();
                if (error != 0 && error != ERROR_HOTKEY_NOT_REGISTERED)
                {
                    Trace.TraceWarning("UnregisterHotKey failed. error={0}", error);
                    return false;
                }
            }

            return true;
        }

        private void ResetHoldState()
        {
            _holdTimer.Stop();
            _isHoldKeyDown = false;

            if (_isOverlayVisible)
            {
                _isOverlayVisible = false;
                TriggerHide?.Invoke(this, EventArgs.Empty);
            }
        }

        public void ForceResetHoldState()
        {
            if (_holdTimer != null && !_holdTimer.Dispatcher.CheckAccess())
            {
                _holdTimer.Dispatcher.BeginInvoke(new Action(ResetHoldState));
                return;
            }
            ResetHoldState();
        }

        private IntPtr InstallMouseHookNative()
        {
            using (var process = Process.GetCurrentProcess())
            using (var module = process.MainModule)
            {
                return SetWindowsHookEx(
                    WH_MOUSE_LL,
                    _mouseHookProc,
                    GetModuleHandle(module.ModuleName),
                    0);
            }
        }

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                if (wParam == (IntPtr)WM_LBUTTONDOWN || wParam == (IntPtr)WM_RBUTTONDOWN ||
                    wParam == (IntPtr)WM_MBUTTONDOWN || wParam == (IntPtr)WM_XBUTTONDOWN)
                {
                    if (_isHoldKeyDown || _holdTimer.IsEnabled)
                        ResetHoldState();
                }
            }
            return CallNextHookEx(_mouseHookId, nCode, wParam, lParam);
        }

        private IntPtr InstallKeyboardHookNative()
        {
            using (var process = Process.GetCurrentProcess())
            using (var module = process.MainModule)
            {
                return SetWindowsHookEx(
                    WH_KEYBOARD_LL,
                    _hookProc,
                    GetModuleHandle(module.ModuleName),
                    0);
            }
        }

        private static bool TryParseToggleHotkey(string hotkey, out int mods, out int vk)
        {
            mods = 0;
            vk = 0;

            if (string.IsNullOrWhiteSpace(hotkey)) return true;

            bool hasPrimaryKey = false;
            foreach (var part in hotkey.Split('+'))
            {
                var token = part.Trim();
                if (token.Length == 0)
                    return false;

                switch (token.ToLowerInvariant())
                {
                    case "ctrl": mods |= MOD_CONTROL; break;
                    case "alt": mods |= MOD_ALT; break;
                    case "shift": mods |= MOD_SHIFT; break;
                    case "win": mods |= MOD_WIN; break;
                    default:
                        if (token.Length != 1 || hasPrimaryKey)
                            return false;

                        vk = char.ToUpperInvariant(token[0]);
                        hasPrimaryKey = true;
                        break;
                }
            }

            return hasPrimaryKey;
        }

        private static int NormalizeHoldDelayMs(int holdDelayMs)
        {
            return holdDelayMs < 0 ? 0 : holdDelayMs;
        }

        private static string NormalizeHoldKey(string holdKey)
        {
            return string.IsNullOrWhiteSpace(holdKey) ? "LControlKey" : holdKey;
        }

        private static string NormalizeToggleHotkey(string toggleHotkey)
        {
            return toggleHotkey ?? string.Empty;
        }

        private static string NormalizeStartupToggleHotkey(string toggleHotkey)
        {
            var normalized = NormalizeToggleHotkey(toggleHotkey);
            if (normalized.Length == 0)
                return normalized;

            if (TryParseToggleHotkey(normalized, out _, out _))
                return normalized;

            Trace.TraceWarning("Invalid persisted toggle hotkey '{0}' detected at startup. Falling back to disabled toggle hotkey.", normalized);
            return string.Empty;
        }

        private static uint GetHoldKeyVkCode(string keyName)
        {
            switch (keyName?.ToLower())
            {
                case "lcontrolkey": return 0xA2;
                case "rcontrolkey": return 0xA3;
                case "lshiftkey": return 0xA0;
                case "rshiftkey": return 0xA1;
                case "lmenu": return 0xA4;
                case "rmenu": return 0xA5;
                case "lwin": return 0x5B;
                case "rwin": return 0x5C;
                default: return 0xA2; // LControl
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == TOGGLE_HOTKEY_ID)
            {
                if (_isOverlayVisible)
                {
                    _isOverlayVisible = false;
                    TriggerHide?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    _isOverlayVisible = true;
                    TriggerShow?.Invoke(this, EventArgs.Empty);
                }
                handled = true;
            }
            return IntPtr.Zero;
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var hookStruct = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));

                bool isKeyDown = wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN;
                bool isKeyUp = wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP;

                if (isKeyDown)
                {
                    if (hookStruct.vkCode == _holdVkCode && !_isHoldKeyDown)
                    {
                        _isHoldKeyDown = true;
                        _holdTimer.Stop();
                        _holdTimer.Start();
                    }
                    else if (hookStruct.vkCode == VK_ESCAPE && _isOverlayVisible)
                    {
                        _isOverlayVisible = false;
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                            TriggerHide?.Invoke(this, EventArgs.Empty)));
                    }
                    else if (_isHoldKeyDown && hookStruct.vkCode != _holdVkCode)
                    {
                        _holdTimer.Stop();
                    }
                }
                else if (isKeyUp)
                {
                    if (hookStruct.vkCode == _holdVkCode)
                    {
                        _isHoldKeyDown = false;
                        _holdTimer.Stop();

                        if (_isOverlayVisible)
                        {
                            _isOverlayVisible = false;
                            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                                TriggerHide?.Invoke(this, EventArgs.Empty)));
                        }
                    }
                }
            }

            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        private void OnHoldTimerTick(object sender, EventArgs e)
        {
            _holdTimer.Stop();
            if (_isHoldKeyDown && !_isOverlayVisible)
            {
                _isOverlayVisible = true;
                TriggerShow?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Dispose()
        {
            _holdTimer?.Stop();

            if (_hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
            }

            if (_mouseHookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_mouseHookId);
                _mouseHookId = IntPtr.Zero;
            }

            if (_hotkeyWindowHandle != IntPtr.Zero)
            {
                TryUnregisterToggleHotkey();
                _hotkeyWindowHandle = IntPtr.Zero;
            }

            if (_hwndSource != null)
            {
                _hwndSource.Dispose();
                _hwndSource = null;
            }
        }
    }
}
