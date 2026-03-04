using Microsoft.Win32;
using System.Reflection;

namespace WhatKey.Services
{
    internal static class AutostartService
    {
        private const string RegKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "WhatKey";

        public static bool IsEnabled()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(RegKey))
                return key?.GetValue(AppName) != null;
        }

        public static void Enable()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(RegKey, writable: true))
                key?.SetValue(AppName, $"\"{Assembly.GetExecutingAssembly().Location}\"");
        }

        public static void Disable()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(RegKey, writable: true))
                key?.DeleteValue(AppName, throwOnMissingValue: false);
        }
    }
}
