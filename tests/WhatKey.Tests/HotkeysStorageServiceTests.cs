using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WhatKey.Models;
using WhatKey.Services;

namespace WhatKey.Tests
{
    [TestClass]
    public class HotkeysStorageServiceTests
    {
        [TestMethod]
        public void Load_WhenFileNotFound_ReturnsDefaultsAndSavesFile()
        {
            using (var scope = new TestStorageScope())
            {
                var storage = scope.CreateStorage();

                var result = storage.Load();

                Assert.AreEqual(HotkeysLoadStatus.MissingFileLoadedDefaults, result.Status);
                Assert.IsTrue(File.Exists(storage.DataFilePath));
            }
        }

        [TestMethod]
        public void Load_WhenFileExists_ReturnsSuccess()
        {
            using (var scope = new TestStorageScope())
            {
                var storage = scope.CreateStorage();
                storage.Settings.HoldDelayMs = 750;
                storage.Save();

                var result = storage.Load();

                Assert.AreEqual(HotkeysLoadStatus.Success, result.Status);
                Assert.AreEqual(750, storage.Settings.HoldDelayMs);
            }
        }

        [TestMethod]
        public void Load_WhenFileIsInvalidJson_ReturnsInvalidStatus()
        {
            using (var scope = new TestStorageScope())
            {
                var storage = scope.CreateStorage();
                Directory.CreateDirectory(scope.DataDir);
                File.WriteAllText(storage.DataFilePath, "{ this is not valid json !!!}}}");

                var result = storage.Load();

                Assert.AreEqual(HotkeysLoadStatus.InvalidFormat, result.Status);
                Assert.IsFalse(string.IsNullOrEmpty(result.ErrorMessage));
            }
        }

        [TestMethod]
        public void Save_PersistsSettingsAndApps()
        {
            using (var scope = new TestStorageScope())
            {
                var storage = scope.CreateStorage();
                storage.Settings.HoldKey = "RShiftKey";
                storage.Settings.HoldDelayMs = 1200;
                storage.Apps.Add(new AppHotkeys
                {
                    ProcessNames = new List<string> { "notepad" },
                    Title = "Notepad",
                    Hotkeys = new ObservableCollection<HotkeyEntry>
                    {
                        new HotkeyEntry { Keys = "Ctrl+S", Description = "Save" }
                    }
                });
                storage.Save();

                var storage2 = new HotkeysStorageService(scope.DataDir);
                storage2.Load();

                Assert.AreEqual("RShiftKey", storage2.Settings.HoldKey);
                Assert.AreEqual(1200, storage2.Settings.HoldDelayMs);
                Assert.AreEqual(1, storage2.Apps.Count);
                Assert.IsTrue(storage2.Apps[0].ProcessNames.Contains("notepad"));
                Assert.AreEqual("Save", storage2.Apps[0].Hotkeys[0].Description);
            }
        }

        [TestMethod]
        public void GetHotkeysForProcess_ReturnsMatchingApp()
        {
            using (var scope = new TestStorageScope())
            {
                var storage = scope.CreateStorage();
                storage.Apps.Add(new AppHotkeys
                {
                    ProcessNames = new List<string> { "notepad" },
                    Title = "Notepad",
                    Hotkeys = new ObservableCollection<HotkeyEntry>
                    {
                        new HotkeyEntry { Keys = "Ctrl+S", Description = "Save" }
                    }
                });

                var hotkeys = storage.GetHotkeysForProcess("notepad");

                Assert.AreEqual(1, hotkeys.Count);
                Assert.AreEqual("Ctrl+S", hotkeys[0].Keys);
            }
        }

        [TestMethod]
        public void GetHotkeysForProcess_WhenNoMatch_FallsBackToDefault()
        {
            using (var scope = new TestStorageScope())
            {
                var storage = scope.CreateStorage();
                storage.Apps.Add(new AppHotkeys
                {
                    ProcessNames = new List<string> { "default" },
                    Title = "Default",
                    Hotkeys = new ObservableCollection<HotkeyEntry>
                    {
                        new HotkeyEntry { Keys = "F1", Description = "Help" }
                    }
                });

                var hotkeys = storage.GetHotkeysForProcess("unknownapp");

                Assert.AreEqual(1, hotkeys.Count);
                Assert.AreEqual("F1", hotkeys[0].Keys);
            }
        }

        [TestMethod]
        public void GetHotkeysForProcess_WhenNeitherExists_ReturnsEmptyList()
        {
            using (var scope = new TestStorageScope())
            {
                var storage = scope.CreateStorage();
                storage.Apps.Add(new AppHotkeys
                {
                    ProcessNames = new List<string> { "someapp" },
                    Title = "Some App"
                });

                var hotkeys = storage.GetHotkeysForProcess("unknownapp");

                Assert.AreEqual(0, hotkeys.Count);
            }
        }

        [TestMethod]
        public void Load_EnsuresSettingsNotNull()
        {
            using (var scope = new TestStorageScope())
            {
                var storage = scope.CreateStorage();
                Directory.CreateDirectory(scope.DataDir);
                File.WriteAllText(storage.DataFilePath, "{\"Settings\": null, \"Apps\": []}");

                storage.Load();

                Assert.IsNotNull(storage.Settings);
            }
        }

        [TestMethod]
        public void GetHotkeysForProcess_MatchesSecondaryProcessName()
        {
            using (var scope = new TestStorageScope())
            {
                var storage = scope.CreateStorage();
                storage.Apps.Add(new AppHotkeys
                {
                    ProcessNames = new List<string> { "totalcmd", "totalcmd64" },
                    Title = "Total Commander",
                    Hotkeys = new ObservableCollection<HotkeyEntry>
                    {
                        new HotkeyEntry { Keys = "F5", Description = "Copy File(s)" }
                    }
                });

                var hotkeysFor32 = storage.GetHotkeysForProcess("totalcmd");
                var hotkeysFor64 = storage.GetHotkeysForProcess("totalcmd64");

                Assert.AreEqual(1, hotkeysFor32.Count);
                Assert.AreEqual("F5", hotkeysFor32[0].Keys);
                Assert.AreEqual(1, hotkeysFor64.Count);
                Assert.AreEqual("F5", hotkeysFor64[0].Keys);
            }
        }

        [TestMethod]
        public void Load_MigratesLegacySingleProcessName()
        {
            using (var scope = new TestStorageScope())
            {
                var storage = scope.CreateStorage();
                Directory.CreateDirectory(scope.DataDir);
                File.WriteAllText(storage.DataFilePath,
                    "{\"apps\": [{\"processName\": \"notepad\", \"title\": \"Notepad\", \"hotkeys\": []}]}");

                storage.Load();

                Assert.AreEqual(1, storage.Apps.Count);
                Assert.IsTrue(storage.Apps[0].ProcessNames.Contains("notepad"),
                    "Legacy processName should be migrated into ProcessNames list.");
                Assert.IsNull(storage.Apps[0].ProcessName,
                    "Legacy processName field should be cleared after migration.");

                var hotkeys = storage.GetHotkeysForProcess("notepad");
                Assert.IsNotNull(hotkeys, "GetHotkeysForProcess should find the migrated entry.");
            }
        }
    }
}
