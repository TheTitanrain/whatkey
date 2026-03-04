using System;
using Microsoft.Win32;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WhatKey.Services;

namespace WhatKey.Tests
{
    [TestClass]
    public class AutostartServiceTests
    {
        private const string TestRegKey = @"Software\WhatKey.Tests\Autostart";
        private const string TestAppName = "WhatKeyTest";

        private static readonly string OriginalRegKey = AutostartService.RegKey;
        private static readonly string OriginalAppName = AutostartService.AppName;

        [TestInitialize]
        public void Setup()
        {
            AutostartService.RegKey = TestRegKey;
            AutostartService.AppName = TestAppName;
            // Ensure the test registry key exists so Enable() can write to it
            Registry.CurrentUser.CreateSubKey(TestRegKey)?.Dispose();
        }

        [TestCleanup]
        public void Cleanup()
        {
            Registry.CurrentUser.DeleteSubKeyTree(@"Software\WhatKey.Tests", throwOnMissingSubKey: false);
            AutostartService.RegKey = OriginalRegKey;
            AutostartService.AppName = OriginalAppName;
        }

        [TestMethod]
        public void IsEnabled_WhenNotRegistered_ReturnsFalse()
        {
            Assert.IsFalse(AutostartService.IsEnabled());
        }

        [TestMethod]
        public void Enable_WritesRegistryValue()
        {
            AutostartService.Enable();

            using (var key = Registry.CurrentUser.OpenSubKey(TestRegKey))
            {
                Assert.IsNotNull(key?.GetValue(TestAppName));
            }
        }

        [TestMethod]
        public void IsEnabled_AfterEnable_ReturnsTrue()
        {
            AutostartService.Enable();

            Assert.IsTrue(AutostartService.IsEnabled());
        }

        [TestMethod]
        public void Disable_RemovesRegistryValue()
        {
            AutostartService.Enable();
            AutostartService.Disable();

            using (var key = Registry.CurrentUser.OpenSubKey(TestRegKey))
            {
                Assert.IsNull(key?.GetValue(TestAppName));
            }
        }

        [TestMethod]
        public void IsEnabled_AfterDisable_ReturnsFalse()
        {
            AutostartService.Enable();
            AutostartService.Disable();

            Assert.IsFalse(AutostartService.IsEnabled());
        }

        [TestMethod]
        public void Disable_WhenNotEnabled_DoesNotThrow()
        {
            AutostartService.Disable();
        }
    }
}
