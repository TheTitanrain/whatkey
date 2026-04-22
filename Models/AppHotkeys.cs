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

        // Legacy field: read from old JSON, set to null after migration in NormalizeData, never written when null
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ObservableCollection<HotkeyEntry> Hotkeys { get; set; } = new ObservableCollection<HotkeyEntry>();

        [JsonPropertyName("groups")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ObservableCollection<HotkeyGroup> Groups { get; set; }

        [JsonIgnore]
        public string ProcessNamesDisplay => string.Join(", ", ProcessNames ?? Enumerable.Empty<string>());
    }
}
