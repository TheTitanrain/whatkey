using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WhatKey.Models;
using WhatKey.Services;
using WhatKey.ViewModels;

namespace WhatKey.Tests
{
    internal sealed class StubActiveWindowService : IActiveWindowService
    {
        public string ProcessNameToReturn { get; set; } = "stubprocess";

        public string GetActiveProcessName() => ProcessNameToReturn;

        public (string ProcessName, IntPtr Hwnd) GetActiveWindowInfo()
            => (ProcessNameToReturn, IntPtr.Zero);
    }

    [TestClass]
    public class EditorViewModelCommandTests
    {
        private static EditorViewModel Create(TestStorageScope scope, IActiveWindowService activeWindow = null)
        {
            return new EditorViewModel(scope.CreateStorage(), activeWindow ?? new StubActiveWindowService());
        }

        private static AppHotkeys MakeApp(string processName, string title = "Test")
        {
            return new AppHotkeys { ProcessNames = new List<string> { processName }, Title = title };
        }

        // ── AddAppCommand ────────────────────────────────────────────────────

        [TestMethod]
        public void AddAppCommand_AddsNewAppToAppsCollection()
        {
            using (var scope = new TestStorageScope())
            {
                var vm = Create(scope);
                var initialCount = vm.Apps.Count;

                vm.AddAppCommand.Execute(null);

                Assert.AreEqual(initialCount + 1, vm.Apps.Count);
            }
        }

        [TestMethod]
        public void AddAppCommand_NewAppIsSelected()
        {
            using (var scope = new TestStorageScope())
            {
                var vm = Create(scope);

                vm.AddAppCommand.Execute(null);

                Assert.IsNotNull(vm.SelectedApp);
            }
        }

        [TestMethod]
        public void AddAppCommand_NewAppAddedToStorageApps()
        {
            using (var scope = new TestStorageScope())
            {
                var storage = scope.CreateStorage();
                var vm = new EditorViewModel(storage, new ActiveWindowService());
                var initialCount = storage.Apps.Count;

                vm.AddAppCommand.Execute(null);

                Assert.AreEqual(initialCount + 1, storage.Apps.Count);
            }
        }

        [TestMethod]
        public void AddAppCommand_NewAppHasDefaultProcessName()
        {
            using (var scope = new TestStorageScope())
            {
                var vm = Create(scope);

                vm.AddAppCommand.Execute(null);

                Assert.IsTrue(vm.SelectedApp.ProcessNames.Count > 0);
                Assert.IsFalse(string.IsNullOrWhiteSpace(vm.SelectedApp.ProcessNames[0]));
            }
        }

        // ── RemoveAppCommand ─────────────────────────────────────────────────

        [TestMethod]
        public void RemoveAppCommand_CanExecute_FalseWhenNoSelection()
        {
            using (var scope = new TestStorageScope())
            {
                var vm = Create(scope);
                vm.SelectedApp = null;

                Assert.IsFalse(vm.RemoveAppCommand.CanExecute(null));
            }
        }

        [TestMethod]
        public void RemoveAppCommand_CanExecute_TrueWhenAppSelected()
        {
            using (var scope = new TestStorageScope())
            {
                var vm = Create(scope);
                var app = MakeApp("notepad");
                vm.Apps.Add(app);
                vm.SelectedApp = app;

                Assert.IsTrue(vm.RemoveAppCommand.CanExecute(null));
            }
        }

        [TestMethod]
        public void RemoveAppCommand_RemovesSelectedAppFromApps()
        {
            using (var scope = new TestStorageScope())
            {
                var vm = Create(scope);
                var app = MakeApp("notepad");
                vm.Apps.Add(app);
                vm.SelectedApp = app;

                vm.RemoveAppCommand.Execute(null);

                CollectionAssert.DoesNotContain(vm.Apps, app);
            }
        }

        [TestMethod]
        public void RemoveAppCommand_RemovesAppFromStorageApps()
        {
            using (var scope = new TestStorageScope())
            {
                var storage = scope.CreateStorage();
                var vm = new EditorViewModel(storage, new ActiveWindowService());
                var app = MakeApp("notepad");
                vm.Apps.Add(app);
                storage.Apps.Add(app);
                vm.SelectedApp = app;

                vm.RemoveAppCommand.Execute(null);

                CollectionAssert.DoesNotContain(storage.Apps, app);
            }
        }

        [TestMethod]
        public void RemoveAppCommand_ClearsSelectedApp()
        {
            using (var scope = new TestStorageScope())
            {
                var vm = Create(scope);
                var app = MakeApp("notepad");
                vm.Apps.Add(app);
                vm.SelectedApp = app;

                vm.RemoveAppCommand.Execute(null);

                Assert.IsNull(vm.SelectedApp);
            }
        }

        // ── AddHotkeyCommand ─────────────────────────────────────────────────

        [TestMethod]
        public void AddHotkeyCommand_CanExecute_FalseWhenNoSelection()
        {
            using (var scope = new TestStorageScope())
            {
                var vm = Create(scope);
                vm.SelectedApp = null;

                Assert.IsFalse(vm.AddHotkeyCommand.CanExecute(null));
            }
        }

        [TestMethod]
        public void AddHotkeyCommand_CanExecute_TrueWhenAppSelected()
        {
            using (var scope = new TestStorageScope())
            {
                var vm = Create(scope);
                var app = MakeApp("notepad");
                vm.Apps.Add(app);
                vm.SelectedApp = app;

                Assert.IsTrue(vm.AddHotkeyCommand.CanExecute(null));
            }
        }

        [TestMethod]
        public void AddHotkeyCommand_AddsHotkeyToSelectedApp()
        {
            using (var scope = new TestStorageScope())
            {
                var vm = Create(scope);
                var app = MakeApp("notepad");
                vm.Apps.Add(app);
                vm.SelectedApp = app;

                vm.AddHotkeyCommand.Execute(null);

                Assert.AreEqual(1, app.Hotkeys.Count);
            }
        }

        [TestMethod]
        public void AddHotkeyCommand_SelectsNewHotkey()
        {
            using (var scope = new TestStorageScope())
            {
                var vm = Create(scope);
                var app = MakeApp("notepad");
                vm.Apps.Add(app);
                vm.SelectedApp = app;

                vm.AddHotkeyCommand.Execute(null);

                Assert.IsNotNull(vm.SelectedHotkey);
                Assert.AreSame(app.Hotkeys[0], vm.SelectedHotkey);
            }
        }

        [TestMethod]
        public void AddHotkeyCommand_NewHotkeyHasDefaultValues()
        {
            using (var scope = new TestStorageScope())
            {
                var vm = Create(scope);
                var app = MakeApp("notepad");
                vm.Apps.Add(app);
                vm.SelectedApp = app;

                vm.AddHotkeyCommand.Execute(null);

                var hotkey = app.Hotkeys[0];
                Assert.IsFalse(string.IsNullOrWhiteSpace(hotkey.Keys));
                Assert.IsFalse(string.IsNullOrWhiteSpace(hotkey.Description));
            }
        }

        // ── RemoveHotkeyCommand ──────────────────────────────────────────────

        [TestMethod]
        public void RemoveHotkeyCommand_CanExecute_FalseWhenNoApp()
        {
            using (var scope = new TestStorageScope())
            {
                var vm = Create(scope);
                vm.SelectedApp = null;

                Assert.IsFalse(vm.RemoveHotkeyCommand.CanExecute(null));
            }
        }

        [TestMethod]
        public void RemoveHotkeyCommand_CanExecute_FalseWhenNoHotkey()
        {
            using (var scope = new TestStorageScope())
            {
                var vm = Create(scope);
                var app = MakeApp("notepad");
                vm.Apps.Add(app);
                vm.SelectedApp = app;
                vm.SelectedHotkey = null;

                Assert.IsFalse(vm.RemoveHotkeyCommand.CanExecute(null));
            }
        }

        [TestMethod]
        public void RemoveHotkeyCommand_CanExecute_TrueWhenBothSelected()
        {
            using (var scope = new TestStorageScope())
            {
                var vm = Create(scope);
                var app = MakeApp("notepad");
                app.Hotkeys.Add(new HotkeyEntry { Keys = "Ctrl+S", Description = "Save" });
                vm.Apps.Add(app);
                vm.SelectedApp = app;
                vm.SelectedHotkey = app.Hotkeys[0];

                Assert.IsTrue(vm.RemoveHotkeyCommand.CanExecute(null));
            }
        }

        [TestMethod]
        public void RemoveHotkeyCommand_RemovesHotkeyFromApp()
        {
            using (var scope = new TestStorageScope())
            {
                var vm = Create(scope);
                var app = MakeApp("notepad");
                var hotkey = new HotkeyEntry { Keys = "Ctrl+S", Description = "Save" };
                app.Hotkeys.Add(hotkey);
                vm.Apps.Add(app);
                vm.SelectedApp = app;
                vm.SelectedHotkey = hotkey;

                vm.RemoveHotkeyCommand.Execute(null);

                CollectionAssert.DoesNotContain(app.Hotkeys, hotkey);
            }
        }

        [TestMethod]
        public void RemoveHotkeyCommand_ClearsSelectedHotkey()
        {
            using (var scope = new TestStorageScope())
            {
                var vm = Create(scope);
                var app = MakeApp("notepad");
                var hotkey = new HotkeyEntry { Keys = "Ctrl+S", Description = "Save" };
                app.Hotkeys.Add(hotkey);
                vm.Apps.Add(app);
                vm.SelectedApp = app;
                vm.SelectedHotkey = hotkey;

                vm.RemoveHotkeyCommand.Execute(null);

                Assert.IsNull(vm.SelectedHotkey);
            }
        }

        // ── DetectAppCommand ─────────────────────────────────────────────────

        [TestMethod]
        public void DetectAppCommand_CanExecute_TrueWhenNotDetecting()
        {
            using (var scope = new TestStorageScope())
            {
                var vm = Create(scope);

                Assert.IsTrue(vm.DetectAppCommand.CanExecute(null));
            }
        }

        [TestMethod]
        public void DetectAppCommand_Execute_SetsIsDetecting()
        {
            using (var scope = new TestStorageScope())
            {
                var vm = Create(scope);

                vm.DetectAppCommand.Execute(null);

                Assert.IsTrue(vm.IsDetecting);
            }
        }

        [TestMethod]
        public void DetectAppCommand_Execute_SetsCountdownButtonText()
        {
            using (var scope = new TestStorageScope())
            {
                var vm = Create(scope);

                vm.DetectAppCommand.Execute(null);

                StringAssert.Contains(vm.DetectButtonText, "3");
            }
        }

        [TestMethod]
        public void DetectTimerTick_IntermediateTick_DecrementsCountdownText()
        {
            using (var scope = new TestStorageScope())
            {
                var vm = Create(scope);
                vm.DetectAppCommand.Execute(null); // starts timer, countdown=3

                // Simulate one tick: 3→2
                SetPrivateField(vm, "_detectCountdown", 2);
                InvokePrivateMethod(vm, "DetectTimerTick", null, EventArgs.Empty);

                StringAssert.Contains(vm.DetectButtonText, "1");
                Assert.IsTrue(vm.IsDetecting);
            }
        }

        [TestMethod]
        public void DetectTimerTick_FinalTick_WhenProcessAlreadyExists_SelectsExistingApp()
        {
            using (var scope = new TestStorageScope())
            {
                var stub = new StubActiveWindowService { ProcessNameToReturn = "notepad" };
                var vm = Create(scope, stub);

                var existingApp = new AppHotkeys
                {
                    ProcessNames = new List<string> { "notepad" },
                    Title = "Existing"
                };
                vm.Apps.Add(existingApp);

                vm.DetectAppCommand.Execute(null);
                SetPrivateField(vm, "_detectCountdown", 1);
                InvokePrivateMethod(vm, "DetectTimerTick", null, EventArgs.Empty);

                Assert.AreSame(existingApp, vm.SelectedApp);
                Assert.IsFalse(vm.IsDetecting);
                Assert.AreEqual("Detect", vm.DetectButtonText);
            }
        }

        [TestMethod]
        public void DetectTimerTick_FinalTick_WhenProcessIsNew_CreatesNewApp()
        {
            using (var scope = new TestStorageScope())
            {
                var stub = new StubActiveWindowService { ProcessNameToReturn = "freshapp" };
                var storage = scope.CreateStorage();
                var vm = new EditorViewModel(storage, stub);
                vm.Apps.Clear();

                vm.DetectAppCommand.Execute(null);
                SetPrivateField(vm, "_detectCountdown", 1);
                InvokePrivateMethod(vm, "DetectTimerTick", null, EventArgs.Empty);

                Assert.AreEqual(1, vm.Apps.Count);
                Assert.IsNotNull(vm.SelectedApp);
                Assert.IsFalse(vm.IsDetecting);
                Assert.AreEqual("Detect", vm.DetectButtonText);
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static void SetPrivateField(object obj, string name, object value)
        {
            var field = obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Field '{name}' not found.");
            field.SetValue(obj, value);
        }

        private static void InvokePrivateMethod(object obj, string name, params object[] args)
        {
            var method = obj.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Method '{name}' not found.");
            method.Invoke(obj, args);
        }
    }
}
