using System.Collections.ObjectModel;
using System.Windows;
using WhatKey.Models;

namespace WhatKey.ViewModels
{
    public class OverlayViewModel : BaseViewModel
    {
        private string _appTitle;
        private ObservableCollection<HotkeyEntry> _hotkeys = new ObservableCollection<HotkeyEntry>();

        public string AppTitle
        {
            get => _appTitle;
            set => SetField(ref _appTitle, value);
        }

        public ObservableCollection<HotkeyEntry> Hotkeys
        {
            get => _hotkeys;
            set
            {
                if (SetField(ref _hotkeys, value))
                    OnPropertyChanged(nameof(EmptyMessageVisibility));
            }
        }

        public Visibility EmptyMessageVisibility =>
            _hotkeys == null || _hotkeys.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }
}
