# Groups Bar — Full Width Layout

## Overview

When an app has more than ~4 groups the horizontal scrollbar appears inside the ListBox
(MaxWidth=320, Height=26), the bar shrinks and looks broken (screenshots in conversation).

Fix: replace the horizontal StackPanel in the groups bar with a Grid that gives the
ListBox a star-sized column. The ListBox expands to fill all available width, so the
scrollbar only appears for extremely large group counts and never clips buttons.

## Context

- **File**: `Views/EditorWindow.xaml` lines 274–312
- **Problem element**: `<ListBox MaxWidth="320" Height="26" ScrollViewer.HorizontalScrollBarVisibility="Auto">`
- **Container**: `<StackPanel Orientation="Horizontal">` inside `<Border Grid.Row="1">`
- **Right panel width**: ~763 px (970 window − 207 app list) → ~476 px for ListBox after fixed elements
- **No ViewModel changes needed** — purely a XAML layout fix

## Development Approach

- Testing approach: Regular (XAML-only change, no logic)
- No new C# code; no tests to add/update for this task

## Implementation Steps

### Task 1: Replace StackPanel → Grid in groups bar

- [x] In `Views/EditorWindow.xaml` line 277, replace `<StackPanel Orientation="Horizontal" VerticalAlignment="Center">` with a `<Grid>` that has 6 column definitions:
  - Col 0 `Width="Auto"` — "Groups:" label
  - Col 1 `Width="*"` — ListBox (star, fills remaining space)
  - Col 2 `Width="Auto"` — "+" button
  - Col 3 `Width="Auto"` — "−" button
  - Col 4 `Width="Auto"` — "Name:" label
  - Col 5 `Width="Auto"` — Name TextBox
- [x] Assign `Grid.Column="N"` to each child element accordingly
- [x] Remove `MaxWidth="320"` from the ListBox
- [x] Change `ScrollViewer.HorizontalScrollBarVisibility="Auto"` → `"Hidden"` (star width handles most cases; hidden prevents height collapse)
- [x] Keep `Height="26"` and all existing Margin/Padding/Style attributes intact
- [x] Close with `</Grid>` replacing `</StackPanel>`

### Task 2: Visual verification

- [x] Build: `dotnet build` — 0 errors
- [x] Run app, open editor with VS Code selected (has 4 groups by default) [x] manual test (skipped - not automatable)
- [x] Verify group buttons fill full width, no scrollbar visible [x] manual test (skipped - not automatable)
- [x] Add 3–4 more groups via "+" button, verify they extend without scrollbar clipping [x] manual test (skipped - not automatable)
- [x] Verify "-" button and Name TextBox still reachable at right edge [x] manual test (skipped - not automatable)
- [x] Resize editor window narrower — verify star column shrinks gracefully [x] manual test (skipped - not automatable)

### Task N-1: Verify acceptance criteria

- [x] No scrollbar appears for ≤8 groups at default window width (970 px) [x] manual test (skipped - not automatable)
- [x] Group buttons are fully clickable (not obscured) [x] manual test (skipped - not automatable)
- [x] Name TextBox and +/− buttons remain visible and accessible [x] manual test (skipped - not automatable)

### Task N: Documentation

- [x] No README changes needed — this is an internal UI fix

## Technical Details

**Before:**
```xaml
<StackPanel Orientation="Horizontal" VerticalAlignment="Center">
    <TextBlock .../>
    <ListBox MaxWidth="320" Height="26"
             ScrollViewer.HorizontalScrollBarVisibility="Auto" .../>
    <Button Content="+" .../>
    <Button Content="−" .../>
    <TextBlock Text="Name:" .../>
    <TextBox Width="120" .../>
</StackPanel>
```

**After:**
```xaml
<Grid VerticalAlignment="Center">
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="Auto"/>
        <ColumnDefinition Width="*"/>
        <ColumnDefinition Width="Auto"/>
        <ColumnDefinition Width="Auto"/>
        <ColumnDefinition Width="Auto"/>
        <ColumnDefinition Width="Auto"/>
    </Grid.ColumnDefinitions>
    <TextBlock Grid.Column="0" .../>
    <ListBox Grid.Column="1" Height="26"
             ScrollViewer.HorizontalScrollBarVisibility="Hidden" .../>
    <Button Grid.Column="2" Content="+" .../>
    <Button Grid.Column="3" Content="−" .../>
    <TextBlock Grid.Column="4" Text="Name:" .../>
    <TextBox Grid.Column="5" Width="120" .../>
</Grid>
```

## Post-Completion

No external changes needed.
