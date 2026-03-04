using Microsoft.VisualStudio.TestTools.UnitTesting;
using WhatKey.ViewModels;

namespace WhatKey.Tests
{
    [TestClass]
    public class OverlayLayoutTests
    {
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
    }
}
