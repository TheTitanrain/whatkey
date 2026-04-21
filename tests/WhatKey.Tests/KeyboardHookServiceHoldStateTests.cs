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
        public void ForceResetHoldState_StopsHoldTimer()
        {
            var settings = new AppSettings { HoldDelayMs = 500, HoldKey = "LControlKey" };
            var service = new KeyboardHookService(settings);

            var timer = GetPrivateField(service, "_holdTimer");
            var startMethod = timer.GetType().GetMethod("Start", BindingFlags.Instance | BindingFlags.Public);
            startMethod.Invoke(timer, null);
            SetPrivateField(service, "_isHoldKeyDown", true);

            service.ForceResetHoldState();

            var isEnabled = (bool)timer.GetType().GetProperty("IsEnabled", BindingFlags.Instance | BindingFlags.Public).GetValue(timer);
            Assert.IsFalse(isEnabled);
            Assert.IsFalse((bool)GetPrivateField(service, "_isHoldKeyDown"));

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

        [TestMethod]
        public void MouseHookCallback_OnButtonDown_ResetsHoldStateAndFiresTriggerHide()
        {
            var settings = new AppSettings { HoldDelayMs = 500, HoldKey = "LControlKey" };
            var service = new KeyboardHookService(settings);

            SetPrivateField(service, "_isHoldKeyDown", true);
            SetPrivateField(service, "_isOverlayVisible", true);

            var hideCalls = 0;
            var showCalls = 0;
            service.TriggerHide += (_, __) => hideCalls++;
            service.TriggerShow += (_, __) => showCalls++;

            var mouseHookProc = (Delegate)GetPrivateField(service, "_mouseHookProc");
            const int WM_LBUTTONDOWN = 0x0201;
            mouseHookProc.DynamicInvoke(0, (IntPtr)WM_LBUTTONDOWN, IntPtr.Zero);

            Assert.AreEqual(1, hideCalls);
            Assert.AreEqual(0, showCalls);
            Assert.IsFalse((bool)GetPrivateField(service, "_isHoldKeyDown"));
            Assert.IsFalse((bool)GetPrivateField(service, "_isOverlayVisible"));

            service.Dispose();
        }

        [TestMethod]
        public void MouseHookCallback_OnButtonDown_WhenOverlayNotVisible_DoesNotFireTriggerHide()
        {
            var settings = new AppSettings { HoldDelayMs = 500, HoldKey = "LControlKey" };
            var service = new KeyboardHookService(settings);

            SetPrivateField(service, "_isHoldKeyDown", true);
            SetPrivateField(service, "_isOverlayVisible", false);

            var hideCalls = 0;
            service.TriggerHide += (_, __) => hideCalls++;

            var mouseHookProc = (Delegate)GetPrivateField(service, "_mouseHookProc");
            const int WM_RBUTTONDOWN = 0x0204;
            mouseHookProc.DynamicInvoke(0, (IntPtr)WM_RBUTTONDOWN, IntPtr.Zero);

            Assert.AreEqual(0, hideCalls);
            Assert.IsFalse((bool)GetPrivateField(service, "_isHoldKeyDown"));

            service.Dispose();
        }

        [TestMethod]
        public void MouseHookCallback_OnMiddleButtonDown_ResetsHoldState()
        {
            var settings = new AppSettings { HoldDelayMs = 500, HoldKey = "LControlKey" };
            var service = new KeyboardHookService(settings);

            SetPrivateField(service, "_isHoldKeyDown", true);
            SetPrivateField(service, "_isOverlayVisible", true);

            var hideCalls = 0;
            service.TriggerHide += (_, __) => hideCalls++;

            var mouseHookProc = (Delegate)GetPrivateField(service, "_mouseHookProc");
            const int WM_MBUTTONDOWN = 0x0207;
            mouseHookProc.DynamicInvoke(0, (IntPtr)WM_MBUTTONDOWN, IntPtr.Zero);

            Assert.AreEqual(1, hideCalls);
            Assert.IsFalse((bool)GetPrivateField(service, "_isHoldKeyDown"));
            Assert.IsFalse((bool)GetPrivateField(service, "_isOverlayVisible"));

            service.Dispose();
        }

        [TestMethod]
        public void MouseHookCallback_OnXButtonDown_ResetsHoldState()
        {
            var settings = new AppSettings { HoldDelayMs = 500, HoldKey = "LControlKey" };
            var service = new KeyboardHookService(settings);

            SetPrivateField(service, "_isHoldKeyDown", true);
            SetPrivateField(service, "_isOverlayVisible", true);

            var hideCalls = 0;
            service.TriggerHide += (_, __) => hideCalls++;

            var mouseHookProc = (Delegate)GetPrivateField(service, "_mouseHookProc");
            const int WM_XBUTTONDOWN = 0x020B;
            mouseHookProc.DynamicInvoke(0, (IntPtr)WM_XBUTTONDOWN, IntPtr.Zero);

            Assert.AreEqual(1, hideCalls);
            Assert.IsFalse((bool)GetPrivateField(service, "_isHoldKeyDown"));
            Assert.IsFalse((bool)GetPrivateField(service, "_isOverlayVisible"));

            service.Dispose();
        }

        [TestMethod]
        public void MouseHookCallback_WhenNCodeNegative_DoesNotResetState()
        {
            var settings = new AppSettings { HoldDelayMs = 500, HoldKey = "LControlKey" };
            var service = new KeyboardHookService(settings);

            SetPrivateField(service, "_isHoldKeyDown", true);
            SetPrivateField(service, "_isOverlayVisible", true);

            var hideCalls = 0;
            service.TriggerHide += (_, __) => hideCalls++;

            var mouseHookProc = (Delegate)GetPrivateField(service, "_mouseHookProc");
            const int WM_LBUTTONDOWN = 0x0201;
            mouseHookProc.DynamicInvoke(-1, (IntPtr)WM_LBUTTONDOWN, IntPtr.Zero);

            Assert.AreEqual(0, hideCalls);
            Assert.IsTrue((bool)GetPrivateField(service, "_isHoldKeyDown"));
            Assert.IsTrue((bool)GetPrivateField(service, "_isOverlayVisible"));

            service.Dispose();
        }

        [TestMethod]
        public void MouseHookCallback_OnButtonDown_WhenTogglePinnedAndNoHoldKey_DoesNotDismissOverlay()
        {
            // Toggle-mode overlay: visible but hold key is not down and timer is not running.
            // Mouse click must not dismiss it — only the toggle hotkey should.
            var settings = new AppSettings { HoldDelayMs = 500, HoldKey = "LControlKey" };
            var service = new KeyboardHookService(settings);

            SetPrivateField(service, "_isHoldKeyDown", false);
            SetPrivateField(service, "_isOverlayVisible", true);

            var hideCalls = 0;
            service.TriggerHide += (_, __) => hideCalls++;

            var mouseHookProc = (Delegate)GetPrivateField(service, "_mouseHookProc");
            const int WM_LBUTTONDOWN = 0x0201;
            mouseHookProc.DynamicInvoke(0, (IntPtr)WM_LBUTTONDOWN, IntPtr.Zero);

            Assert.AreEqual(0, hideCalls);
            Assert.IsTrue((bool)GetPrivateField(service, "_isOverlayVisible"));

            service.Dispose();
        }

        [TestMethod]
        public void MouseHookCallback_OnNonButtonMessage_DoesNotResetState()
        {
            var settings = new AppSettings { HoldDelayMs = 500, HoldKey = "LControlKey" };
            var service = new KeyboardHookService(settings);

            SetPrivateField(service, "_isHoldKeyDown", true);
            SetPrivateField(service, "_isOverlayVisible", true);

            var hideCalls = 0;
            service.TriggerHide += (_, __) => hideCalls++;

            var mouseHookProc = (Delegate)GetPrivateField(service, "_mouseHookProc");
            const int WM_MOUSEMOVE = 0x0200;
            mouseHookProc.DynamicInvoke(0, (IntPtr)WM_MOUSEMOVE, IntPtr.Zero);

            Assert.AreEqual(0, hideCalls);
            Assert.IsTrue((bool)GetPrivateField(service, "_isHoldKeyDown"));
            Assert.IsTrue((bool)GetPrivateField(service, "_isOverlayVisible"));

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
