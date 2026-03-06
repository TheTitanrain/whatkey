using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json.Serialization;

namespace WhatKey.Models
{
    public class AppHotkeys
    {
        [JsonPropertyName("processName")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string ProcessName { get; set; }

        [JsonPropertyName("processNames")]
        public List<string> ProcessNames { get; set; } = new List<string>();

        public string Title { get; set; }
        public ObservableCollection<HotkeyEntry> Hotkeys { get; set; } = new ObservableCollection<HotkeyEntry>();

        [JsonIgnore]
        public string ProcessNamesDisplay => string.Join(", ", ProcessNames ?? Enumerable.Empty<string>());
    }
}
