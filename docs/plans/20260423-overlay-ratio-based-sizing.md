# Overlay Ratio-Based Sizing

## Overview

Replace hardcoded pixel caps with screen-relative ratios and split the dual-purpose
`HotkeysListMaxHeight` into two explicit properties — one for column calculation, one for the
ScrollViewer cap. Fixes the regression where the previous dynamic-height plan collapsed all
layouts to a single column by making `maxListHeight` too large.

Key changes:
- `OverlayViewModel`: three new ratio constants, new `ColumnTargetHeight` mutable property, remove `OverlayHeaderFooterOverheadDips`, fix `UpdateLayoutForHotkeysCount` to use `ColumnTargetHeight`
- `OverlayWindow.xaml.cs`: compute `columnTargetHeight`/`scrollCapHeight`/`maxWidth` from monitor bounds via ratios
- `OverlayWindow.xaml`: remove `MaxWidth="980"` from `<Border>` (was blocking width expansion)
- Tests: rename/update broken tests, add tests for new behaviour

## Context

- `ViewModels/OverlayViewModel.cs` — constants, `HotkeysListMaxHeight`, `UpdateLayoutForHotkeysCount`
- `Views/OverlayWindow.xaml.cs` — `ShowWithGroups`, `Dispatcher.BeginInvoke` block
- `Views/OverlayWindow.xaml` — `<Border MaxWidth="980">` at line 22
- `tests/WhatKey.Tests/OverlayLayoutTests.cs` — all overlay layout tests
- Spec: `docs/superpowers/specs/2026-04-23-overlay-ratio-based-sizing-design.md`

## Development Approach

- **Testing approach**: Regular (code first, then tests)
- Complete each task fully before moving to the next
- **CRITICAL: every task MUST include new/updated tests**
- **CRITICAL: all tests must pass before starting next task**
- Run `dotnet test tests/WhatKey.Tests -c Release` after each task

## Implementation Steps

### Task 1: OverlayViewModel — ratio constants + ColumnTargetHeight + UpdateLayoutForHotkeysCount fix

**Files:**
- Modify: `ViewModels/OverlayViewModel.cs`
- Modify: `tests/WhatKey.Tests/OverlayLayoutTests.cs`

- [ ] Add three ratio constants:
  ```csharp
  public const double OverlayColumnTargetRatio = 0.65;
  public const double OverlayScrollCapRatio    = 0.90;
  public const double OverlayMaxWidthRatio     = 0.80;
  ```
- [ ] Remove `OverlayHeaderFooterOverheadDips` constant
- [ ] Add `_columnTargetHeight` backing field (default `DefaultHotkeysListMaxHeight`) and `ColumnTargetHeight` mutable property with `SetField`
- [ ] Update `UpdateLayoutForHotkeysCount` to pass `ColumnTargetHeight` instead of `HotkeysListMaxHeight` to `CalculateOverlayColumns`
- [ ] Rename `OverlayViewModel_UsesSharedHotkeysListMaxHeightForUiAndCalculation` → `OverlayViewModel_HotkeysListMaxHeight_DefaultIsScrollCapOnly`; assert only that `HotkeysListMaxHeight == DefaultHotkeysListMaxHeight`
- [ ] Rename `OverlayViewModel_UpdateLayoutForHotkeysCount_UsesDynamicHotkeysListMaxHeight` → `OverlayViewModel_UpdateLayoutForHotkeysCount_UsesColumnTargetHeight`; set `ColumnTargetHeight=60`, expect 2 columns for 3 hotkeys
- [ ] Add `OverlayViewModel_ColumnTargetHeight_DefaultEqualsHotkeysListMaxHeight` — both properties default to `DefaultHotkeysListMaxHeight`
- [ ] Add `OverlayViewModel_ColumnTargetHeight_IsSettableAndNotifiesProperty` — set value, assert returned + PropertyChanged raised
- [ ] Add `OverlayViewModel_UpdateLayoutForHotkeysCount_UsesColumnTargetHeightNotScrollCap` — set `ColumnTargetHeight=60`, `HotkeysListMaxHeight=9000`, call with 3 hotkeys, expect 2 columns
- [ ] Run `dotnet test tests/WhatKey.Tests -c Release` — must pass before Task 2

### Task 2: OverlayWindow.xaml.cs — ratio-based bounds computation in ShowWithGroups

**Files:**
- Modify: `Views/OverlayWindow.xaml.cs`
- Modify: `tests/WhatKey.Tests/OverlayLayoutTests.cs`

- [ ] In `Dispatcher.BeginInvoke`, immediately after `bounds` is computed, replace the existing `OverlayHeaderFooterOverheadDips` computation with:
  ```csharp
  var columnTargetHeight = Math.Max(
      OverlayViewModel.DefaultHotkeyRowHeight * 2,
      bounds.Height * OverlayViewModel.OverlayColumnTargetRatio);

  var scrollCapHeight = Math.Max(
      OverlayViewModel.DefaultHotkeyRowHeight * 2,
      bounds.Height * OverlayViewModel.OverlayScrollCapRatio);

  var maxWidth = Math.Min(
      bounds.Width,
      Math.Max(OverlayViewModel.DefaultOverlayMinWidth, bounds.Width * OverlayViewModel.OverlayMaxWidthRatio));

  _viewModel.ColumnTargetHeight = columnTargetHeight;
  _viewModel.HotkeysListMaxHeight = scrollCapHeight;
  UpdateLayout();
  ```
- [ ] Replace `MaxWidth = Math.Min(OverlayViewModel.DefaultOverlayMaxWidth, bounds.Width)` with `MaxWidth = maxWidth` and `MinWidth = Math.Min(OverlayViewModel.DefaultOverlayMinWidth, maxWidth)`
- [ ] Update `OverlayWindowCodeBehind_ShowWithGroups_SetsHotkeysListMaxHeightFromBounds`: remove `OverlayHeaderFooterOverheadDips` assertion, add assertions for `OverlayScrollCapRatio`, `OverlayColumnTargetRatio`, `bounds.Height * OverlayViewModel.OverlayScrollCapRatio`
- [ ] Update `OverlayWindowCodeBehind_ShowWithGroups_UpdatesColumnsAndKeepsShowPipeline`: remove old `MaxWidth = Math.Min(OverlayViewModel.DefaultOverlayMaxWidth, bounds.Width)` assertion, add `OverlayViewModel.OverlayMaxWidthRatio` and `_viewModel.ColumnTargetHeight = columnTargetHeight` assertions
- [ ] Run `dotnet test tests/WhatKey.Tests -c Release` — must pass before Task 3

### Task 3: OverlayWindow.xaml — remove Border MaxWidth hardcode

**Files:**
- Modify: `Views/OverlayWindow.xaml`
- Modify: `tests/WhatKey.Tests/OverlayLayoutTests.cs`

- [ ] Remove `MaxWidth="980"` from the `<Border>` element (currently line 22)
- [ ] Add `OverlayWindowXaml_BorderDoesNotHardcodeMaxWidth` — assert XAML does not contain `MaxWidth="980"`
- [ ] Run `dotnet test tests/WhatKey.Tests -c Release` — must pass before Task 4

### Task 4: Verify acceptance criteria + update documentation

**Files:**
- Modify: `CLAUDE.md`
- Modify: `docs/plans/20260423-overlay-ratio-based-sizing.md` (move to completed)

- [ ] Run full test suite: `dotnet test tests/WhatKey.Tests -c Release`
- [ ] Run `dotnet build` — verify 0 errors, 0 warnings
- [ ] Update `CLAUDE.md` "Overlay layout behavior" section: reflect `ColumnTargetHeight` (65% screen height) vs `HotkeysListMaxHeight` (90% screen height scroll cap), ratio constants, Border MaxWidth removal
- [ ] Move plan to `docs/plans/completed/`

## Post-Completion

**Manual verification:**
- Trigger overlay on Firefox (≥27 hotkeys) — confirm 2 columns, window wider than 420px
- Trigger overlay on an app with >46 hotkeys — confirm 3 columns
- Confirm scrollbar appears only when content overflows 90% of screen height
- Confirm window does not overflow monitor work area on any screen
