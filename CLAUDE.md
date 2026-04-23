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

- `KeyboardHookService` installs a low-level mouse hook (`WH_MOUSE_LL`) alongside the keyboard hook in `Install()`. The mouse hook delegate is stored in the constructor to prevent GC, but `SetWindowsHookEx` runs in `Install()`.
- Mouse hook installation failure is non-fatal: logs a `Trace.TraceWarning` and continues without mouse-dismiss; keyboard hook failure remains fatal (throws `InvalidOperationException`).
- Any `WM_LBUTTONDOWN`, `WM_RBUTTONDOWN`, `WM_MBUTTONDOWN`, or `WM_XBUTTONDOWN` event calls `ResetHoldState()` internally **only when hold mode is active** (`_isHoldKeyDown || _holdTimer.IsEnabled`): stops the hold timer, fires `TriggerHide` if the overlay was visible, clears `_isHoldKeyDown` and `_isOverlayVisible`. Toggle-pinned overlays (hold key not active, timer stopped) are not dismissed by mouse clicks.
- `ForceResetHoldState()` is the public entry point for external callers (session lock, sleep); delegates to the same private `ResetHoldState()`.
- `App.xaml.cs` subscribes to `SystemEvents.SessionSwitch` and `SystemEvents.PowerModeChanged` at startup and calls `_hookService.ForceResetHoldState()` on `SessionLock`, `RemoteDisconnect`, `ConsoleDisconnect`, and `Suspend`.
- Both `SystemEvents` subscriptions are removed on app shutdown to avoid static event leaks.
- Mouse hook is uninstalled in `KeyboardHookService.Dispose()` via `UnhookWindowsHookEx`.

## Overlay layout behavior

- `OverlayViewModel` owns deterministic column count selection for the hotkeys overlay (`1/2/3` columns).
- Column count is recalculated in `ShowWithGroups(...)` based on total hotkey count across all groups and max overlay height constraints.
- `Views/OverlayWindow.xaml` binds layout columns from the view model instead of hardcoding a single vertical list.
- Keep column selection logic UI-independent so it remains covered by `tests/WhatKey.Tests/OverlayLayoutTests.cs`.
- Two separate height properties on `OverlayViewModel`: `HotkeysListMaxHeight` (scroll cap, 90% of screen height, bound to ScrollViewer `MaxHeight`) and `ColumnTargetHeight` (column trigger, 65% of screen height, passed to `CalculateOverlayColumns`). Both default to `DefaultHotkeysListMaxHeight = 460` before `ShowWithGroups` is called.
- `ShowWithGroups` computes all three sizing values from monitor bounds using ratio constants: `OverlayColumnTargetRatio = 0.65`, `OverlayScrollCapRatio = 0.90`, `OverlayMaxWidthRatio = 0.80`. Max width is clamped to `bounds.Width` to avoid exceeding the work area on narrow screens.
- Window XAML has no hardcoded `MaxHeight` or `MaxWidth` on either the `<Window>` or `<Border>` elements; all sizing is controlled at runtime via code-behind and the ScrollViewer binding.

## HotkeyGroup model and group management

- `HotkeyGroup` (`Models/HotkeyGroup.cs`): `Name` (string) + `Hotkeys` (ObservableCollection<HotkeyEntry>). Groups organize hotkeys into named categories per app.
- `AppHotkeys.Groups` (`[JsonProperty("groups")]`) is the canonical list of groups. Legacy `Hotkeys` is kept with `NullValueHandling.Ignore` for backwards-compat JSON reading only; never written back to disk.
- `NormalizeData()` migrates old flat `hotkeys` array: wraps it in a single `HotkeyGroup { Name = "General" }` pushed into `Groups`, then sets `Hotkeys = null`. Mirrors the `processName → processNames` migration pattern.
- `GetGroupsForProcess(string processName)` returns `List<HotkeyGroup>` using same lookup as `GetHotkeysForProcess`. `GetHotkeysForProcess` now flattens all groups' hotkeys (used by keyboard hook for matching).
- `App.xaml.cs` calls `GetGroupsForProcess` and passes result to `OverlayWindow.ShowWithGroups(groups, processName, hwnd)`.
- `EditorViewModel` exposes `Groups: ObservableCollection<HotkeyGroup>` (synced from `SelectedApp.Groups`) and `SelectedGroup: HotkeyGroup`. When `SelectedApp` changes, `Groups` refreshes and first group is auto-selected.
- `AddGroupCommand` / `RemoveGroupCommand` add/remove groups from both `SelectedApp.Groups` and `Groups`; `AddHotkeyCommand` / `RemoveHotkeyCommand` target `SelectedGroup.Hotkeys`.
- New apps (`AddApp`) are initialized with `Groups = [{ Name = "General" }]`.

## KeyTokensConverter

- `Converters/KeyTokensConverter.cs`: `IValueConverter` taking the `Keys` string of a `HotkeyEntry` and returning `List<KeyToken>`.
- Splits on `+` for modifiers and `space` for chords (e.g., `Ctrl+K Ctrl+W`).
- Creates `KeyToken` objects with text and brush color; inserts separator tokens for `+` and ` `.
- Priority order for brush selection (first match wins):
  - Any token matches `F([1-9]|1[0-2])` (case-insensitive, exact) → green `#A6E3A1` (function keys)
  - Any token matches `Ctrl|Alt|Shift|Win|Esc|Tab|Meta` (case-insensitive) → yellow `#F9E2AF` (modifiers)
  - Otherwise → default lavender `#CDD6F4`
  - Separators use dark gray `#6C7086`
- Returns empty list on null input.
- Registered in `OverlayWindow.xaml` `Window.Resources` and bound to `ItemsSource` of key tokens `ItemsControl`.
