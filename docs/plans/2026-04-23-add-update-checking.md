# Add Update Checking

## Overview
Add a "Check for Updates" button to the About window and tray menu, plus auto-check on
startup. Updates are fetched from GitHub Releases API. Auto-check shows a tray balloon;
manual check shows a dialog.

## Context
- Files involved: `WhatKey.csproj`, `Services/UpdateCheckResult.cs` (new),
  `Services/UpdateService.cs` (new), `Views/AboutWindow.xaml`,
  `Views/AboutWindow.xaml.cs`, `App.xaml.cs`
- Related patterns: `AutostartService.cs` (static service), `AboutWindow` code-behind
  style, tray balloon via `TaskbarIcon.ShowBalloonTip`
- Dependencies: `System.Net.Http` (in-box .NET 4.8), `System.Text.Json` (already referenced)
- GitHub API endpoint: `https://api.github.com/repos/TheTitanrain/whatkey/releases/latest`, field `tag_name`
- Current version hardcoded as "Version 1.0" in AboutWindow.xaml — will move to `<Version>` in csproj

## Development Approach
- Testing approach: Regular (code first, then tests)
- Complete each task fully before moving to the next
- CRITICAL: every task MUST include new/updated tests
- CRITICAL: all tests must pass before starting next task

## Implementation Steps

### Task 1: Add assembly version and create UpdateService

**Files:**
- Modify: `WhatKey.csproj`
- Create: `Services/UpdateCheckResult.cs`
- Create: `Services/UpdateService.cs`
- Create: `tests/WhatKey.Tests/UpdateServiceTests.cs`

- [x] Add `<Version>1.0.0</Version>` to WhatKey.csproj PropertyGroup
- [x] Create `UpdateCheckResult` record-style class: `bool UpdateAvailable`, `string LatestVersion`, `string ReleaseUrl`
- [x] Create `UpdateService` with constructor accepting `Func<Task<string>>` for injectable fetch (defaults to live GitHub API call)
- [x] Implement `CheckForUpdateAsync(Version currentVersion)`: fetch JSON, parse `tag_name` and `html_url` via `System.Text.Json`, strip leading `v`, compare with `Version.Parse`, return `UpdateCheckResult`
- [x] Use `static readonly HttpClient` for the default fetcher (avoids socket exhaustion, fine for single-process app)
- [x] Write unit tests: update available, already up to date, network error returns null/throws, tag with and without `v` prefix
- [x] Run `dotnet test` — must pass

### Task 2: Check for Updates button in About window

**Files:**
- Modify: `Views/AboutWindow.xaml`
- Modify: `Views/AboutWindow.xaml.cs`

- [x] Replace hardcoded "Version 1.0" TextBlock with binding to `Assembly.GetExecutingAssembly().GetName().Version` formatted as `v{major}.{minor}.{build}`
- [x] Add "Check for Updates" Button in bottom bar next to Close button (same style as Close)
- [x] Add async `CheckUpdates_Click` handler in code-behind: creates `UpdateService`, calls `CheckForUpdateAsync`, shows `MessageBox` — either "You're up to date" or "Version X.X available — open release page?" (Yes/No), Yes opens GitHub releases URL via `Process.Start`
- [x] Handle exceptions: show "Could not check for updates: {message}" on failure
- [x] Run `dotnet test` — must pass (no new tests needed for UI wiring)

### Task 3: Tray menu item + auto-check on startup

**Files:**
- Modify: `App.xaml.cs`

- [x] Add `UpdateService _updateService` field, instantiate in `OnStartup`
- [x] Add "Check for updates" `MenuItem` to tray context menu (between autostart and About separator): click handler calls `CheckForUpdateAsync`, shows `MessageBox` with result (same logic as About window handler — extract to private method `ShowUpdateResult(UpdateCheckResult)`)
- [x] After `InitializeTray()`, fire-and-forget auto-check: `_ = CheckForUpdatesInBackgroundAsync()` — private async method that catches all exceptions silently; if update available, calls `_trayIcon.ShowBalloonTip("Update available", "WhatKey {version} is available. Click to open.", BalloonIcon.Info)` and wires one-time `TrayBalloonTipClicked` to open release URL
- [x] Run `dotnet test` — must pass

### Task 4: Verify acceptance criteria

- [x] Run `dotnet test` (full suite)
- [x] Run `dotnet build` — 0 errors, 0 warnings
- [x] Manual smoke: launch app, About window shows version + button works, tray menu item works, startup balloon appears if update available (manual test - skipped, not automatable)

### Task 5: Update documentation

- [ ] Update CLAUDE.md with update-check service pattern (injectable fetch delegate for testability)
- [ ] Move this plan to `docs/plans/completed/`
