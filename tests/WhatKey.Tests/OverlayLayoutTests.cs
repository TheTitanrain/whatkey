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

            StringAssert.Contains(xaml, "ItemsSource=\"{Binding Groups}\"");
            StringAssert.Contains(xaml, "UniformGrid");
            StringAssert.Contains(xaml, "OverlayColumns");
        }

        [TestMethod]
        public void OverlayWindowXaml_PreservesHotkeyRowStyleAndEmptyState()
        {
            var xaml = LoadOverlayWindowXaml();

            StringAssert.Contains(xaml, "ItemsSource=\"{Binding Keys, Converter={StaticResource KeyTokens}");
            StringAssert.Contains(xaml, "Text=\"{Binding Description}\"");
            StringAssert.Contains(xaml, "Text=\"No hotkeys defined for this application.\"");
        }

        [TestMethod]
        public void OverlayWindowXaml_ConstrainsScrollableAreaForLongLists()
        {
            var xaml = LoadOverlayWindowXaml();

            StringAssert.Contains(xaml, "MaxHeight=\"{Binding HotkeysListMaxHeight}\"");
            StringAssert.Contains(xaml, "VerticalScrollBarVisibility=\"Auto\"");
        }

        [TestMethod]
        public void OverlayViewModel_UsesSharedHotkeysListMaxHeightForUiAndCalculation()
        {
            var viewModel = new OverlayViewModel();

            Assert.AreEqual(OverlayViewModel.DefaultHotkeysListMaxHeight, viewModel.HotkeysListMaxHeight);
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
        public void CalculateOverlayColumns_WhenWidthLimitsToOneColumn_UsesOneColumn()
        {
            var rowsPerColumn = (int)(OverlayViewModel.DefaultHotkeysListMaxHeight / OverlayViewModel.DefaultHotkeyRowHeight);

            var columns = OverlayViewModel.CalculateOverlayColumns(
                hotkeysCount: rowsPerColumn + 10,
                availableWidth: OverlayViewModel.DefaultMinColumnWidth + 10d);

            Assert.AreEqual(1, columns);
        }

        [TestMethod]
        public void CalculateOverlayColumns_WhenWidthLimitsToTwoColumns_CapsAtTwoColumns()
        {
            var rowsPerColumn = (int)(OverlayViewModel.DefaultHotkeysListMaxHeight / OverlayViewModel.DefaultHotkeyRowHeight);

            var columns = OverlayViewModel.CalculateOverlayColumns(
                hotkeysCount: (rowsPerColumn * 3),
                availableWidth: (OverlayViewModel.DefaultMinColumnWidth * 2d) + 20d);

            Assert.AreEqual(2, columns);
        }

        [TestMethod]
        public void CalculateOverlayColumns_WithFiniteWidthAndInvalidMinColumnWidth_ReturnsOneColumn()
        {
            var columns = OverlayViewModel.CalculateOverlayColumns(
                hotkeysCount: 20,
                availableWidth: 600d,
                minColumnWidth: 0d);

            Assert.AreEqual(1, columns);
        }

        [TestMethod]
        public void CalculateOverlayColumns_WithWideFiniteWidth_StillCapsAtThreeColumns()
        {
            var rowsPerColumn = (int)(OverlayViewModel.DefaultHotkeysListMaxHeight / OverlayViewModel.DefaultHotkeyRowHeight);

            var columns = OverlayViewModel.CalculateOverlayColumns(
                hotkeysCount: (rowsPerColumn * 3) + 10,
                availableWidth: OverlayViewModel.DefaultOverlayMaxWidth);

            Assert.AreEqual(OverlayViewModel.MaxOverlayColumns, columns);
        }

        [TestMethod]
        public void OverlayWindowCodeBehind_ShowWithGroups_UpdatesColumnsAndKeepsShowPipeline()
        {
            var codeBehind = LoadSourceFile("Views", "OverlayWindow.xaml.cs");

            StringAssert.Contains(codeBehind, "_viewModel.UpdateLayoutForHotkeysCount(totalHotkeys);");
            StringAssert.Contains(codeBehind, "MinWidth = Math.Min(OverlayViewModel.DefaultOverlayMinWidth, bounds.Width);");
            StringAssert.Contains(codeBehind, "MaxWidth = Math.Min(OverlayViewModel.DefaultOverlayMaxWidth, bounds.Width);");
            StringAssert.Contains(codeBehind, "_viewModel.UpdateLayoutForHotkeysCount(totalHotkeys, MaxWidth);");
            StringAssert.Contains(codeBehind, "var listWidth = GetAvailableHotkeysListWidth();");
            StringAssert.Contains(codeBehind, "_viewModel.UpdateLayoutForHotkeysCount(totalHotkeys, listWidth);");
            StringAssert.Contains(codeBehind, "Left = Math.Max(bounds.Left, Math.Min(centeredLeft, maxLeft));");
            StringAssert.Contains(codeBehind, "Top = Math.Max(bounds.Top, Math.Min(centeredTop, maxTop));");
            StringAssert.Contains(codeBehind, "Dispatcher.BeginInvoke(new Action(() =>");
            StringAssert.Contains(codeBehind, "BeginAnimation(OpacityProperty, fadeIn);");
            StringAssert.Contains(codeBehind, "if (!IsVisible)");
            StringAssert.Contains(codeBehind, "Show();");
        }

        [TestMethod]
        public void Acceptance_LongList_UsesMultiColumnLayout()
        {
            var rowsPerColumn = (int)(OverlayViewModel.DefaultHotkeysListMaxHeight / OverlayViewModel.DefaultHotkeyRowHeight);
            var twoColumnCount = rowsPerColumn + 5;
            var threeColumnCount = (rowsPerColumn * 2) + 5;

            Assert.AreEqual(2, OverlayViewModel.CalculateOverlayColumns(twoColumnCount));
            Assert.AreEqual(3, OverlayViewModel.CalculateOverlayColumns(threeColumnCount));
        }

        [TestMethod]
        public void Acceptance_ShortList_UsesSingleColumnLayout()
        {
            Assert.AreEqual(1, OverlayViewModel.CalculateOverlayColumns(3));
        }

        [TestMethod]
        public void Acceptance_EmptyList_ShowsEmptyStateMessage()
        {
            var viewModel = new OverlayViewModel();

            viewModel.Groups = new System.Collections.ObjectModel.ObservableCollection<WhatKey.Models.HotkeyGroup>();
            Assert.AreEqual("Visible", viewModel.EmptyMessageVisibility.ToString());

            viewModel.Groups.Add(new WhatKey.Models.HotkeyGroup
            {
                Hotkeys = new System.Collections.ObjectModel.ObservableCollection<WhatKey.Models.HotkeyEntry>
                {
                    new WhatKey.Models.HotkeyEntry { Keys = "Ctrl+P", Description = "Quick Open" }
                }
            });
            Assert.AreEqual("Collapsed", viewModel.EmptyMessageVisibility.ToString());
        }

        [TestMethod]
        public void EmptyMessageVisibility_WhenGroupsSetToNull_IsVisible()
        {
            var viewModel = new OverlayViewModel
            {
                Groups = new System.Collections.ObjectModel.ObservableCollection<WhatKey.Models.HotkeyGroup>
                {
                    new WhatKey.Models.HotkeyGroup
                    {
                        Hotkeys = new System.Collections.ObjectModel.ObservableCollection<WhatKey.Models.HotkeyEntry>
                        {
                            new WhatKey.Models.HotkeyEntry()
                        }
                    }
                }
            };

            viewModel.Groups = null;

            Assert.AreEqual("Visible", viewModel.EmptyMessageVisibility.ToString());
        }

        [TestMethod]
        public void CalculateOverlayColumns_WhenRowsPerColumnWouldBeZero_FallsBackToOneRowPerColumn()
        {
            var columns = OverlayViewModel.CalculateOverlayColumns(
                hotkeysCount: 2,
                maxListHeight: 1,
                estimatedRowHeight: 10,
                maxColumns: OverlayViewModel.MaxOverlayColumns);

            Assert.AreEqual(2, columns);
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
