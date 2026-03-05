using System.Collections.ObjectModel;
using System.Windows;
using WhatKey.Models;

namespace WhatKey.ViewModels
{
    public class OverlayViewModel : BaseViewModel
    {
        public const double DefaultHotkeysListMaxHeight = 460d;
        public const double DefaultHotkeyRowHeight = 30d;
        public const double DefaultMinColumnWidth = 240d;
        public const double DefaultOverlayMaxWidth = 980d;
        public const int MinOverlayColumns = 1;
        public const int MaxOverlayColumns = 3;

        private string _appTitle;
        private int _overlayColumns = MinOverlayColumns;
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

        public int OverlayColumns
        {
            get => _overlayColumns;
            set => SetField(ref _overlayColumns, value);
        }

        public Visibility EmptyMessageVisibility =>
            _hotkeys == null || _hotkeys.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        public double HotkeysListMaxHeight => DefaultHotkeysListMaxHeight;

        public static int CalculateOverlayColumns(
            int hotkeysCount,
            double maxListHeight = DefaultHotkeysListMaxHeight,
            double estimatedRowHeight = DefaultHotkeyRowHeight,
            int maxColumns = MaxOverlayColumns,
            double availableWidth = double.PositiveInfinity,
            double minColumnWidth = DefaultMinColumnWidth)
        {
            if (hotkeysCount <= 0)
                return MinOverlayColumns;

            if (maxListHeight <= 0 || estimatedRowHeight <= 0)
                return MinOverlayColumns;

            if (maxColumns < MinOverlayColumns)
                return MinOverlayColumns;

            var widthLimitedMaxColumns = maxColumns;
            if (!double.IsInfinity(availableWidth) && !double.IsNaN(availableWidth) && availableWidth > 0d)
            {
                if (minColumnWidth <= 0d)
                    return MinOverlayColumns;

                widthLimitedMaxColumns = (int)(availableWidth / minColumnWidth);
                if (widthLimitedMaxColumns < MinOverlayColumns)
                    widthLimitedMaxColumns = MinOverlayColumns;
                else if (widthLimitedMaxColumns > maxColumns)
                    widthLimitedMaxColumns = maxColumns;
            }

            var rowsPerColumn = (int)(maxListHeight / estimatedRowHeight);
            if (rowsPerColumn < 1)
                rowsPerColumn = 1;

            var requiredColumns = (hotkeysCount + rowsPerColumn - 1) / rowsPerColumn;

            if (requiredColumns < MinOverlayColumns)
                return MinOverlayColumns;

            return requiredColumns > widthLimitedMaxColumns ? widthLimitedMaxColumns : requiredColumns;
        }

        public void UpdateLayoutForHotkeysCount(int hotkeysCount, double availableWidth = double.PositiveInfinity)
        {
            OverlayColumns = CalculateOverlayColumns(hotkeysCount, availableWidth: availableWidth);
        }
    }
}
