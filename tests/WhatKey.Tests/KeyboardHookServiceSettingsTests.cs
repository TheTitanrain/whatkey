using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WhatKey.Models;
using WhatKey.Services;

namespace WhatKey.Tests
{
    [TestClass]
    public class KeyboardHookServiceSettingsTests
    {
        [TestMethod]
        public void UpdateSettings_AppliesHoldDelayHoldKeyAndToggleHotkey()
        {
            var initial = new AppSettings
            {
                HoldDelayMs = 500,
                HoldKey = "LControlKey",
                ToggleHotkey = "Ctrl+Alt+H"
            };
            var service = new KeyboardHookService(initial);

            service.UpdateSettings(new AppSettings
            {
                HoldDelayMs = 1200,
                HoldKey = "RShiftKey",
                ToggleHotkey = "Ctrl+Shift+T"
            });

            Assert.AreEqual(1200, initial.HoldDelayMs);
            Assert.AreEqual("RShiftKey", initial.HoldKey);
            Assert.AreEqual("Ctrl+Shift+T", initial.ToggleHotkey);
            Assert.AreEqual(1200d, GetHoldTimerInterval(service).TotalMilliseconds, 0.001d);
            Assert.AreEqual((uint)0xA1, GetHoldVkCode(service));

            service.Dispose();
        }

        [TestMethod]
        public void UpdateSettings_WithNegativeDelay_ClampsDelayToZero()
        {
            var settings = new AppSettings();
            var service = new KeyboardHookService(settings);

            service.UpdateSettings(new AppSettings
            {
                HoldDelayMs = -50,
                HoldKey = "LControlKey",
                ToggleHotkey = "Ctrl+Alt+H"
            });

            Assert.AreEqual(0, settings.HoldDelayMs);
            Assert.AreEqual(0d, GetHoldTimerInterval(service).TotalMilliseconds, 0.001d);

            service.Dispose();
        }

        [TestMethod]
        public void UpdateSettings_WithWindowHandle_UnregistersThenRegistersToggleHotkey()
        {
            var settings = new AppSettings
            {
                HoldDelayMs = 500,
                HoldKey = "LControlKey",
                ToggleHotkey = "Ctrl+Alt+H"
            };
            var service = new KeyboardHookService(settings);

            var calls = new List<string>();
            SetRegisterDelegate(service, (hWnd, id, modifiers, vk) =>
            {
                calls.Add("register");
                Assert.AreEqual(9001, id);
                Assert.AreEqual(0x4000 | 0x0002 | 0x0004, modifiers);
                Assert.AreEqual((int)'T', vk);
                return true;
            });
            SetUnregisterDelegate(service, (hWnd, id) =>
            {
                calls.Add("unregister");
                Assert.AreEqual(9001, id);
                return true;
            });

            SetPrivateField(service, "_hotkeyWindowHandle", new IntPtr(123456));
            SetLastErrorDelegate(service, () => 0);

            service.UpdateSettings(new AppSettings
            {
                HoldDelayMs = 600,
                HoldKey = "LControlKey",
                ToggleHotkey = "Ctrl+Shift+T"
            });

            CollectionAssert.AreEqual(new[] { "unregister", "register" }, calls);

            service.Dispose();
        }

        [TestMethod]
        public void UpdateSettings_WhenRegisterFails_RollsBackToPreviousSettings()
        {
            var settings = new AppSettings
            {
                HoldDelayMs = 500,
                HoldKey = "LControlKey",
                ToggleHotkey = "Ctrl+Alt+H"
            };
            var service = new KeyboardHookService(settings);

            SetPrivateField(service, "_hotkeyWindowHandle", new IntPtr(42));
            SetUnregisterDelegate(service, (hWnd, id) => true);
            SetRegisterDelegate(service, (hWnd, id, modifiers, vk) => vk == (int)'H');
            SetLastErrorDelegate(service, () => 123);

            Assert.ThrowsException<InvalidOperationException>(() =>
                service.UpdateSettings(new AppSettings
                {
                    HoldDelayMs = 1500,
                    HoldKey = "RShiftKey",
                    ToggleHotkey = "Ctrl+Shift+T"
                }));

            Assert.AreEqual(500, settings.HoldDelayMs);
            Assert.AreEqual("LControlKey", settings.HoldKey);
            Assert.AreEqual("Ctrl+Alt+H", settings.ToggleHotkey);
            Assert.AreEqual(500d, GetHoldTimerInterval(service).TotalMilliseconds, 0.001d);
            Assert.AreEqual((uint)0xA2, GetHoldVkCode(service));

            service.Dispose();
        }

        [TestMethod]
        public void UpdateSettings_WhenSharedSettingsInstanceWasMutated_RollsBackFromAppliedSnapshot()
        {
            var sharedSettings = new AppSettings
            {
                HoldDelayMs = 500,
                HoldKey = "LControlKey",
                ToggleHotkey = "Ctrl+Alt+H"
            };
            var service = new KeyboardHookService(sharedSettings);

            // Simulate editor mutating the same shared settings instance before save callback.
            sharedSettings.HoldDelayMs = 1700;
            sharedSettings.HoldKey = "RShiftKey";
            sharedSettings.ToggleHotkey = "Ctrl+Shift+T";

            SetPrivateField(service, "_hotkeyWindowHandle", new IntPtr(777));
            SetUnregisterDelegate(service, (hWnd, id) => true);
            SetRegisterDelegate(service, (hWnd, id, modifiers, vk) => vk == (int)'H');
            SetLastErrorDelegate(service, () => 123);

            Assert.ThrowsException<InvalidOperationException>(() => service.UpdateSettings(sharedSettings));

            Assert.AreEqual(500, sharedSettings.HoldDelayMs);
            Assert.AreEqual("LControlKey", sharedSettings.HoldKey);
            Assert.AreEqual("Ctrl+Alt+H", sharedSettings.ToggleHotkey);
            Assert.AreEqual((uint)0xA2, GetHoldVkCode(service));

            service.Dispose();
        }

        [TestMethod]
        public void UpdateSettings_ResetsHoldStateAndHidesOverlay()
        {
            var settings = new AppSettings();
            var service = new KeyboardHookService(settings);

            SetPrivateField(service, "_isHoldKeyDown", true);
            SetPrivateField(service, "_isOverlayVisible", true);
            var hideCalls = 0;
            service.TriggerHide += (sender, args) => hideCalls++;
            SetLastErrorDelegate(service, () => 0);

            service.UpdateSettings(new AppSettings
            {
                HoldDelayMs = 900,
                HoldKey = "RControlKey",
                ToggleHotkey = "Ctrl+Alt+H"
            });

            Assert.IsFalse((bool)GetPrivateField(service, "_isHoldKeyDown"));
            Assert.IsFalse((bool)GetPrivateField(service, "_isOverlayVisible"));
            Assert.AreEqual(1, hideCalls);

            service.Dispose();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void UpdateSettings_WithNullSettings_Throws()
        {
            var service = new KeyboardHookService(new AppSettings());
            try
            {
                service.UpdateSettings(null);
            }
            finally
            {
                service.Dispose();
            }
        }

        [TestMethod]
        public void UpdateSettings_WhenUnregisterFails_ThrowsAndKeepsPreviousSettings()
        {
            var settings = new AppSettings
            {
                HoldDelayMs = 500,
                HoldKey = "LControlKey",
                ToggleHotkey = "Ctrl+Alt+H"
            };
            var service = new KeyboardHookService(settings);

            SetPrivateField(service, "_hotkeyWindowHandle", new IntPtr(111));
            SetUnregisterDelegate(service, (hWnd, id) => false);
            SetLastErrorDelegate(service, () => 5);

            Assert.ThrowsException<InvalidOperationException>(() =>
                service.UpdateSettings(new AppSettings
                {
                    HoldDelayMs = 900,
                    HoldKey = "RShiftKey",
                    ToggleHotkey = "Ctrl+Shift+T"
                }));

            Assert.AreEqual(500, settings.HoldDelayMs);
            Assert.AreEqual("LControlKey", settings.HoldKey);
            Assert.AreEqual("Ctrl+Alt+H", settings.ToggleHotkey);
            Assert.AreEqual((uint)0xA2, GetHoldVkCode(service));
            Assert.AreEqual(500d, GetHoldTimerInterval(service).TotalMilliseconds, 0.001d);

            service.Dispose();
        }

        [TestMethod]
        public void UpdateSettings_WhenRollbackRegisterFails_ThrowsHotkeyRecoveryException()
        {
            var settings = new AppSettings
            {
                HoldDelayMs = 500,
                HoldKey = "LControlKey",
                ToggleHotkey = "Ctrl+Alt+H"
            };
            var service = new KeyboardHookService(settings);

            SetPrivateField(service, "_hotkeyWindowHandle", new IntPtr(222));
            SetUnregisterDelegate(service, (hWnd, id) => true);
            SetRegisterDelegate(service, (hWnd, id, modifiers, vk) => false);
            SetLastErrorDelegate(service, () => 5);

            Assert.ThrowsException<HotkeyRecoveryException>(() =>
                service.UpdateSettings(new AppSettings
                {
                    HoldDelayMs = 800,
                    HoldKey = "RShiftKey",
                    ToggleHotkey = "Ctrl+Shift+T"
                }));

            Assert.AreEqual(500, settings.HoldDelayMs);
            Assert.AreEqual("LControlKey", settings.HoldKey);
            Assert.AreEqual("Ctrl+Alt+H", settings.ToggleHotkey);
            Assert.AreEqual((uint)0xA2, GetHoldVkCode(service));

            service.Dispose();
        }

        [TestMethod]
        public void UpdateSettings_WithInvalidToggleHotkey_ThrowsAndRollsBack()
        {
            var settings = new AppSettings
            {
                HoldDelayMs = 500,
                HoldKey = "LControlKey",
                ToggleHotkey = "Ctrl+Alt+H"
            };
            var service = new KeyboardHookService(settings);

            SetPrivateField(service, "_hotkeyWindowHandle", new IntPtr(333));
            SetUnregisterDelegate(service, (hWnd, id) => true);
            SetRegisterDelegate(service, (hWnd, id, modifiers, vk) => true);
            SetLastErrorDelegate(service, () => 0);

            Assert.ThrowsException<InvalidOperationException>(() =>
                service.UpdateSettings(new AppSettings
                {
                    HoldDelayMs = 800,
                    HoldKey = "RShiftKey",
                    ToggleHotkey = "Ctrl+"
                }));

            Assert.AreEqual(500, settings.HoldDelayMs);
            Assert.AreEqual("LControlKey", settings.HoldKey);
            Assert.AreEqual("Ctrl+Alt+H", settings.ToggleHotkey);
            Assert.AreEqual((uint)0xA2, GetHoldVkCode(service));

            service.Dispose();
        }

        [TestMethod]
        public void Install_WhenHookInstallationFails_Throws()
        {
            var settings = new AppSettings();
            var service = new KeyboardHookService(settings);
            try
            {
                SetPrivateField(service, "_installKeyboardHook", new Func<IntPtr>(() => IntPtr.Zero));
                SetLastErrorDelegate(service, () => 5);

                Assert.ThrowsException<InvalidOperationException>(() => service.Install());
            }
            finally
            {
                service.Dispose();
            }
        }

        [TestMethod]
        public void Constructor_WithInvalidPersistedToggleHotkey_DisablesToggleHotkey()
        {
            var settings = new AppSettings
            {
                HoldDelayMs = 500,
                HoldKey = "LControlKey",
                ToggleHotkey = "Ctrl+F1"
            };

            using (var service = new KeyboardHookService(settings))
            {
                var applied = service.GetAppliedSettingsSnapshot();
                Assert.AreEqual(string.Empty, applied.ToggleHotkey);
                Assert.AreEqual(string.Empty, settings.ToggleHotkey);
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

        private static void SetRegisterDelegate(
            KeyboardHookService service,
            Func<IntPtr, int, int, int, bool> callback)
        {
            SetPrivateField(service, "_registerHotKey", callback);
        }

        private static void SetUnregisterDelegate(
            KeyboardHookService service,
            Func<IntPtr, int, bool> callback)
        {
            SetPrivateField(service, "_unregisterHotKey", callback);
        }

        private static void SetLastErrorDelegate(
            KeyboardHookService service,
            Func<int> callback)
        {
            SetPrivateField(service, "_getLastWin32Error", callback);
        }

        private static object GetPrivateField(object instance, string fieldName)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Field '{fieldName}' was not found.");
            return field.GetValue(instance);
        }

        private static void SetPrivateField(object instance, string fieldName, object value)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Field '{fieldName}' was not found.");
            field.SetValue(instance, value);
        }
    }
}
