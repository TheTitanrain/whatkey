using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media.Animation;
using WhatKey.Models;
using WhatKey.ViewModels;

namespace WhatKey.Views
{
    public partial class OverlayWindow : Window
    {
        private readonly OverlayViewModel _viewModel;

        public OverlayWindow()
        {
            InitializeComponent();
            _viewModel = new OverlayViewModel();
            DataContext = _viewModel;
        }

        public void ShowWithHotkeys(List<HotkeyEntry> hotkeys, string processName)
        {
            _viewModel.Hotkeys = new ObservableCollection<HotkeyEntry>(hotkeys);
            _viewModel.AppTitle = string.IsNullOrEmpty(processName) ? "Unknown Application" : processName;

            // Recenter on primary screen each time it's shown
            Opacity = 0;

            if (!IsVisible)
                Show();

            // Position after layout
            Dispatcher.BeginInvoke(new Action(() =>
            {
                Left = (SystemParameters.PrimaryScreenWidth - ActualWidth) / 2;
                Top = (SystemParameters.PrimaryScreenHeight - ActualHeight) / 2;
            }));

            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150));
            BeginAnimation(OpacityProperty, fadeIn);
        }

        public void HideOverlay()
        {
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(150));
            fadeOut.Completed += (s, e) =>
            {
                if (Opacity == 0)
                    Hide();
            };
            BeginAnimation(OpacityProperty, fadeOut);
        }
    }
}
