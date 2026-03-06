using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WhatKey.Models;
using WhatKey.Services;
using WhatKey.ViewModels;

namespace WhatKey.Tests
{
    [TestClass]
    public class ProcessNamesTextTests
    {
        private static EditorViewModel CreateViewModel(TestStorageScope scope)
        {
            return new EditorViewModel(scope.CreateStorage(), new ActiveWindowService());
        }

        private static AppHotkeys SelectApp(EditorViewModel vm, string rawNames)
        {
            var app = new AppHotkeys { ProcessNames = new List<string>(), Title = "Test" };
            vm.Apps.Add(app);
            vm.SelectedApp = app;
            vm.ProcessNamesText = rawNames;
            return app;
        }

        private static void FlushByChangingSelection(EditorViewModel vm)
        {
            var dummy = new AppHotkeys { ProcessNames = new List<string>(), Title = "Dummy" };
            vm.Apps.Add(dummy);
            vm.SelectedApp = dummy;
        }

        [TestMethod]
        public void FlushProcessNamesText_CommaSeparated_ParsesAllNames()
        {
            using (var scope = new TestStorageScope())
            {
                var vm = CreateViewModel(scope);
                var app = SelectApp(vm, "notepad,code,explorer");

                FlushByChangingSelection(vm);

                CollectionAssert.AreEquivalent(
                    new[] { "notepad", "code", "explorer" },
                    app.ProcessNames);
            }
        }

        [TestMethod]
        public void FlushProcessNamesText_SemicolonSeparated_ParsesAllNames()
        {
            using (var scope = new TestStorageScope())
            {
                var vm = CreateViewModel(scope);
                var app = SelectApp(vm, "notepad;code");

                FlushByChangingSelection(vm);

                CollectionAssert.AreEquivalent(new[] { "notepad", "code" }, app.ProcessNames);
            }
        }

        [TestMethod]
        public void FlushProcessNamesText_MixedSeparators_ParsesAllNames()
        {
            using (var scope = new TestStorageScope())
            {
                var vm = CreateViewModel(scope);
                var app = SelectApp(vm, "totalcmd,totalcmd64;explorer");

                FlushByChangingSelection(vm);

                CollectionAssert.AreEquivalent(
                    new[] { "totalcmd", "totalcmd64", "explorer" },
                    app.ProcessNames);
            }
        }

        [TestMethod]
        public void FlushProcessNamesText_TrimsWhitespace()
        {
            using (var scope = new TestStorageScope())
            {
                var vm = CreateViewModel(scope);
                var app = SelectApp(vm, "  notepad  ,  code  ");

                FlushByChangingSelection(vm);

                CollectionAssert.AreEquivalent(new[] { "notepad", "code" }, app.ProcessNames);
            }
        }

        [TestMethod]
        public void FlushProcessNamesText_NormalizesToLowerCase()
        {
            using (var scope = new TestStorageScope())
            {
                var vm = CreateViewModel(scope);
                var app = SelectApp(vm, "NOTEPAD,Code,EXPLORER");

                FlushByChangingSelection(vm);

                CollectionAssert.AreEquivalent(
                    new[] { "notepad", "code", "explorer" },
                    app.ProcessNames);
            }
        }

        [TestMethod]
        public void FlushProcessNamesText_EmptyEntries_AreFiltered()
        {
            using (var scope = new TestStorageScope())
            {
                var vm = CreateViewModel(scope);
                var app = SelectApp(vm, ",notepad,,code,");

                FlushByChangingSelection(vm);

                CollectionAssert.AreEquivalent(new[] { "notepad", "code" }, app.ProcessNames);
            }
        }

        [TestMethod]
        public void FlushProcessNamesText_EmptyString_ResultsInEmptyList()
        {
            using (var scope = new TestStorageScope())
            {
                var vm = CreateViewModel(scope);
                var app = SelectApp(vm, "");

                FlushByChangingSelection(vm);

                Assert.AreEqual(0, app.ProcessNames.Count);
            }
        }

        [TestMethod]
        public void FlushProcessNamesText_WhitespaceOnlyEntries_AreFiltered()
        {
            using (var scope = new TestStorageScope())
            {
                var vm = CreateViewModel(scope);
                var app = SelectApp(vm, " , , ");

                FlushByChangingSelection(vm);

                Assert.AreEqual(0, app.ProcessNames.Count);
            }
        }

        [TestMethod]
        public void FlushProcessNamesText_TriggeredBySave()
        {
            using (var scope = new TestStorageScope())
            {
                var storage = scope.CreateStorage();
                var vm = new EditorViewModel(storage, new ActiveWindowService());

                var app = new AppHotkeys { ProcessNames = new List<string> { "old" }, Title = "Test" };
                vm.Apps.Add(app);
                vm.SelectedApp = app;
                vm.ProcessNamesText = "NEW , NAMES";

                vm.SaveCommand.Execute(null);

                CollectionAssert.AreEquivalent(new[] { "new", "names" }, app.ProcessNames);
            }
        }

        [TestMethod]
        public void ProcessNamesText_ReflectsCurrentSelectionNames()
        {
            using (var scope = new TestStorageScope())
            {
                var vm = CreateViewModel(scope);
                var app = new AppHotkeys
                {
                    ProcessNames = new List<string> { "notepad", "wordpad" },
                    Title = "Test"
                };
                vm.Apps.Add(app);
                vm.SelectedApp = app;

                Assert.IsTrue(vm.ProcessNamesText.Contains("notepad"));
                Assert.IsTrue(vm.ProcessNamesText.Contains("wordpad"));
            }
        }
    }
}
