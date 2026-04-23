using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Globalization;
using System.Linq;
using System.Windows.Media;
using WhatKey.Converters;

namespace WhatKey.Tests
{
    [TestClass]
    public class KeyTokensConverterTests
    {
        private readonly KeyTokensConverter _converter = new KeyTokensConverter();

        [TestMethod]
        public void NullInput_ReturnsEmptyList()
        {
            var result = _converter.Convert(null, typeof(object), null, CultureInfo.InvariantCulture);

            Assert.IsNotNull(result);
            var list = result as System.Collections.Generic.List<KeyToken>;
            Assert.AreEqual(0, list.Count);
        }

        [TestMethod]
        public void EmptyString_ReturnsEmptyList()
        {
            var result = _converter.Convert("", typeof(object), null, CultureInfo.InvariantCulture);

            var list = result as System.Collections.Generic.List<KeyToken>;
            Assert.AreEqual(0, list.Count);
        }

        [TestMethod]
        public void WhitespaceOnlyString_ReturnsEmptyList()
        {
            var result = _converter.Convert("   ", typeof(object), null, CultureInfo.InvariantCulture);

            var list = result as System.Collections.Generic.List<KeyToken>;
            Assert.AreEqual(0, list.Count);
        }

        [TestMethod]
        public void SinglePlainKey_ReturnsSingleTokenWithDefaultBrush()
        {
            var result = _converter.Convert("A", typeof(object), null, CultureInfo.InvariantCulture);

            var list = result as System.Collections.Generic.List<KeyToken>;
            Assert.AreEqual(1, list.Count);
            Assert.AreEqual("A", list[0].Text);
            AssertColorEquals(0xCD, 0xD6, 0xF4, list[0].Brush); // default color
        }

        [TestMethod]
        public void FunctionKeyF1_ReturnsGreenBrush()
        {
            var result = _converter.Convert("F1", typeof(object), null, CultureInfo.InvariantCulture);

            var list = result as System.Collections.Generic.List<KeyToken>;
            Assert.AreEqual(1, list.Count);
            AssertColorEquals(0xA6, 0xE3, 0xA1, list[0].Brush); // green
        }

        [TestMethod]
        public void FunctionKeyF12_ReturnsGreenBrush()
        {
            var result = _converter.Convert("F12", typeof(object), null, CultureInfo.InvariantCulture);

            var list = result as System.Collections.Generic.List<KeyToken>;
            Assert.AreEqual(1, list.Count);
            AssertColorEquals(0xA6, 0xE3, 0xA1, list[0].Brush); // green
        }

        [TestMethod]
        public void FunctionKeyF13_ReturnsDefaultBrush()
        {
            var result = _converter.Convert("F13", typeof(object), null, CultureInfo.InvariantCulture);

            var list = result as System.Collections.Generic.List<KeyToken>;
            Assert.AreEqual(1, list.Count);
            AssertColorEquals(0xCD, 0xD6, 0xF4, list[0].Brush); // default, not function
        }

        [TestMethod]
        public void ModifierCtrl_ReturnsYellowBrush()
        {
            var result = _converter.Convert("Ctrl", typeof(object), null, CultureInfo.InvariantCulture);

            var list = result as System.Collections.Generic.List<KeyToken>;
            Assert.AreEqual(1, list.Count);
            AssertColorEquals(0xF9, 0xE2, 0xAF, list[0].Brush); // yellow
        }

        [TestMethod]
        public void ModifierAlt_ReturnsYellowBrush()
        {
            var result = _converter.Convert("Alt", typeof(object), null, CultureInfo.InvariantCulture);

            var list = result as System.Collections.Generic.List<KeyToken>;
            Assert.AreEqual(1, list.Count);
            AssertColorEquals(0xF9, 0xE2, 0xAF, list[0].Brush); // yellow
        }

        [TestMethod]
        public void ModifierShift_ReturnsYellowBrush()
        {
            var result = _converter.Convert("Shift", typeof(object), null, CultureInfo.InvariantCulture);

            var list = result as System.Collections.Generic.List<KeyToken>;
            Assert.AreEqual(1, list.Count);
            AssertColorEquals(0xF9, 0xE2, 0xAF, list[0].Brush); // yellow
        }

        [TestMethod]
        public void ModifierWin_ReturnsYellowBrush()
        {
            var result = _converter.Convert("Win", typeof(object), null, CultureInfo.InvariantCulture);

            var list = result as System.Collections.Generic.List<KeyToken>;
            Assert.AreEqual(1, list.Count);
            AssertColorEquals(0xF9, 0xE2, 0xAF, list[0].Brush); // yellow
        }

        [TestMethod]
        public void ModifierEsc_ReturnsYellowBrush()
        {
            var result = _converter.Convert("Esc", typeof(object), null, CultureInfo.InvariantCulture);

            var list = result as System.Collections.Generic.List<KeyToken>;
            Assert.AreEqual(1, list.Count);
            AssertColorEquals(0xF9, 0xE2, 0xAF, list[0].Brush); // yellow
        }

        [TestMethod]
        public void ModifierTab_ReturnsYellowBrush()
        {
            var result = _converter.Convert("Tab", typeof(object), null, CultureInfo.InvariantCulture);

            var list = result as System.Collections.Generic.List<KeyToken>;
            Assert.AreEqual(1, list.Count);
            AssertColorEquals(0xF9, 0xE2, 0xAF, list[0].Brush); // yellow
        }

        [TestMethod]
        public void ModifierMeta_ReturnsYellowBrush()
        {
            var result = _converter.Convert("Meta", typeof(object), null, CultureInfo.InvariantCulture);

            var list = result as System.Collections.Generic.List<KeyToken>;
            Assert.AreEqual(1, list.Count);
            AssertColorEquals(0xF9, 0xE2, 0xAF, list[0].Brush); // yellow
        }

        [TestMethod]
        public void CaseInsensitiveModifier_CtrlLowercase_ReturnsYellowBrush()
        {
            var result = _converter.Convert("ctrl", typeof(object), null, CultureInfo.InvariantCulture);

            var list = result as System.Collections.Generic.List<KeyToken>;
            Assert.AreEqual(1, list.Count);
            AssertColorEquals(0xF9, 0xE2, 0xAF, list[0].Brush); // yellow
        }

        [TestMethod]
        public void SingleChordCtrlK_ReturnsFourTokens()
        {
            var result = _converter.Convert("Ctrl+K", typeof(object), null, CultureInfo.InvariantCulture);

            var list = result as System.Collections.Generic.List<KeyToken>;
            Assert.AreEqual(3, list.Count);
            Assert.AreEqual("Ctrl", list[0].Text);
            AssertColorEquals(0xF9, 0xE2, 0xAF, list[0].Brush); // yellow
            Assert.AreEqual("+", list[1].Text);
            AssertColorEquals(0x6C, 0x70, 0x86, list[1].Brush); // separator gray
            Assert.AreEqual("K", list[2].Text);
            AssertColorEquals(0xCD, 0xD6, 0xF4, list[2].Brush); // default
        }

        [TestMethod]
        public void MultiChordCtrlKCtrlW_ReturnsSixTokensWithSpaceSeparator()
        {
            var result = _converter.Convert("Ctrl+K Ctrl+W", typeof(object), null, CultureInfo.InvariantCulture);

            var list = result as System.Collections.Generic.List<KeyToken>;
            Assert.AreEqual(7, list.Count);
            // Ctrl
            Assert.AreEqual("Ctrl", list[0].Text);
            // +
            Assert.AreEqual("+", list[1].Text);
            // K
            Assert.AreEqual("K", list[2].Text);
            // Space separator
            Assert.AreEqual(" ", list[3].Text);
            // Ctrl
            Assert.AreEqual("Ctrl", list[4].Text);
            // +
            Assert.AreEqual("+", list[5].Text);
            // W
            Assert.AreEqual("W", list[6].Text);
        }

        [TestMethod]
        public void MultiChordWithDoubleSpace_SkipsEmptyChord()
        {
            var result = _converter.Convert("Ctrl+K  Ctrl+W", typeof(object), null, CultureInfo.InvariantCulture);

            var list = result as System.Collections.Generic.List<KeyToken>;
            // Should be same as single space - no orphan separator
            Assert.AreEqual(7, list.Count);
            Assert.AreEqual("Ctrl", list[0].Text);
            Assert.AreEqual("+", list[1].Text);
            Assert.AreEqual("K", list[2].Text);
            Assert.AreEqual(" ", list[3].Text);
            Assert.AreEqual("Ctrl", list[4].Text);
            Assert.AreEqual("+", list[5].Text);
            Assert.AreEqual("W", list[6].Text);
        }

        [TestMethod]
        public void TrailingSpace_IgnoredInParsing()
        {
            var result = _converter.Convert("Ctrl+K ", typeof(object), null, CultureInfo.InvariantCulture);

            var list = result as System.Collections.Generic.List<KeyToken>;
            // Should be same as without trailing space
            Assert.AreEqual(3, list.Count);
            Assert.AreEqual("Ctrl", list[0].Text);
            Assert.AreEqual("+", list[1].Text);
            Assert.AreEqual("K", list[2].Text);
        }

        [TestMethod]
        public void LeadingSpace_IgnoredInParsing()
        {
            var result = _converter.Convert(" Ctrl+K", typeof(object), null, CultureInfo.InvariantCulture);

            var list = result as System.Collections.Generic.List<KeyToken>;
            // Should be same as without leading space
            Assert.AreEqual(3, list.Count);
            Assert.AreEqual("Ctrl", list[0].Text);
            Assert.AreEqual("+", list[1].Text);
            Assert.AreEqual("K", list[2].Text);
        }

        [TestMethod]
        public void ComplexChordWithMultipleModifiers_ReturnsCorrectTokens()
        {
            var result = _converter.Convert("Ctrl+Shift+Alt+A", typeof(object), null, CultureInfo.InvariantCulture);

            var list = result as System.Collections.Generic.List<KeyToken>;
            Assert.AreEqual(7, list.Count);
            Assert.AreEqual("Ctrl", list[0].Text);
            Assert.AreEqual("+", list[1].Text);
            Assert.AreEqual("Shift", list[2].Text);
            Assert.AreEqual("+", list[3].Text);
            Assert.AreEqual("Alt", list[4].Text);
            Assert.AreEqual("+", list[5].Text);
            Assert.AreEqual("A", list[6].Text);
        }

        [TestMethod]
        public void FunctionKeyInChord_ReturnsGreenForFKey()
        {
            var result = _converter.Convert("Shift+F5", typeof(object), null, CultureInfo.InvariantCulture);

            var list = result as System.Collections.Generic.List<KeyToken>;
            Assert.AreEqual(3, list.Count);
            // Shift - yellow
            AssertColorEquals(0xF9, 0xE2, 0xAF, list[0].Brush);
            // +
            AssertColorEquals(0x6C, 0x70, 0x86, list[1].Brush);
            // F5 - green
            AssertColorEquals(0xA6, 0xE3, 0xA1, list[2].Brush);
        }

        private void AssertColorEquals(byte expectedR, byte expectedG, byte expectedB, Brush brush)
        {
            var solidBrush = brush as SolidColorBrush;
            Assert.IsNotNull(solidBrush);
            var color = solidBrush.Color;
            Assert.AreEqual(expectedR, color.R, $"Red component mismatch. Expected {expectedR:X2}, got {color.R:X2}");
            Assert.AreEqual(expectedG, color.G, $"Green component mismatch. Expected {expectedG:X2}, got {color.G:X2}");
            Assert.AreEqual(expectedB, color.B, $"Blue component mismatch. Expected {expectedB:X2}, got {color.B:X2}");
        }
    }
}
