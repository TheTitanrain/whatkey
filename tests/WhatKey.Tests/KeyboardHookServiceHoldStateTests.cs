using System;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WhatKey.Models;
using WhatKey.Services;

namespace WhatKey.Tests
{
    [TestClass]
    public class KeyboardHookServiceHoldStateTests
    {
        [TestMethod]
        public void ForceResetHoldState_WhenOverlayVisible_FiresTriggerHideAndClearsFlags()
        {
            var settings = new AppSettings { HoldDelayMs = 500, HoldKey = "LControlKey" };
            var service = new KeyboardHookService(settings);

            SetPrivateField(service, "_isHoldKeyDown", true);
            SetPrivateField(service, "_isOverlayVisible", true);

            var hideCalls = 0;
            var showCalls = 0;
            service.TriggerHide += (_, __) => hideCalls++;
            service.TriggerShow += (_, __) => showCalls++;

            service.ForceResetHoldState();

            Assert.AreEqual(1, hideCalls);
            Assert.AreEqual(0, showCalls);
            Assert.IsFalse((bool)GetPrivateField(service, "_isHoldKeyDown"));
            Assert.IsFalse((bool)GetPrivateField(service, "_isOverlayVisible"));

            service.Dispose();
        }

        [TestMethod]
        public void ForceResetHoldState_WhenOverlayNotVisible_DoesNotFireTriggerHide()
        {
            var settings = new AppSettings { HoldDelayMs = 500, HoldKey = "LControlKey" };
            var service = new KeyboardHookService(settings);

            SetPrivateField(service, "_isHoldKeyDown", true);
            SetPrivateField(service, "_isOverlayVisible", false);

            var hideCalls = 0;
            service.TriggerHide += (_, __) => hideCalls++;

            service.ForceResetHoldState();

            Assert.AreEqual(0, hideCalls);
            Assert.IsFalse((bool)GetPrivateField(service, "_isHoldKeyDown"));

            service.Dispose();
        }

        [TestMethod]
        public void ForceResetHoldState_StopsHoldTimer_PreventsTriggerShowFromFiring()
        {
            var settings = new AppSettings { HoldDelayMs = 500, HoldKey = "LControlKey" };
            var service = new KeyboardHookService(settings);

            var timer = GetPrivateField(service, "_holdTimer");
            var startMethod = timer.GetType().GetMethod("Start", BindingFlags.Instance | BindingFlags.Public);
            startMethod.Invoke(timer, null);
            SetPrivateField(service, "_isHoldKeyDown", true);

            var showCalls = 0;
            service.TriggerShow += (_, __) => showCalls++;

            service.ForceResetHoldState();

            var isEnabled = (bool)timer.GetType().GetProperty("IsEnabled", BindingFlags.Instance | BindingFlags.Public).GetValue(timer);
            Assert.IsFalse(isEnabled);
            Assert.AreEqual(0, showCalls);

            service.Dispose();
        }

        [TestMethod]
        public void ForceResetHoldState_WhenOverlayVisibleAndTimerRunning_HidesAndStopsTimer()
        {
            var settings = new AppSettings { HoldDelayMs = 500, HoldKey = "LControlKey" };
            var service = new KeyboardHookService(settings);

            var timer = GetPrivateField(service, "_holdTimer");
            var startMethod = timer.GetType().GetMethod("Start", BindingFlags.Instance | BindingFlags.Public);
            startMethod.Invoke(timer, null);
            SetPrivateField(service, "_isHoldKeyDown", true);
            SetPrivateField(service, "_isOverlayVisible", true);

            var hideCalls = 0;
            service.TriggerHide += (_, __) => hideCalls++;

            service.ForceResetHoldState();

            Assert.AreEqual(1, hideCalls);
            var isEnabled = (bool)timer.GetType().GetProperty("IsEnabled", BindingFlags.Instance | BindingFlags.Public).GetValue(timer);
            Assert.IsFalse(isEnabled);
            Assert.IsFalse((bool)GetPrivateField(service, "_isHoldKeyDown"));
            Assert.IsFalse((bool)GetPrivateField(service, "_isOverlayVisible"));

            service.Dispose();
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
