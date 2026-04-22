using System.Windows.Media;

namespace WhatKey.Converters
{
    public class KeyToken
    {
        public string Text { get; }
        public Brush Brush { get; }

        public KeyToken(string text, Brush brush)
        {
            Text = text;
            Brush = brush;
        }
    }
}
