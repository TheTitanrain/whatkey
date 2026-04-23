using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;
using System.Windows.Media;

namespace WhatKey.Converters
{
    public class KeyAccentBrushConverter : IValueConverter
    {
        private static readonly Brush FunctionBrush = new SolidColorBrush(Color.FromRgb(0xA6, 0xE3, 0xA1));
        private static readonly Brush ModifierBrush = new SolidColorBrush(Color.FromRgb(0xF9, 0xE2, 0xAF));
        private static readonly Brush DefaultBrush  = new SolidColorBrush(Color.FromRgb(0x89, 0xDC, 0xEB));

        private static readonly Regex FunctionKeyRegex =
            new Regex(@"^f([1-9]|1[0-2])$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly string[] ModifierKeys =
            { "ctrl", "alt", "shift", "win", "esc", "tab", "meta" };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value == null) return DefaultBrush;
                var keys = value.ToString();
                if (string.IsNullOrWhiteSpace(keys)) return DefaultBrush;

                bool hasModifier = false;
                foreach (var part in keys.Split('+'))
                {
                    var token = part.Trim();
                    if (FunctionKeyRegex.IsMatch(token)) return FunctionBrush;
                    if (!hasModifier && IsModifier(token)) hasModifier = true;
                }
                return hasModifier ? ModifierBrush : DefaultBrush;
            }
            catch
            {
                return DefaultBrush;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();

        private static bool IsModifier(string token)
        {
            if (string.IsNullOrEmpty(token)) return false;
            var lower = token.ToLowerInvariant();
            foreach (var mod in ModifierKeys)
                if (mod == lower) return true;
            return false;
        }
    }
}
