using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WhatKey.Services
{
    public class ActiveWindowService
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        public string GetActiveProcessName()
        {
            try
            {
                var hwnd = GetForegroundWindow();
                if (hwnd == IntPtr.Zero) return string.Empty;

                GetWindowThreadProcessId(hwnd, out uint pid);
                if (pid == 0) return string.Empty;

                var process = Process.GetProcessById((int)pid);
                return process.ProcessName.ToLower();
            }
            catch
            {
                return string.Empty;
            }
        }

        public (string ProcessName, IntPtr Hwnd) GetActiveWindowInfo()
        {
            try
            {
                var hwnd = GetForegroundWindow();
                if (hwnd == IntPtr.Zero) return (string.Empty, IntPtr.Zero);

                GetWindowThreadProcessId(hwnd, out uint pid);
                if (pid == 0) return (string.Empty, IntPtr.Zero);

                var process = Process.GetProcessById((int)pid);
                return (process.ProcessName.ToLower(), hwnd);
            }
            catch
            {
                return (string.Empty, IntPtr.Zero);
            }
        }
    }
}
