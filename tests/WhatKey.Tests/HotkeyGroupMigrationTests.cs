using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WhatKey.Models;
using WhatKey.Services;

namespace WhatKey.Tests
{
    [TestClass]
    public class HotkeyGroupMigrationTests
    {
        [TestMethod]
        public void NormalizeData_MigratesLegacyFlatHotkeys_ToGeneralGroup()
        {
            using (var scope = new TestStorageScope())
            {
                var storage = scope.CreateStorage();
                Directory.CreateDirectory(scope.DataDir);
                File.WriteAllText(storage.DataFilePath, @"{
                    ""apps"": [{
                        ""processNames"": [""notepad""],
                        ""title"": ""Notepad"",
                        ""hotkeys"": [
                            { ""keys"": ""Ctrl+S"", ""description"": ""Save"" },
                            { ""keys"": ""Ctrl+Z"", ""description"": ""Undo"" }
                        ]
                    }]
                }");

                storage.Load();

                Assert.AreEqual(1, storage.Apps.Count);
                Assert.IsNull(storage.Apps[0].Hotkeys, "Hotkeys field should be null after migration.");
                Assert.IsNotNull(storage.Apps[0].Groups, "Groups should be initialized after migration.");
                Assert.AreEqual(1, storage.Apps[0].Groups.Count);
                Assert.AreEqual("General", storage.Apps[0].Groups[0].Name);
                Assert.AreEqual(2, storage.Apps[0].Groups[0].Hotkeys.Count);
                Assert.AreEqual("Ctrl+S", storage.Apps[0].Groups[0].Hotkeys[0].Keys);
                Assert.AreEqual("Ctrl+Z", storage.Apps[0].Groups[0].Hotkeys[1].Keys);
            }
        }

        [TestMethod]
        public void NormalizeData_WhenAppHasNoHotkeys_InitializesEmptyGroups()
        {
            using (var scope = new TestStorageScope())
            {
                var storage = scope.CreateStorage();
                Directory.CreateDirectory(scope.DataDir);
                File.WriteAllText(storage.DataFilePath, @"{
                    ""apps"": [{
                        ""processNames"": [""notepad""],
                        ""title"": ""Notepad""
                    }]
                }");

                storage.Load();

                Assert.IsNotNull(storage.Apps[0].Groups, "Groups should be initialized even when no hotkeys.");
                Assert.AreEqual(0, storage.Apps[0].Groups.Count);
                Assert.IsNull(storage.Apps[0].Hotkeys, "Hotkeys should be null after normalization.");
            }
        }

        [TestMethod]
        public void NormalizeData_WhenAppAlreadyHasGroups_DoesNotDuplicateMigration()
        {
            using (var scope = new TestStorageScope())
            {
                var storage = scope.CreateStorage();
                Directory.CreateDirectory(scope.DataDir);
                File.WriteAllText(storage.DataFilePath, @"{
                    ""apps"": [{
                        ""processNames"": [""code""],
                        ""title"": ""VS Code"",
                        ""groups"": [
                            {
                                ""name"": ""File"",
                                ""hotkeys"": [{ ""keys"": ""Ctrl+N"", ""description"": ""New File"" }]
                            },
                            {
                                ""name"": ""Edit"",
                                ""hotkeys"": [{ ""keys"": ""Ctrl+Z"", ""description"": ""Undo"" }]
                            }
                        ]
                    }]
                }");

                storage.Load();

                Assert.AreEqual(2, storage.Apps[0].Groups.Count, "Groups should not be duplicated.");
                Assert.AreEqual("File", storage.Apps[0].Groups[0].Name);
                Assert.AreEqual("Edit", storage.Apps[0].Groups[1].Name);
            }
        }

        [TestMethod]
        public void GetGroupsForProcess_ReturnsCorrectGroups()
        {
            using (var scope = new TestStorageScope())
            {
                var storage = scope.CreateStorage();
                storage.Apps.Add(new AppHotkeys
                {
                    ProcessNames = new List<string> { "code" },
                    Title = "VS Code",
                    Groups = new ObservableCollection<HotkeyGroup>
                    {
                        new HotkeyGroup
                        {
                            Name = "File",
                            Hotkeys = new ObservableCollection<HotkeyEntry>
                            {
                                new HotkeyEntry { Keys = "Ctrl+N", Description = "New File" }
                            }
                        },
                        new HotkeyGroup
                        {
                            Name = "Edit",
                            Hotkeys = new ObservableCollection<HotkeyEntry>
                            {
                                new HotkeyEntry { Keys = "Ctrl+Z", Description = "Undo" },
                                new HotkeyEntry { Keys = "Ctrl+Y", Description = "Redo" }
                            }
                        }
                    }
                });

                var groups = storage.GetGroupsForProcess("code");

                Assert.AreEqual(2, groups.Count);
                Assert.AreEqual("File", groups[0].Name);
                Assert.AreEqual(1, groups[0].Hotkeys.Count);
                Assert.AreEqual("Edit", groups[1].Name);
                Assert.AreEqual(2, groups[1].Hotkeys.Count);
            }
        }

        [TestMethod]
        public void GetGroupsForProcess_WhenNoMatch_FallsBackToDefault()
        {
            using (var scope = new TestStorageScope())
            {
                var storage = scope.CreateStorage();
                storage.Apps.Add(new AppHotkeys
                {
                    ProcessNames = new List<string> { "default" },
                    Title = "Default",
                    Groups = new ObservableCollection<HotkeyGroup>
                    {
                        new HotkeyGroup
                        {
                            Name = "General",
                            Hotkeys = new ObservableCollection<HotkeyEntry>
                            {
                                new HotkeyEntry { Keys = "F1", Description = "Help" }
                            }
                        }
                    }
                });

                var groups = storage.GetGroupsForProcess("unknownapp");

                Assert.AreEqual(1, groups.Count);
                Assert.AreEqual("General", groups[0].Name);
                Assert.AreEqual("F1", groups[0].Hotkeys[0].Keys);
            }
        }

        [TestMethod]
        public void GetGroupsForProcess_WhenNoMatchAndNoDefault_ReturnsEmptyList()
        {
            using (var scope = new TestStorageScope())
            {
                var storage = scope.CreateStorage();
                storage.Apps.Add(new AppHotkeys
                {
                    ProcessNames = new List<string> { "someapp" },
                    Title = "Some App"
                });

                var groups = storage.GetGroupsForProcess("unknownapp");

                Assert.AreEqual(0, groups.Count);
            }
        }

        [TestMethod]
        public void GetHotkeysForProcess_FlattensAllGroupsHotkeys()
        {
            using (var scope = new TestStorageScope())
            {
                var storage = scope.CreateStorage();
                storage.Apps.Add(new AppHotkeys
                {
                    ProcessNames = new List<string> { "code" },
                    Title = "VS Code",
                    Groups = new ObservableCollection<HotkeyGroup>
                    {
                        new HotkeyGroup
                        {
                            Name = "File",
                            Hotkeys = new ObservableCollection<HotkeyEntry>
                            {
                                new HotkeyEntry { Keys = "Ctrl+N", Description = "New File" }
                            }
                        },
                        new HotkeyGroup
                        {
                            Name = "Edit",
                            Hotkeys = new ObservableCollection<HotkeyEntry>
                            {
                                new HotkeyEntry { Keys = "Ctrl+Z", Description = "Undo" },
                                new HotkeyEntry { Keys = "Ctrl+Y", Description = "Redo" }
                            }
                        }
                    }
                });

                var hotkeys = storage.GetHotkeysForProcess("code");

                Assert.AreEqual(3, hotkeys.Count);
                Assert.AreEqual("Ctrl+N", hotkeys[0].Keys);
                Assert.AreEqual("Ctrl+Z", hotkeys[1].Keys);
                Assert.AreEqual("Ctrl+Y", hotkeys[2].Keys);
            }
        }

        [TestMethod]
        public void NormalizeData_WhenLegacyHotkeysAndGroupsBothPresent_OnlyMigratesHotkeys()
        {
            using (var scope = new TestStorageScope())
            {
                var storage = scope.CreateStorage();
                Directory.CreateDirectory(scope.DataDir);
                File.WriteAllText(storage.DataFilePath, @"{
                    ""apps"": [{
                        ""processNames"": [""notepad""],
                        ""title"": ""Notepad"",
                        ""groups"": [
                            { ""name"": ""Existing"", ""hotkeys"": [{ ""keys"": ""F1"", ""description"": ""Help"" }] }
                        ],
                        ""hotkeys"": [
                            { ""keys"": ""Ctrl+S"", ""description"": ""Save"" }
                        ]
                    }]
                }");

                storage.Load();

                Assert.AreEqual(2, storage.Apps[0].Groups.Count,
                    "Legacy hotkeys should be appended as 'General' group alongside existing groups.");
                Assert.AreEqual("Existing", storage.Apps[0].Groups[0].Name);
                Assert.AreEqual("General", storage.Apps[0].Groups[1].Name);
                Assert.AreEqual("Ctrl+S", storage.Apps[0].Groups[1].Hotkeys[0].Keys);
            }
        }
    }
}
