using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using WhatKey.ViewModels;

namespace WhatKey.Tests
{
    [TestClass]
    public class OverlayLayoutTests
    {
        [TestMethod]
        public void OverlayWindowXaml_UsesUniformGridWithOverlayColumnsBinding()
        {
            var xaml = LoadOverlayWindowXaml();

            StringAssert.Contains(xaml, "<UniformGrid Columns=\"{Binding OverlayColumns, FallbackValue=1}\"/>");
        }

        [TestMethod]
        public void OverlayWindowXaml_PreservesHotkeyRowStyleAndEmptyState()
        {
            var xaml = LoadOverlayWindowXaml();

            StringAssert.Contains(xaml, "Text=\"{Binding Keys}\"");
            StringAssert.Contains(xaml, "Text=\"{Binding Description}\"");
            StringAssert.Contains(xaml, "Text=\"No hotkeys defined for this application.\"");
        }

        [TestMethod]
        public void OverlayWindowXaml_ConstrainsScrollableAreaForLongLists()
        {
            var xaml = LoadOverlayWindowXaml();

            StringAssert.Contains(xaml, "<ScrollViewer MaxHeight=\"460\"");
            StringAssert.Contains(xaml, "VerticalScrollBarVisibility=\"Auto\"");
        }

        [TestMethod]
        public void CalculateOverlayColumns_NoHotkeys_ReturnsOneColumn()
        {
            var columns = OverlayViewModel.CalculateOverlayColumns(0);

            Assert.AreEqual(1, columns);
        }

        [TestMethod]
        public void CalculateOverlayColumns_UpToRowsPerColumn_UsesOneColumn()
        {
            var rowsPerColumn = (int)(OverlayViewModel.DefaultHotkeysListMaxHeight / OverlayViewModel.DefaultHotkeyRowHeight);

            var columns = OverlayViewModel.CalculateOverlayColumns(rowsPerColumn);

            Assert.AreEqual(1, columns);
        }

        [TestMethod]
        public void CalculateOverlayColumns_AboveRowsPerColumn_UsesTwoColumns()
        {
            var rowsPerColumn = (int)(OverlayViewModel.DefaultHotkeysListMaxHeight / OverlayViewModel.DefaultHotkeyRowHeight);

            var columns = OverlayViewModel.CalculateOverlayColumns(rowsPerColumn + 1);

            Assert.AreEqual(2, columns);
        }

        [TestMethod]
        public void CalculateOverlayColumns_AboveTwoColumnsCapacity_UsesThreeColumns()
        {
            var rowsPerColumn = (int)(OverlayViewModel.DefaultHotkeysListMaxHeight / OverlayViewModel.DefaultHotkeyRowHeight);

            var columns = OverlayViewModel.CalculateOverlayColumns((rowsPerColumn * 2) + 1);

            Assert.AreEqual(3, columns);
        }

        [TestMethod]
        public void CalculateOverlayColumns_AboveThreeColumnsCapacity_CapsAtThreeColumns()
        {
            var rowsPerColumn = (int)(OverlayViewModel.DefaultHotkeysListMaxHeight / OverlayViewModel.DefaultHotkeyRowHeight);

            var columns = OverlayViewModel.CalculateOverlayColumns((rowsPerColumn * 3) + 100);

            Assert.AreEqual(3, columns);
        }

        [TestMethod]
        public void CalculateOverlayColumns_InvalidLayoutParameters_ReturnsOneColumn()
        {
            var columns = OverlayViewModel.CalculateOverlayColumns(
                hotkeysCount: 20,
                maxListHeight: 0,
                estimatedRowHeight: -1,
                maxColumns: 0);

            Assert.AreEqual(1, columns);
        }

        private static string LoadOverlayWindowXaml()
        {
            var directory = AppContext.BaseDirectory;

            while (!string.IsNullOrEmpty(directory))
            {
                var xamlPath = Path.Combine(directory, "Views", "OverlayWindow.xaml");
                if (File.Exists(xamlPath))
                    return File.ReadAllText(xamlPath);

                directory = Directory.GetParent(directory)?.FullName;
            }

            Assert.Fail("Unable to locate Views/OverlayWindow.xaml from test base directory.");
            return string.Empty;
        }
    }
}
