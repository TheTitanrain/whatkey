using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Globalization;
using System.Windows.Media;
using WhatKey.Converters;

namespace WhatKey.Tests
{
    [TestClass]
    public class KeyAccentBrushConverterTests
    {
        private static readonly Color GreenColor = Color.FromRgb(0xA6, 0xE3, 0xA1);
        private static readonly Color YellowColor = Color.FromRgb(0xF9, 0xE2, 0xAF);
        private static readonly Color DefaultColor = Color.FromRgb(0x89, 0xDC, 0xEB);

        private readonly KeyAccentBrushConverter _converter = new KeyAccentBrushConverter();

        private Color ConvertToColor(object value)
        {
            var result = _converter.Convert(value, typeof(Brush), null, CultureInfo.InvariantCulture);
            return ((SolidColorBrush)result).Color;
        }

        [TestMethod]
        public void F1_ReturnsGreen()
        {
            Assert.AreEqual(GreenColor, ConvertToColor("F1"));
        }

        [TestMethod]
        public void F12_ReturnsGreen()
        {
            Assert.AreEqual(GreenColor, ConvertToColor("F12"));
        }

        [TestMethod]
        public void F9_ReturnsGreen()
        {
            Assert.AreEqual(GreenColor, ConvertToColor("F9"));
        }

        [TestMethod]
        public void F10_ReturnsGreen()
        {
            Assert.AreEqual(GreenColor, ConvertToColor("F10"));
        }

        [TestMethod]
        public void F13_ReturnsDefault()
        {
            Assert.AreEqual(DefaultColor, ConvertToColor("F13"));
        }

        [TestMethod]
        public void FilePrefixToken_DoesNotMatchFunctionKey_ReturnsDefault()
        {
            Assert.AreEqual(DefaultColor, ConvertToColor("File"));
        }

        [TestMethod]
        public void CtrlS_ReturnsYellow()
        {
            Assert.AreEqual(YellowColor, ConvertToColor("Ctrl+S"));
        }

        [TestMethod]
        public void AltF4_FKeyWins_ReturnsGreen()
        {
            Assert.AreEqual(GreenColor, ConvertToColor("Alt+F4"));
        }

        [TestMethod]
        public void ShiftA_ReturnsYellow()
        {
            Assert.AreEqual(YellowColor, ConvertToColor("Shift+A"));
        }

        [TestMethod]
        public void PlainLetter_ReturnsDefault()
        {
            Assert.AreEqual(DefaultColor, ConvertToColor("A"));
        }

        [TestMethod]
        public void Null_ReturnsDefault()
        {
            Assert.AreEqual(DefaultColor, ConvertToColor(null));
        }

        [TestMethod]
        public void EmptyString_ReturnsDefault()
        {
            Assert.AreEqual(DefaultColor, ConvertToColor(""));
        }

        [TestMethod]
        public void WinKey_ReturnsYellow()
        {
            Assert.AreEqual(YellowColor, ConvertToColor("Win+D"));
        }

        [TestMethod]
        public void EscKey_ReturnsYellow()
        {
            Assert.AreEqual(YellowColor, ConvertToColor("Esc"));
        }

        [TestMethod]
        public void TabKey_ReturnsYellow()
        {
            Assert.AreEqual(YellowColor, ConvertToColor("Tab"));
        }

        [TestMethod]
        public void MetaKey_ReturnsYellow()
        {
            Assert.AreEqual(YellowColor, ConvertToColor("Meta+Space"));
        }

        [TestMethod]
        public void CaseInsensitiveFunctionKey_ReturnsGreen()
        {
            Assert.AreEqual(GreenColor, ConvertToColor("f5"));
        }

        [TestMethod]
        public void TokensWithSpaces_AreTrimmed()
        {
            Assert.AreEqual(YellowColor, ConvertToColor("Ctrl + S"));
        }
    }
}
