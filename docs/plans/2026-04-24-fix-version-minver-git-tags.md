---
# Fix: Automatic Version from Git Tags (About Window + UpdateService)

## Overview
About window always shows v1.0.0 because `<Version>1.0.0</Version>` in WhatKey.csproj is
hardcoded and never updated. We add MinVer (NuGet package) to drive the assembly version
from git tags automatically. Workflow becomes: create tag v1.7.0 → build → assembly
version = 1.7.0 → About window shows it. No manual csproj edits on each release.

## Context
- Files involved: `WhatKey.csproj`
- About window reads `Assembly.GetExecutingAssembly().GetName().Version` at `Views/AboutWindow.xaml.cs:16` — no code changes needed
- UpdateService also uses assembly version for comparison — correct after fix
- Latest tag: `v1.6.0`, target version for this branch: `v1.7.0`
- MinVer strips `v` prefix from tags automatically via `MinVerTagPrefix`

## Development Approach
- **Testing approach**: Regular (verify build and assembly version after change)
- Minimal change: one package add, one property remove, one git tag
- No new abstractions needed
- **CRITICAL: every task MUST include new/updated tests**
- **CRITICAL: all tests must pass before starting next task**

## Implementation Steps

### Task 1: Add MinVer and remove hardcoded version

**Files:**
- Modify: `WhatKey.csproj`

- [x] Add `<PackageReference Include="MinVer" Version="5.0.0" PrivateAssets="All" />` to ItemGroup
- [x] Add `<MinVerTagPrefix>v</MinVerTagPrefix>` to PropertyGroup so `v1.7.0` tag → version `1.7.0`
- [x] Remove `<Version>1.0.0</Version>` from PropertyGroup
- [x] Run `dotnet restore` — must succeed
- [x] Run `dotnet build` — must succeed with 0 errors, 0 warnings
- [x] Verify MinVer reads existing tags: `dotnet msbuild -getProperty:Version` should return something like `1.6.0-alpha.0.N` (commits ahead of v1.6.0 tag)
- [x] Run full test suite: `dotnet test tests/WhatKey.Tests` — must pass before task 2

### Task 2: Tag v1.7.0 to set release version

**Files:**
- No file changes — git operation only

- [x] Create annotated tag: `git tag -a v1.7.0 -m "Release v1.7.0 — update checking feature"`
- [x] Run `dotnet build` again — `dotnet msbuild -getProperty:Version` must return `1.7.0`
- [x] Run full test suite: `dotnet test tests/WhatKey.Tests` — must pass before task 3

### Task 3: Verify acceptance criteria

- [x] Run `dotnet test tests/WhatKey.Tests` — all tests pass (171 passed)
- [x] Build and check: About window shows version 1.7.0 (launch app manually or inspect binary properties) — binary shows 1.7.1-alpha.0.1; correct MinVer behavior (HEAD is 1 commit past v1.7.0 tag); MinVer wiring confirmed working

### Task 4: Update documentation

- [ ] Update CLAUDE.md to document MinVer versioning pattern (tag → version)
- [ ] Move this plan to `docs/plans/completed/`
