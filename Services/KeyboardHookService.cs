using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using WhatKey.Models;

namespace WhatKey.Services
{
    public class KeyboardHookService : IDisposable
    {
        // Hook constants
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;
        private const int WM_HOTKEY = 0x0312;

        // Hotkey modifier constants
        private const int MOD_ALT = 0x0001;
        private const int MOD_CONTROL = 0x0002;
        private const int MOD_SHIFT = 0x0004;
        private const int MOD_WIN = 0x0008;
        private const int MOD_NOREPEAT = 0x4000;

        // VK codes
        private const uint VK_ESCAPE = 0x1B;

        private const int TOGGLE_HOTKEY_ID = 9001;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

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
        private DispatcherTimer _holdTimer;
        private HwndSource _hwndSource;
        private readonly AppSettings _settings;
        private readonly uint _holdVkCode;

        private bool _isHoldKeyDown;
        private bool _isOverlayVisible;

        public event EventHandler TriggerShow;
        public event EventHandler TriggerHide;

        public KeyboardHookService(AppSettings settings)
        {
            _settings = settings;
            _hookProc = HookCallback;
            _holdVkCode = GetHoldKeyVkCode(settings.HoldKey);

            _holdTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(settings.HoldDelayMs)
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
            _hwndSource.AddHook(WndProc);

            // Install low-level keyboard hook
            using (var process = Process.GetCurrentProcess())
            using (var module = process.MainModule)
            {
                _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _hookProc,
                    GetModuleHandle(module.ModuleName), 0);
            }

            RegisterToggleHotkey();
        }

        public void UpdateSettings(AppSettings settings)
        {
            // Unregister old hotkey and register new one
            if (_hwndSource != null)
                UnregisterHotKey(_hwndSource.Handle, TOGGLE_HOTKEY_ID);

            _holdTimer.Interval = TimeSpan.FromMilliseconds(settings.HoldDelayMs);

            RegisterToggleHotkey();
        }

        public void NotifyOverlayHidden()
        {
            _isOverlayVisible = false;
        }

        private void RegisterToggleHotkey()
        {
            if (_hwndSource == null) return;

            ParseToggleHotkey(_settings.ToggleHotkey, out int mods, out int vk);
            if (vk != 0)
                RegisterHotKey(_hwndSource.Handle, TOGGLE_HOTKEY_ID, mods | MOD_NOREPEAT, vk);
        }

        private static void ParseToggleHotkey(string hotkey, out int mods, out int vk)
        {
            mods = 0;
            vk = 0;

            if (string.IsNullOrEmpty(hotkey)) return;

            foreach (var part in hotkey.Split('+'))
            {
                switch (part.Trim().ToLower())
                {
                    case "ctrl": mods |= MOD_CONTROL; break;
                    case "alt": mods |= MOD_ALT; break;
                    case "shift": mods |= MOD_SHIFT; break;
                    case "win": mods |= MOD_WIN; break;
                    default:
                        var key = part.Trim();
                        if (key.Length == 1)
                            vk = char.ToUpper(key[0]);
                        break;
                }
            }
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

            if (_hwndSource != null)
            {
                UnregisterHotKey(_hwndSource.Handle, TOGGLE_HOTKEY_ID);
                _hwndSource.Dispose();
                _hwndSource = null;
            }
        }
    }
}
