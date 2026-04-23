using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
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
        private const uint MONITOR_DEFAULTTOPRIMARY = 1;

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

        private int _fadeOutGeneration;

        public OverlayWindow()
        {
            InitializeComponent();
            _viewModel = new OverlayViewModel();
            DataContext = _viewModel;
        }

        public void ShowWithGroups(List<HotkeyGroup> groups, List<HotkeyGroup> systemGroups, string processName, IntPtr sourceHwnd = default)
        {
            ++_fadeOutGeneration;
            var safeGroups = groups ?? new List<HotkeyGroup>();
            var safeSystem = systemGroups ?? new List<HotkeyGroup>();
            var totalHotkeys = safeGroups.Sum(g => g.Hotkeys != null ? g.Hotkeys.Count : 0)
                             + safeSystem.Sum(g => g.Hotkeys != null ? g.Hotkeys.Count : 0);
            _viewModel.Groups = new ObservableCollection<HotkeyGroup>(safeGroups);
            _viewModel.SystemGroups = new ObservableCollection<HotkeyGroup>(safeSystem);
            _viewModel.UpdateLayoutForHotkeysCount(totalHotkeys);
            _viewModel.AppTitle = string.IsNullOrEmpty(processName) ? "Unknown Application" : processName;

            // Recenter on active monitor each time it's shown
            Opacity = 0;

            if (!IsVisible)
                Show();

            // Position after layout
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var bounds = GetMonitorWorkAreaDips(sourceHwnd);
                MinWidth = Math.Min(OverlayViewModel.DefaultOverlayMinWidth, bounds.Width);
                MaxWidth = Math.Min(OverlayViewModel.DefaultOverlayMaxWidth, bounds.Width);
                _viewModel.UpdateLayoutForHotkeysCount(totalHotkeys, MaxWidth);
                UpdateLayout();
                var listWidth = GetAvailableHotkeysListWidth();
                _viewModel.UpdateLayoutForHotkeysCount(totalHotkeys, listWidth);
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

        private double GetAvailableHotkeysListWidth()
        {
            var width = HotkeysScrollViewer.ActualWidth;
            if (width <= 0d || double.IsNaN(width))
                return MaxWidth;

            if (HotkeysScrollViewer.ComputedVerticalScrollBarVisibility == Visibility.Visible)
                width -= SystemParameters.VerticalScrollBarWidth;

            return width > 0d ? width : OverlayViewModel.DefaultMinColumnWidth;
        }

        private Rect GetMonitorWorkAreaDips(IntPtr hwnd)
        {
            var fallback = new Rect(0, 0,
                SystemParameters.PrimaryScreenWidth,
                SystemParameters.PrimaryScreenHeight);

            IntPtr hMonitor;
            if (hwnd == IntPtr.Zero)
            {
                var ourHwnd = new WindowInteropHelper(this).Handle;
                hMonitor = ourHwnd != IntPtr.Zero
                    ? MonitorFromWindow(ourHwnd, MONITOR_DEFAULTTONEAREST)
                    : MonitorFromWindow(IntPtr.Zero, MONITOR_DEFAULTTOPRIMARY);
            }
            else
            {
                hMonitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            }

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
            var gen = _fadeOutGeneration;
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(150));
            fadeOut.Completed += (s, e) =>
            {
                if (_fadeOutGeneration == gen)
                    Hide();
            };
            BeginAnimation(OpacityProperty, fadeOut);
        }
    }
}
