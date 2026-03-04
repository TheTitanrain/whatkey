# Internal Development Notes

## Runtime settings apply flow

- `EditorViewModel.SaveCommand` persists data through `HotkeysStorageService.Save(...)`.
- After successful save, `EditorViewModel` raises `SettingsSaved`.
- `App.xaml.cs` wires this event at startup via `RuntimeSettingsCoordinator.Attach(...)`.
- The coordinator invokes `KeyboardHookService.UpdateSettings(...)` with saved settings.
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
