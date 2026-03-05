using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json;

namespace WhatKey.Models
{
    public class AppHotkeys
    {
        [JsonProperty("processName", NullValueHandling = NullValueHandling.Ignore)]
        public string ProcessName { get; set; }

        [JsonProperty("processNames")]
        public List<string> ProcessNames { get; set; } = new List<string>();

        public string Title { get; set; }
        public ObservableCollection<HotkeyEntry> Hotkeys { get; set; } = new ObservableCollection<HotkeyEntry>();

        [JsonIgnore]
        public string ProcessNamesDisplay => string.Join(", ", ProcessNames ?? Enumerable.Empty<string>());
    }
}
