# Overlay Ratio-Based Sizing Design

## Overview

Replace hardcoded pixel caps in the overlay window with screen-relative ratios. Separate the
column-trigger height from the scroll-cap height so each concept has a single, explicit owner.

## Problem

After the dynamic-height plan was implemented, `HotkeysListMaxHeight` was set to
`screenHeight - 150` (~930px on 1080p). `CalculateOverlayColumns` then used this value as the
target height per column, producing `rowsPerColumn = 930 / 30 = 31`. Firefox's ~27 hotkeys fit
in one column, collapsing the 3-column layout. Additionally, `MaxWidth` was still capped at the
hardcoded 980px constant instead of adapting to monitor width.

## Goals

- Window expands horizontally when more hotkeys require more columns.
- Column count is driven by 65% of screen height (comfortable reading height per column).
- Window height is capped at 90% of screen height; scrollbar appears beyond that.
- Window max width is 80% of screen width.

## Non-Goals

- Changing column count algorithm beyond the height parameter fix.
- Changing minimum width (stays 420px).
- Changing row height constant (stays 30px).

## Architecture

Two new ViewModel properties replace the single `HotkeysListMaxHeight` dual-use:

| Property | Purpose | ScrollViewer binding? | Column calc? |
| --- | --- | --- | --- |
| `HotkeysListMaxHeight` | Scroll cap (90% screen height) | ✅ | ❌ |
| `ColumnTargetHeight` | Column trigger (65% screen height) | ❌ | ✅ |

`ShowWithGroups` computes both from monitor bounds using ratio constants and sets them before
any layout or column calculation.

## OverlayViewModel Changes

### New constants

```csharp
public const double OverlayColumnTargetRatio = 0.65;
public const double OverlayScrollCapRatio    = 0.90;
public const double OverlayMaxWidthRatio     = 0.80;
```

Remove `OverlayHeaderFooterOverheadDips` (replaced by ratio approach).

### New property

```csharp
private double _columnTargetHeight = DefaultHotkeysListMaxHeight;

public double ColumnTargetHeight
{
    get => _columnTargetHeight;
    set => SetField(ref _columnTargetHeight, value);
}
```

Default matches `DefaultHotkeysListMaxHeight` so behaviour before `ShowWithGroups` is called is
unchanged.

### UpdateLayoutForHotkeysCount

Use `ColumnTargetHeight` (not `HotkeysListMaxHeight`):

```csharp
public void UpdateLayoutForHotkeysCount(int hotkeysCount, double availableWidth = double.PositiveInfinity)
{
    OverlayColumns = CalculateOverlayColumns(hotkeysCount, ColumnTargetHeight, availableWidth: availableWidth);
}
```

## OverlayWindow.xaml.cs Changes

Inside `Dispatcher.BeginInvoke`, immediately after `bounds` is computed:

```csharp
var columnTargetHeight = Math.Max(
    OverlayViewModel.DefaultHotkeyRowHeight * 2,
    bounds.Height * OverlayViewModel.OverlayColumnTargetRatio);

var scrollCapHeight = Math.Max(
    OverlayViewModel.DefaultHotkeyRowHeight * 2,
    bounds.Height * OverlayViewModel.OverlayScrollCapRatio);

var maxWidth = Math.Max(
    OverlayViewModel.DefaultOverlayMinWidth,
    bounds.Width * OverlayViewModel.OverlayMaxWidthRatio);

_viewModel.ColumnTargetHeight = columnTargetHeight;
_viewModel.HotkeysListMaxHeight = scrollCapHeight;
UpdateLayout();

MinWidth = Math.Min(OverlayViewModel.DefaultOverlayMinWidth, maxWidth);
MaxWidth = maxWidth;
_viewModel.UpdateLayoutForHotkeysCount(totalHotkeys, MaxWidth);
UpdateLayout();
var listWidth = GetAvailableHotkeysListWidth();
_viewModel.UpdateLayoutForHotkeysCount(totalHotkeys, listWidth);
```

`DefaultOverlayMaxWidth` constant is kept unchanged (used in `CalculateOverlayColumns_WithWideFiniteWidth_StillCapsAtThreeColumns` as `availableWidth` — that test remains valid as-is). It is no longer referenced in production code.

## OverlayWindow.xaml Changes

None. `MaxHeight="650"` already removed in previous plan. ScrollViewer binding to
`HotkeysListMaxHeight` unchanged.

## Tests (OverlayLayoutTests.cs)

### Update

- `OverlayViewModel_UpdateLayoutForHotkeysCount_UsesDynamicHotkeysListMaxHeight` →
  rename to `…UsesColumnTargetHeight`; test `ColumnTargetHeight` setter, not `HotkeysListMaxHeight`.
- `OverlayWindowCodeBehind_ShowWithGroups_SetsHotkeysListMaxHeightFromBounds` →
  assert `OverlayScrollCapRatio`, `OverlayColumnTargetRatio` strings present; remove
  `OverlayHeaderFooterOverheadDips` assertion.
- `OverlayWindowCodeBehind_ShowWithGroups_UpdatesColumnsAndKeepsShowPipeline` →
  add assertion for `_viewModel.ColumnTargetHeight = columnTargetHeight`.

### Add

- `OverlayViewModel_ColumnTargetHeight_DefaultEqualsHotkeysListMaxHeight` — both default to `DefaultHotkeysListMaxHeight`.
- `OverlayViewModel_ColumnTargetHeight_IsSettableAndNotifiesProperty` — set value, assert returned and PropertyChanged raised.
- `OverlayViewModel_UpdateLayoutForHotkeysCount_UsesColumnTargetHeightNotScrollCap` — set
  `ColumnTargetHeight=60`, `HotkeysListMaxHeight=9000`, call with 3 hotkeys, expect 2 columns
  (proves column calc ignores the scroll cap).

## Acceptance Criteria

- On 1080p: Firefox (~27 hotkeys) displays in 2 columns; window width grows beyond 420px.
- On 1080p: Very large hotkey set (>31 hotkeys) shows 3 columns.
- On 1080p: If content overflows 90% screen height, scrollbar appears.
- All existing tests pass; new tests pass.
- `dotnet build` — 0 errors, 0 warnings.
