---
# Fix Hold Key State Reset: Mouse Clicks, Screen Lock, and Sleep

## Overview

Three categories of bugs where the hold key state (`_isHoldKeyDown`, `_holdTimer`, overlay
visibility) gets stuck because key-up events are never received or external events are not handled:

1. Mouse button press while holding the trigger key does not cancel the hold timer
2. Screen lock (Win+L, lid close, timeout) while holding Ctrl leaves overlay stuck after unlock
3. System sleep/wake has the same stuck-overlay problem

Additionally the app has no low-level mouse hook at all, and no session/power event subscriptions.

## Context

- Files involved:
  - `WhatKey/Services/KeyboardHookService.cs` (hold timer, keyboard hook, all state flags)
  - `WhatKey/App.xaml.cs` (app lifecycle, event wiring)
  - `tests/WhatKey.Tests/` (existing test infrastructure using interface stubs)
- Related patterns:
  - `ResetHoldStateForRuntimeUpdate()` (private, already does timer stop + flag reset + overlay hide) — will be made public/general
  - `IActiveWindowService` stub pattern shows how to test service behavior
  - No existing mouse hook or session/power subscriptions

## Development Approach

- **Testing approach**: Regular (code first, then tests)
- Complete each task fully before moving to the next
- **CRITICAL: every task MUST include new/updated tests**
- **CRITICAL: all tests must pass before starting next task**

## Implementation Steps

### Task 1: Add low-level mouse hook to cancel hold timer on mouse button press

**Files:**
- Modify: `WhatKey/Services/KeyboardHookService.cs`

- [x] Add P/Invoke constant `WH_MOUSE_LL = 14` and mouse message constants (`WM_LBUTTONDOWN = 0x0201`, `WM_RBUTTONDOWN = 0x0204`, `WM_MBUTTONDOWN = 0x0207`, `WM_XBUTTONDOWN = 0x020B`)
- [x] Add `_mouseHookHandle` (IntPtr) and `_mouseHookProc` (HookProc) fields to prevent GC collection
- [x] Install mouse hook in constructor after keyboard hook is installed (same thread, same `SetWindowsHookEx` call pattern)
- [x] Add `MouseHookCallback`: when any `WM_*BUTTONDOWN` fires, call `ResetHoldState()` (stop timer; if overlay visible, fire `TriggerHide` and set `_isOverlayVisible = false`; reset `_isHoldKeyDown = false`)
- [x] Uninstall mouse hook in `Dispose()` with `UnhookWindowsHookEx`
- [x] Rename private `ResetHoldStateForRuntimeUpdate()` to `ResetHoldState()` and make it `private` but also add a public `ForceResetHoldState()` that delegates to it (needed by Task 2)
- [x] write tests: simulate `ForceResetHoldState()` after timer started — verify `TriggerHide` fires when overlay was visible, verify `TriggerShow` never fires after reset
- [x] run project test suite — must pass before Task 2

### Task 2: Handle session lock and system sleep/wake to reset hold state

**Files:**
- Modify: `WhatKey/App.xaml.cs`

- [x] Subscribe to `SystemEvents.SessionSwitch` at startup (after `_hookService` is created)
- [x] In `OnSessionSwitch` handler: on `SessionSwitchReason.SessionLock`, `SessionSwitchReason.RemoteDisconnect`, `SessionSwitchReason.ConsoleDisconnect` — call `_hookService.ForceResetHoldState()`
- [x] Subscribe to `SystemEvents.PowerModeChanged` at startup
- [x] In `OnPowerModeChanged` handler: on `PowerModes.Suspend` — call `_hookService.ForceResetHoldState()`
- [x] Unsubscribe both events on app shutdown (in the existing shutdown path) to avoid static event leaks
- [x] write tests: verify `ForceResetHoldState()` when called externally resets state cleanly (timer stopped, flags cleared, `TriggerHide` fires iff overlay was visible)
- [x] run project test suite — must pass before Task 3

### Task 3: Verify acceptance criteria

- [ ] manual test: hold Ctrl, click mouse button — overlay must NOT appear
- [ ] manual test: hold Ctrl until overlay appears, click mouse — overlay must disappear
- [ ] manual test: hold Ctrl, press Win+L to lock screen, unlock — no stuck overlay
- [ ] manual test: hold Ctrl on sleep trigger, wake — no stuck overlay
- [ ] run full test suite: `dotnet test tests/WhatKey.Tests`
- [ ] verify build: `dotnet build` with 0 errors, 0 warnings

### Task 4: Update documentation

- [ ] update `CLAUDE.md` to note mouse hook and session/power reset behavior under "Runtime settings apply flow" or new section
- [ ] move this plan to `docs/plans/completed/`
