using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
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

        [TestMethod]
        public void UpdateLayoutForHotkeysCount_RecalculatesColumns_WhenCountChanges()
        {
            var viewModel = new OverlayViewModel();
            var rowsPerColumn = (int)(OverlayViewModel.DefaultHotkeysListMaxHeight / OverlayViewModel.DefaultHotkeyRowHeight);

            viewModel.UpdateLayoutForHotkeysCount(rowsPerColumn);
            Assert.AreEqual(1, viewModel.OverlayColumns);

            viewModel.UpdateLayoutForHotkeysCount(rowsPerColumn + 1);
            Assert.AreEqual(2, viewModel.OverlayColumns);

            viewModel.UpdateLayoutForHotkeysCount((rowsPerColumn * 2) + 1);
            Assert.AreEqual(3, viewModel.OverlayColumns);
        }

        [TestMethod]
        public void OverlayWindowCodeBehind_ShowWithHotkeys_UpdatesColumnsAndKeepsShowPipeline()
        {
            var codeBehind = LoadSourceFile("Views", "OverlayWindow.xaml.cs");

            StringAssert.Contains(codeBehind, "_viewModel.UpdateLayoutForHotkeysCount(safeHotkeys.Count);");
            StringAssert.Contains(codeBehind, "Dispatcher.BeginInvoke(new Action(() =>");
            StringAssert.Contains(codeBehind, "BeginAnimation(OpacityProperty, fadeIn);");
            StringAssert.Contains(codeBehind, "if (!IsVisible)");
            StringAssert.Contains(codeBehind, "Show();");
        }

        private static string LoadOverlayWindowXaml()
        {
            return LoadSourceFile("Views", "OverlayWindow.xaml");
        }

        private static string LoadSourceFile(params string[] relativePathParts)
        {
            var directory = AppContext.BaseDirectory;

            while (!string.IsNullOrEmpty(directory))
            {
                var sourcePath = Path.Combine(relativePathParts.Prepend(directory).ToArray());
                if (File.Exists(sourcePath))
                    return File.ReadAllText(sourcePath);

                directory = Directory.GetParent(directory)?.FullName;
            }

            Assert.Fail($"Unable to locate {Path.Combine(relativePathParts)} from test base directory.");
            return string.Empty;
        }
    }
}
