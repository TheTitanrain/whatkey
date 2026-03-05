using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using WhatKey.Models;
using WhatKey.ViewModels;

namespace WhatKey.Views
{
    public partial class OverlayWindow : Window
    {
        private readonly OverlayViewModel _viewModel;

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        private const uint MONITOR_DEFAULTTONEAREST = 2;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left, Top, Right, Bottom; }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MONITORINFO
        {
            public uint cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        public OverlayWindow()
        {
            InitializeComponent();
            _viewModel = new OverlayViewModel();
            DataContext = _viewModel;
        }

        public void ShowWithHotkeys(List<HotkeyEntry> hotkeys, string processName, IntPtr sourceHwnd = default)
        {
            var safeHotkeys = hotkeys ?? new List<HotkeyEntry>();
            _viewModel.Hotkeys = new ObservableCollection<HotkeyEntry>(safeHotkeys);
            _viewModel.UpdateLayoutForHotkeysCount(safeHotkeys.Count);
            _viewModel.AppTitle = string.IsNullOrEmpty(processName) ? "Unknown Application" : processName;

            // Recenter on active monitor each time it's shown
            Opacity = 0;

            if (!IsVisible)
                Show();

            // Position after layout
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var bounds = GetMonitorWorkAreaDips(sourceHwnd);
                MaxWidth = Math.Min(OverlayViewModel.DefaultOverlayMaxWidth, bounds.Width);
                _viewModel.UpdateLayoutForHotkeysCount(safeHotkeys.Count, MaxWidth);
                UpdateLayout();

                var centeredLeft = bounds.Left + (bounds.Width - ActualWidth) / 2;
                var centeredTop = bounds.Top + (bounds.Height - ActualHeight) / 2;

                var maxLeft = bounds.Right - ActualWidth;
                var maxTop = bounds.Bottom - ActualHeight;

                Left = Math.Max(bounds.Left, Math.Min(centeredLeft, maxLeft));
                Top = Math.Max(bounds.Top, Math.Min(centeredTop, maxTop));
            }));

            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150));
            BeginAnimation(OpacityProperty, fadeIn);
        }

        private Rect GetMonitorWorkAreaDips(IntPtr hwnd)
        {
            var fallback = new Rect(0, 0,
                SystemParameters.PrimaryScreenWidth,
                SystemParameters.PrimaryScreenHeight);

            if (hwnd == IntPtr.Zero) return fallback;

            var hMonitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            var info = new MONITORINFO { cbSize = (uint)Marshal.SizeOf<MONITORINFO>() };
            if (!GetMonitorInfo(hMonitor, ref info)) return fallback;

            var source = PresentationSource.FromVisual(this);
            if (source?.CompositionTarget == null) return fallback;

            var m = source.CompositionTarget.TransformFromDevice;
            var topLeft = m.Transform(new Point(info.rcWork.Left, info.rcWork.Top));
            var bottomRight = m.Transform(new Point(info.rcWork.Right, info.rcWork.Bottom));
            return new Rect(topLeft, bottomRight);
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
