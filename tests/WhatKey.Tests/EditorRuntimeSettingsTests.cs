using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WhatKey.Models;
using WhatKey.Services;
using WhatKey.ViewModels;

namespace WhatKey.Tests
{
    [TestClass]
    public class EditorRuntimeSettingsTests
    {
        [TestMethod]
        public void SaveCommand_RaisesSettingsSaved_WithCurrentSettings()
        {
            using (var scope = new TestStorageScope())
            {
                var storage = scope.CreateStorage();
                var viewModel = new EditorViewModel(storage, new ActiveWindowService());

                var raised = false;
                AppSettings payload = null;
                viewModel.SettingsSaved += (sender, settings) =>
                {
                    raised = true;
                    payload = settings;
                };

                viewModel.SaveCommand.Execute(null);

                Assert.IsTrue(raised);
                Assert.AreSame(storage.Settings, payload);
            }
        }

        [TestMethod]
        public void RuntimeSettingsCoordinator_Attach_InvokesCallbackAfterSave()
        {
            using (var scope = new TestStorageScope())
            {
                var storage = scope.CreateStorage();
                var viewModel = new EditorViewModel(storage, new ActiveWindowService());

                var calls = 0;
                AppSettings payload = null;

                RuntimeSettingsCoordinator.Attach(viewModel, settings =>
                {
                    calls++;
                    payload = settings;
                });

                viewModel.SaveCommand.Execute(null);

                Assert.AreEqual(1, calls);
                Assert.AreSame(storage.Settings, payload);
            }
        }

        [TestMethod]
        public void RuntimeSettingsCoordinator_Attach_WithNullEditorViewModel_Throws()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
                RuntimeSettingsCoordinator.Attach(null, _ => { }));
        }

        [TestMethod]
        public void RuntimeSettingsCoordinator_Attach_WithNullCallback_Throws()
        {
            using (var scope = new TestStorageScope())
            {
                var storage = scope.CreateStorage();
                var viewModel = new EditorViewModel(storage, new ActiveWindowService());
                Assert.ThrowsException<ArgumentNullException>(() =>
                    RuntimeSettingsCoordinator.Attach(viewModel, null));
            }
        }

        [TestMethod]
        public void SaveCommand_AppliesRuntimeSettingsWithoutRestart()
        {
            using (var scope = new TestStorageScope())
            {
                var storage = scope.CreateStorage();
                var viewModel = new EditorViewModel(storage, new ActiveWindowService());
                var service = new KeyboardHookService(storage.Settings);
                try
                {
                    RuntimeSettingsCoordinator.Attach(viewModel, settings => service.UpdateSettings(settings));

                    viewModel.Settings.HoldDelayMs = 1350;
                    viewModel.Settings.HoldKey = "RShiftKey";
                    viewModel.Settings.ToggleHotkey = "Ctrl+Shift+T";

                    Assert.AreEqual(1000d, GetHoldTimerInterval(service).TotalMilliseconds, 0.001d);
                    Assert.AreEqual((uint)0xA2, GetHoldVkCode(service));

                    viewModel.SaveCommand.Execute(null);

                    Assert.AreEqual(1350, storage.Settings.HoldDelayMs);
                    Assert.AreEqual("RShiftKey", storage.Settings.HoldKey);
                    Assert.AreEqual("Ctrl+Shift+T", storage.Settings.ToggleHotkey);
                    Assert.AreEqual(1350d, GetHoldTimerInterval(service).TotalMilliseconds, 0.001d);
                    Assert.AreEqual((uint)0xA1, GetHoldVkCode(service));
                }
                finally
                {
                    service.Dispose();
                }
            }
        }

        [TestMethod]
        public void SaveCommand_WithoutSubscribers_SavesSettings()
        {
            using (var scope = new TestStorageScope())
            {
                var storage = scope.CreateStorage();
                var viewModel = new EditorViewModel(storage, new ActiveWindowService());

                viewModel.Settings.HoldDelayMs = 777;
                viewModel.SaveCommand.Execute(null);

                Assert.AreEqual(777, storage.Settings.HoldDelayMs);
            }
        }

        [TestMethod]
        public void SaveCommand_WhenRuntimeApplyFails_DoesNotPersistSettingsBeforeApply()
        {
            using (var scope = new TestStorageScope())
            {
                var storage = scope.CreateStorage();
                var viewModel = new EditorViewModel(storage, new ActiveWindowService());

                viewModel.Settings.HoldDelayMs = 999;
                RuntimeSettingsCoordinator.Attach(viewModel, _ => throw new InvalidOperationException("apply failed"));

                Assert.ThrowsException<InvalidOperationException>(() => viewModel.SaveCommand.Execute(null));
                Assert.IsFalse(File.Exists(storage.DataFilePath));
            }
        }

        [TestMethod]
        public void SaveCommand_WhenRuntimeApplyFails_RollsBackStorageApps()
        {
            using (var scope = new TestStorageScope())
            {
                var storage = scope.CreateStorage();
                storage.Apps.Clear();
                storage.Apps.Add(new AppHotkeys
                {
                    ProcessNames = new System.Collections.Generic.List<string> { "existing" },
                    Title = "Existing App"
                });

                var viewModel = new EditorViewModel(storage, new ActiveWindowService());
                viewModel.Apps.Clear();
                viewModel.Apps.Add(new AppHotkeys
                {
                    ProcessNames = new System.Collections.Generic.List<string> { "newapp" },
                    Title = "New App"
                });

                RuntimeSettingsCoordinator.Attach(viewModel, _ => throw new InvalidOperationException("apply failed"));

                Assert.ThrowsException<InvalidOperationException>(() => viewModel.SaveCommand.Execute(null));
                Assert.AreEqual(1, storage.Apps.Count);
                Assert.IsTrue(storage.Apps[0].ProcessNames.Contains("existing"));
                Assert.AreEqual("Existing App", storage.Apps[0].Title);
            }
        }

        [TestMethod]
        public void SaveCommand_WithRuntimeApply_PersistsNormalizedSettings()
        {
            using (var scope = new TestStorageScope())
            {
                var storage = scope.CreateStorage();
                var viewModel = new EditorViewModel(storage, new ActiveWindowService());
                var service = new KeyboardHookService(storage.Settings);
                try
                {
                    RuntimeSettingsCoordinator.Attach(viewModel, settings =>
                    {
                        service.UpdateSettings(settings);
                        storage.Save();
                    });

                    viewModel.Settings.HoldDelayMs = -12;
                    viewModel.SaveCommand.Execute(null);

                    var json = File.ReadAllText(storage.DataFilePath);
                    var savedData = JsonConvert.DeserializeObject<HotkeysData>(json);
                    Assert.AreEqual(0, savedData.Settings.HoldDelayMs);
                }
                finally
                {
                    service.Dispose();
                }
            }
        }

        private static TimeSpan GetHoldTimerInterval(KeyboardHookService service)
        {
            var timer = GetPrivateField(service, "_holdTimer");
            var intervalProperty = timer.GetType().GetProperty("Interval", BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(intervalProperty, "DispatcherTimer.Interval property was not found.");
            return (TimeSpan)intervalProperty.GetValue(timer);
        }

        private static uint GetHoldVkCode(KeyboardHookService service)
        {
            return (uint)GetPrivateField(service, "_holdVkCode");
        }

        private static object GetPrivateField(object instance, string fieldName)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Field '{fieldName}' was not found.");
            return field.GetValue(instance);
        }

        private sealed class TestStorageScope : IDisposable
        {
            private readonly string _dataDir;

            public TestStorageScope()
            {
                _dataDir = Path.Combine(Path.GetTempPath(), "WhatKey.Tests", Guid.NewGuid().ToString("N"));
            }

            public HotkeysStorageService CreateStorage()
            {
                return new HotkeysStorageService(_dataDir);
            }

            public void Dispose()
            {
                if (Directory.Exists(_dataDir))
                    Directory.Delete(_dataDir, true);
            }
        }
    }
}
