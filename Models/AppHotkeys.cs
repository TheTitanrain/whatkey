using System.Collections.ObjectModel;

namespace WhatKey.Models
{
    public class AppHotkeys
    {
        public string ProcessName { get; set; }
        public string Title { get; set; }
        public ObservableCollection<HotkeyEntry> Hotkeys { get; set; } = new ObservableCollection<HotkeyEntry>();
    }
}
