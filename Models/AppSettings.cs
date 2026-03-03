namespace WhatKey.Models
{
    public class AppSettings
    {
        public string HoldKey { get; set; } = "LControlKey";
        public int HoldDelayMs { get; set; } = 500;
        public string ToggleHotkey { get; set; } = "Ctrl+Alt+H";
    }
}
