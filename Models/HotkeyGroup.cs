using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace WhatKey.Models
{
    public class HotkeyGroup
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "General";

        [JsonPropertyName("hotkeys")]
        public ObservableCollection<HotkeyEntry> Hotkeys { get; set; } = new ObservableCollection<HotkeyEntry>();
    }
}
