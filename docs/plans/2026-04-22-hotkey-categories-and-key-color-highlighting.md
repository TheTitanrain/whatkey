---
# Hotkey Categories (HotkeyGroup) and Key Color Highlighting

## Overview
Add `HotkeyGroup` model that organizes hotkeys into named categories per app. The overlay displays groups with section headers. F1-F12 keys are highlighted with one accent color; modifier keys (Ctrl, Alt, Shift, Win, Esc, Tab) are highlighted with a second accent color. The editor gains group management UI.

## Context
- Files involved:
  - `Models/HotkeyEntry.cs`, `Models/AppHotkeys.cs` — model changes
  - `Models/HotkeyGroup.cs` — new file
  - `Services/HotkeysStorageService.cs` — migration + new GetGroupsForProcess
  - `ViewModels/OverlayViewModel.cs`, `ViewModels/EditorViewModel.cs`
  - `Views/OverlayWindow.xaml`, `Views/OverlayWindow.xaml.cs`
  - `Views/EditorWindow.xaml`
  - `Converters/KeyAccentBrushConverter.cs` — new file
  - `App.xaml.cs`
  - `Assets/hotkeys.defaults.json`
  - `tests/WhatKey.Tests/OverlayLayoutTests.cs`, `EditorViewModelCommandTests.cs`, `HotkeysStorageServiceTests.cs`
  - `tests/WhatKey.Tests/HotkeyGroupMigrationTests.cs` — new file
  - `tests/WhatKey.Tests/KeyAccentBrushConverterTests.cs` — new file
- Related patterns: legacy `processName → processNames` migration (same pattern for `hotkeys → groups`)
- Dependencies: none new

## Development Approach
- **Testing approach**: Regular (code first, then tests)
- Complete each task fully before moving to the next
- **CRITICAL: every task MUST include new/updated tests**
- **CRITICAL: all tests must pass before starting next task**

## Implementation Steps

### Task 1: HotkeyGroup model, data migration, storage service

**Files:**
- Create: `Models/HotkeyGroup.cs`
- Modify: `Models/AppHotkeys.cs`
- Modify: `Services/HotkeysStorageService.cs`
- Modify: `ViewModels/EditorViewModel.cs` (CloneApps, AddApp)
- Modify: `Assets/hotkeys.defaults.json`
- Create: `tests/WhatKey.Tests/HotkeyGroupMigrationTests.cs`
- Modify: `tests/WhatKey.Tests/HotkeysStorageServiceTests.cs`

- [x] Create `HotkeyGroup` with `public string Name` and `public ObservableCollection<HotkeyEntry> Hotkeys`
- [x] In `AppHotkeys`: add `[JsonProperty("groups")] public ObservableCollection<HotkeyGroup> Groups`; mark existing `Hotkeys` with `[JsonProperty(NullValueHandling = NullValueHandling.Ignore)]` (null after migration, never written)
- [x] In `NormalizeData()`: if `app.Hotkeys != null`, wrap in a single `HotkeyGroup { Name = "General", Hotkeys = app.Hotkeys }` added to `app.Groups`, then set `app.Hotkeys = null`; initialize `app.Groups` if null
- [x] Add `GetGroupsForProcess(string processName) → List<HotkeyGroup>` (same lookup logic as `GetHotkeysForProcess`, returns groups from matching app)
- [x] Rewrite `GetHotkeysForProcess` to call `GetGroupsForProcess` and flatten all groups' hotkeys
- [x] In `EditorViewModel.AddApp()`: initialize new app with `Groups = new ObservableCollection<HotkeyGroup> { new HotkeyGroup { Name = "General" } }`
- [x] In `EditorViewModel.CloneApps()`: clone `Groups` (not `Hotkeys`)
- [x] Update `hotkeys.defaults.json`: replace `"hotkeys": [...]` with `"groups": [{"name": "General", "hotkeys": [...]}]` for all apps; give VS Code meaningful groups (File, Editing, Navigation, View) and Total Commander groups (File Operations, Navigation, Selection)
- [x] Write `HotkeyGroupMigrationTests`: test `NormalizeData` migrates flat `hotkeys` JSON to single "General" group; test `GetGroupsForProcess` returns correct groups; test empty groups initialized on normalize
- [x] Update `HotkeysStorageServiceTests` if any tests reference flat `app.Hotkeys`
- [x] Run `dotnet test` — must pass before Task 2

### Task 2: Overlay — group display and key color highlighting

**Files:**
- Create: `Converters/KeyAccentBrushConverter.cs`
- Create: `tests/WhatKey.Tests/KeyAccentBrushConverterTests.cs`
- Modify: `ViewModels/OverlayViewModel.cs`
- Modify: `Views/OverlayWindow.xaml`
- Modify: `Views/OverlayWindow.xaml.cs`
- Modify: `App.xaml.cs`
- Modify: `tests/WhatKey.Tests/OverlayLayoutTests.cs`

- [ ] Create `KeyAccentBrushConverter : IValueConverter` in `Converters/` — input is the Keys string of a `HotkeyEntry`; split on `+` and trim each token; priority order: if any token matches a function key pattern (`F([1-9]|1[0-2])`, case-insensitive, exact token) return green brush `#A6E3A1`; else if any token matches a modifier key set (`Ctrl`, `Alt`, `Shift`, `Win`, `Esc`, `Tab`, `Meta`, case-insensitive) return yellow brush `#F9E2AF`; else return default brush `#89DCEB`; return default on null/exception
- [ ] Write `KeyAccentBrushConverterTests`: verify F1 → green, F12 → green, non-F-prefix tokens not matched (e.g., "File" should not match); Ctrl+S → yellow, Alt+F4 → green (F-key wins), Shift+A → yellow, plain letter → default
- [ ] `OverlayViewModel`: replace `Hotkeys: ObservableCollection<HotkeyEntry>` with `Groups: ObservableCollection<HotkeyGroup>`; update `EmptyMessageVisibility` to check `Groups == null || !Groups.Any(g => g.Hotkeys?.Count > 0)`; keep `CalculateOverlayColumns` and `UpdateLayoutForHotkeysCount` (caller computes total count)
- [ ] `OverlayWindow.xaml`: replace flat `ItemsControl ItemsSource="{Binding Hotkeys}"` with outer `ItemsControl ItemsSource="{Binding Groups}"` (StackPanel panel); each group item shows a `TextBlock` header (group name, styled as section label with separator line) followed by inner `ItemsControl` for that group's hotkeys; apply `KeyAccentBrushConverter` to key TextBlock foreground (binding to `Keys` property); register converter in `Window.Resources`
- [ ] `OverlayWindow.xaml.cs`: rename `ShowWithHotkeys` to `ShowWithGroups(List<HotkeyGroup> groups, string processName, IntPtr sourceHwnd)`; compute `var totalHotkeys = groups.Sum(g => g.Hotkeys?.Count ?? 0)` and pass to `UpdateLayoutForHotkeysCount`; set `_viewModel.Groups`
- [ ] `App.xaml.cs`: call `GetGroupsForProcess` instead of `GetHotkeysForProcess`; call `ShowWithGroups`
- [ ] Update `OverlayLayoutTests`: update checks to reflect groups-based binding; update code-behind checks for `ShowWithGroups` method; keep all `CalculateOverlayColumns` pure-function tests unchanged
- [ ] Run `dotnet test` — must pass before Task 3

### Task 3: Editor — group management UI

**Files:**
- Modify: `ViewModels/EditorViewModel.cs`
- Modify: `Views/EditorWindow.xaml`
- Modify: `tests/WhatKey.Tests/EditorViewModelCommandTests.cs`

- [ ] `EditorViewModel`: add `SelectedGroup: HotkeyGroup` (notifying), `Groups: ObservableCollection<HotkeyGroup>` (synced from `SelectedApp.Groups`); add `AddGroupCommand`, `RemoveGroupCommand`
- [ ] When `SelectedApp` changes: refresh `Groups` from `SelectedApp.Groups`, auto-select first group, reset `SelectedHotkey = null`
- [ ] `AddGroupCommand`: add `HotkeyGroup { Name = "New Group" }` to both `SelectedApp.Groups` and `Groups`, select it; guard: SelectedApp != null
- [ ] `RemoveGroupCommand`: remove `SelectedGroup` from `SelectedApp.Groups` and `Groups`; select first remaining group or null; guard: SelectedApp != null && SelectedGroup != null
- [ ] `AddHotkeyCommand`: add to `SelectedGroup.Hotkeys`; guard: SelectedApp != null && SelectedGroup != null
- [ ] `RemoveHotkeyCommand`: remove from `SelectedGroup.Hotkeys`; guard: both non-null
- [ ] `EditorWindow.xaml`: above DataGrid, add group bar — `ListBox` with `ItemsSource="{Binding Groups}"`, `SelectedItem="{Binding SelectedGroup}"` showing group names (inline TextBox binding to `SelectedGroup.Name`); Add Group / Remove Group buttons beside it; DataGrid `ItemsSource` → `{Binding SelectedGroup.Hotkeys}`
- [ ] Update `EditorViewModelCommandTests`: test `AddGroupCommand` adds group and selects it; test `RemoveGroupCommand` removes group and selects fallback; test `AddHotkeyCommand` adds to SelectedGroup; test app selection auto-selects first group
- [ ] Run `dotnet test` — must pass before Task 4

### Task 4: Verify acceptance criteria and documentation

- [ ] Run full test suite: `dotnet test tests/WhatKey.Tests`
- [ ] Update `CLAUDE.md`: add section on HotkeyGroup model (Name + Hotkeys), migration pattern, KeyAccentBrushConverter color tiers (F-keys green, modifiers yellow, default blue), GetGroupsForProcess usage, group management in EditorViewModel
- [ ] Move this plan to `docs/plans/completed/`
