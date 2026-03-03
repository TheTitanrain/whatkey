using System.Collections.Generic;

namespace WhatKey.Models
{
    public class HotkeysData
    {
        public AppSettings Settings { get; set; } = new AppSettings();
        public List<AppHotkeys> Apps { get; set; } = new List<AppHotkeys>();
    }
}
