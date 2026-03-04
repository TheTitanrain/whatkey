using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WhatKey.Models
{
    public class AppSettings : INotifyPropertyChanged
    {
        private string _holdKey = "LControlKey";
        private int _holdDelayMs = 500;
        private string _toggleHotkey = "Ctrl+Alt+H";

        public event PropertyChangedEventHandler PropertyChanged;

        public string HoldKey
        {
            get => _holdKey;
            set
            {
                if (_holdKey == value) return;
                _holdKey = value;
                OnPropertyChanged();
            }
        }

        public int HoldDelayMs
        {
            get => _holdDelayMs;
            set
            {
                if (_holdDelayMs == value) return;
                _holdDelayMs = value;
                OnPropertyChanged();
            }
        }

        public string ToggleHotkey
        {
            get => _toggleHotkey;
            set
            {
                if (_toggleHotkey == value) return;
                _toggleHotkey = value;
                OnPropertyChanged();
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
