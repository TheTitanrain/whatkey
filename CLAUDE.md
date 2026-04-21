# Internal Development Notes

## Runtime settings apply flow

- `EditorViewModel.SaveCommand` persists data through `HotkeysStorageService.Save(...)`.
- After successful save, `EditorViewModel` raises `SettingsSaved`.
- `App.xaml.cs` wires `editorViewModel.SettingsSaved` at startup and invokes `KeyboardHookService.UpdateSettings(...)` with saved settings.
- After successful runtime apply, `App.xaml.cs` saves once more to persist normalized runtime values back to disk atomically.
- `KeyboardHookService.UpdateSettings(...)` applies runtime fields immediately:
  - `HoldDelayMs`
  - `HoldKey` (including virtual key recalculation)
  - `ToggleHotkey` (stable unregister/register with current values, with rollback from applied runtime snapshot if registration fails)
  - hold runtime state reset (`_holdTimer`, hold flag, overlay visibility) when hold key changes
  - dispatcher affinity guard (`Dispatcher.CheckAccess`) before mutating timer/window-bound state
- Failed runtime apply now throws, and `App.xaml.cs` restores and re-saves the last applied settings snapshot to keep disk/runtime state consistent.
- If rollback cannot restore the previous toggle hotkey, `KeyboardHookService` throws `HotkeyRecoveryException` and `App.xaml.cs` shuts down to avoid degraded runtime state.
- Startup now fails fast with a user-visible error if initial toggle hotkey registration cannot be installed.
- `AppSettings` implements `INotifyPropertyChanged` so rollback/normalization mutations are reflected in bound editor UI.

## Constraints

- Keep the app-level lifecycle in `App.xaml.cs`; do not move `KeyboardHookService` ownership into view models.
- Runtime updates must be applied without app restart when editor settings are saved.
- Preserve tests in `tests/WhatKey.Tests` that guard Save -> runtime apply behavior.
- Tests must not touch real `%APPDATA%` user settings; use isolated temporary storage paths.

## Multiple process names per app entry

- `AppHotkeys.ProcessNames` (`List<string>`, JSON key `processNames`) is the canonical list of process names for an entry.
- Legacy `processName` (single string) is kept on the model with `NullValueHandling.Ignore` for backwards-compat JSON reading only; it is never written back to disk.
- `NormalizeData()` migrates old `processName` → `ProcessNames`, lowercases all names, and sets `ProcessName = null`.
- `GetHotkeysForProcess()` and `GetDefaultHotkeys()` iterate `ProcessNames`; "default" entry is also detected via `ProcessNames.Contains("default")`.
- `EditorViewModel.ProcessNamesText` is a raw-string property backed by `_processNamesRaw` (no normalization in the setter) so the TextBox accepts mid-entry input like `totalcmd,` without rewriting it on every keypress.
- Parsing into `ProcessNames` happens in `FlushProcessNamesText()`, called when `SelectedApp` changes and at the top of `Save()`.

## Hold state reset

- `KeyboardHookService` installs a low-level mouse hook (`WH_MOUSE_LL`) alongside the keyboard hook at construction time.
- Any `WM_LBUTTONDOWN`, `WM_RBUTTONDOWN`, `WM_MBUTTONDOWN`, or `WM_XBUTTONDOWN` event calls `ResetHoldState()` internally **only when hold mode is active** (`_isHoldKeyDown || _holdTimer.IsEnabled`): stops the hold timer, fires `TriggerHide` if the overlay was visible, clears `_isHoldKeyDown` and `_isOverlayVisible`. Toggle-pinned overlays (hold key not active, timer stopped) are not dismissed by mouse clicks.
- `ForceResetHoldState()` is the public entry point for external callers (session lock, sleep); delegates to the same private `ResetHoldState()`.
- `App.xaml.cs` subscribes to `SystemEvents.SessionSwitch` and `SystemEvents.PowerModeChanged` at startup and calls `_hookService.ForceResetHoldState()` on `SessionLock`, `RemoteDisconnect`, `ConsoleDisconnect`, and `Suspend`.
- Both `SystemEvents` subscriptions are removed on app shutdown to avoid static event leaks.
- Mouse hook is uninstalled in `KeyboardHookService.Dispose()` via `UnhookWindowsHookEx`.

## Overlay layout behavior

- `OverlayViewModel` owns deterministic column count selection for the hotkeys overlay (`1/2/3` columns).
- Column count is recalculated in `ShowWithHotkeys(...)` based on current hotkey count and max overlay height constraints.
- `Views/OverlayWindow.xaml` binds layout columns from the view model instead of hardcoding a single vertical list.
- Keep column selection logic UI-independent so it remains covered by `tests/WhatKey.Tests/OverlayLayoutTests.cs`.
