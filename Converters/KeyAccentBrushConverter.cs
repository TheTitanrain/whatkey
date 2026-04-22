using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;
using System.Windows.Media;

namespace WhatKey.Converters
{
    public class KeyAccentBrushConverter : IValueConverter
    {
        private static readonly Brush FunctionKeyBrush = new SolidColorBrush(Color.FromRgb(0xA6, 0xE3, 0xA1));
        private static readonly Brush ModifierKeyBrush = new SolidColorBrush(Color.FromRgb(0xF9, 0xE2, 0xAF));
        private static readonly Brush DefaultKeyBrush = new SolidColorBrush(Color.FromRgb(0x89, 0xDC, 0xEB));

        private static readonly string[] ModifierKeys = { "ctrl", "alt", "shift", "win", "esc", "tab", "meta" };
        private static readonly Regex FunctionKeyRegex = new Regex(@"^f([1-9]|1[0-2])$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value == null)
                    return DefaultKeyBrush;

                var keys = value.ToString();
                if (string.IsNullOrWhiteSpace(keys))
                    return DefaultKeyBrush;

                var tokens = keys.Split('+');
                var hasModifier = false;

                foreach (var token in tokens)
                {
                    var trimmed = token.Trim();
                    if (FunctionKeyRegex.IsMatch(trimmed))
                        return FunctionKeyBrush;
                    if (IsModifierKey(trimmed))
                        hasModifier = true;
                }

                return hasModifier ? ModifierKeyBrush : DefaultKeyBrush;
            }
            catch
            {
                return DefaultKeyBrush;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();

        private static bool IsModifierKey(string token)
        {
            if (token == null) return false;
            var lower = token.ToLowerInvariant();
            foreach (var mod in ModifierKeys)
                if (mod == lower) return true;
            return false;
        }
    }
}
