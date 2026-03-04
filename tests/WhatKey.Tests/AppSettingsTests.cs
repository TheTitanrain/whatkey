using System.ComponentModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WhatKey.Models;

namespace WhatKey.Tests
{
    [TestClass]
    public class AppSettingsTests
    {
        [TestMethod]
        public void HoldKey_Change_RaisesPropertyChanged()
        {
            var settings = new AppSettings();
            string raised = null;
            settings.PropertyChanged += (s, e) => raised = e.PropertyName;

            settings.HoldKey = "RShiftKey";

            Assert.AreEqual(nameof(AppSettings.HoldKey), raised);
        }

        [TestMethod]
        public void HoldDelayMs_Change_RaisesPropertyChanged()
        {
            var settings = new AppSettings();
            string raised = null;
            settings.PropertyChanged += (s, e) => raised = e.PropertyName;

            settings.HoldDelayMs = 1001;

            Assert.AreEqual(nameof(AppSettings.HoldDelayMs), raised);
        }

        [TestMethod]
        public void ToggleHotkey_Change_RaisesPropertyChanged()
        {
            var settings = new AppSettings();
            string raised = null;
            settings.PropertyChanged += (s, e) => raised = e.PropertyName;

            settings.ToggleHotkey = "Ctrl+Shift+T";

            Assert.AreEqual(nameof(AppSettings.ToggleHotkey), raised);
        }

        [TestMethod]
        public void NoChange_DoesNotRaisePropertyChanged()
        {
            var settings = new AppSettings();
            var eventCount = 0;
            settings.PropertyChanged += (s, e) => eventCount++;

            settings.HoldKey = "LControlKey";       // same as default
            settings.HoldDelayMs = 1000;             // same as default
            settings.ToggleHotkey = "Ctrl+Alt+H";  // same as default

            Assert.AreEqual(0, eventCount);
        }

        [TestMethod]
        public void DefaultValues_AreCorrect()
        {
            var settings = new AppSettings();

            Assert.AreEqual("LControlKey", settings.HoldKey);
            Assert.AreEqual(1000, settings.HoldDelayMs);
            Assert.AreEqual("Ctrl+Alt+H", settings.ToggleHotkey);
        }
    }
}
