using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using WhatKey.Models;

namespace WhatKey.ViewModels
{
    public class OverlayViewModel : BaseViewModel
    {
        public const double DefaultHotkeysListMaxHeight = 460d;
        public const double DefaultHotkeyRowHeight = 30d;
        public const double DefaultMinColumnWidth = 240d;
        public const double DefaultOverlayMinWidth = 420d;
        public const double DefaultOverlayMaxWidth = 980d;
        public const double OverlayColumnTargetRatio = 0.65;
        public const double OverlayScrollCapRatio    = 0.90;
        public const double OverlayMaxWidthRatio     = 0.80;
        public const int MinOverlayColumns = 1;
        public const int MaxOverlayColumns = 3;

        private string _appTitle;
        private int _overlayColumns = MinOverlayColumns;
        private double _hotkeysListMaxHeight = DefaultHotkeysListMaxHeight;
        private double _columnTargetHeight = DefaultHotkeysListMaxHeight;
        private ObservableCollection<HotkeyGroup> _groups = new ObservableCollection<HotkeyGroup>();
        private ObservableCollection<HotkeyGroup> _systemGroups = new ObservableCollection<HotkeyGroup>();

        public string AppTitle
        {
            get => _appTitle;
            set => SetField(ref _appTitle, value);
        }

        public ObservableCollection<HotkeyGroup> Groups
        {
            get => _groups;
            set
            {
                if (SetField(ref _groups, value))
                    OnPropertyChanged(nameof(EmptyMessageVisibility));
            }
        }

        public ObservableCollection<HotkeyGroup> SystemGroups
        {
            get => _systemGroups;
            set
            {
                if (SetField(ref _systemGroups, value))
                    OnPropertyChanged(nameof(SystemSectionVisible));
            }
        }

        public Visibility SystemSectionVisible =>
            _systemGroups != null && _systemGroups.Any(g => g.Hotkeys != null && g.Hotkeys.Count > 0)
                ? Visibility.Visible : Visibility.Collapsed;

        public int OverlayColumns
        {
            get => _overlayColumns;
            set => SetField(ref _overlayColumns, value);
        }

        public Visibility EmptyMessageVisibility =>
            _groups == null || !_groups.Any(g => g.Hotkeys != null && g.Hotkeys.Count > 0) ? Visibility.Visible : Visibility.Collapsed;

        public double HotkeysListMaxHeight
        {
            get => _hotkeysListMaxHeight;
            set => SetField(ref _hotkeysListMaxHeight, value);
        }

        public double ColumnTargetHeight
        {
            get => _columnTargetHeight;
            set => SetField(ref _columnTargetHeight, value);
        }

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
            OverlayColumns = CalculateOverlayColumns(hotkeysCount, ColumnTargetHeight, availableWidth: availableWidth);
        }
    }
}
