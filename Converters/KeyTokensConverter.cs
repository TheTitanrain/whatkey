using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Data;
using System.Windows.Media;

namespace WhatKey.Converters
{
    public class KeyTokensConverter : IValueConverter
    {
        private static readonly Brush ModifierBrush;
        private static readonly Brush FunctionBrush;
        private static readonly Brush DefaultBrush;
        private static readonly Brush SeparatorBrush;

        static KeyTokensConverter()
        {
            ModifierBrush = new SolidColorBrush(Color.FromRgb(0xF9, 0xE2, 0xAF));
            ModifierBrush.Freeze();
            FunctionBrush = new SolidColorBrush(Color.FromRgb(0xA6, 0xE3, 0xA1));
            FunctionBrush.Freeze();
            DefaultBrush = new SolidColorBrush(Color.FromRgb(0xCD, 0xD6, 0xF4));
            DefaultBrush.Freeze();
            SeparatorBrush = new SolidColorBrush(Color.FromRgb(0x6C, 0x70, 0x86));
            SeparatorBrush.Freeze();
        }

        private static readonly string[] ModifierKeys = { "ctrl", "alt", "shift", "win", "esc", "tab", "meta" };
        private static readonly Regex FunctionKeyRegex = new Regex(@"^f([1-9]|1[0-2])$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var result = new List<KeyToken>();
            if (value == null) return result;

            var keys = value.ToString();
            if (string.IsNullOrWhiteSpace(keys)) return result;

            // Handle chord sequences (space-separated, e.g. "Ctrl+K Ctrl+W")
            var chords = keys.Split(' ');
            for (int ci = 0; ci < chords.Length; ci++)
            {
                if (ci > 0)
                    result.Add(new KeyToken(" ", SeparatorBrush));

                var parts = chords[ci].Split('+');
                for (int i = 0; i < parts.Length; i++)
                {
                    if (i > 0)
                        result.Add(new KeyToken("+", SeparatorBrush));

                    var token = parts[i].Trim();
                    if (string.IsNullOrEmpty(token)) continue;

                    result.Add(new KeyToken(token, GetBrush(token)));
                }
            }

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();

        private static Brush GetBrush(string token)
        {
            if (FunctionKeyRegex.IsMatch(token)) return FunctionBrush;
            if (IsModifier(token)) return ModifierBrush;
            return DefaultBrush;
        }

        private static bool IsModifier(string token)
        {
            if (token == null) return false;
            var lower = token.ToLowerInvariant();
            foreach (var mod in ModifierKeys)
                if (mod == lower) return true;
            return false;
        }
    }
}
