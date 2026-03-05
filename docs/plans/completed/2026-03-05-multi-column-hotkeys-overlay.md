# Много-колоночный список горячих клавиш в оверлее

## Overview
Реализовать отображение длинного списка горячих клавиш в 2-3 колонках, чтобы оверлей не становился слишком высоким и не требовал вертикальной прокрутки в типичных сценариях.

## Context
- Files involved: Views/OverlayWindow.xaml, Views/OverlayWindow.xaml.cs, ViewModels/OverlayViewModel.cs, Models/HotkeyEntry.cs, tests/WhatKey.Tests/OverlayLayoutTests.cs
- Related patterns: MVVM с биндингом DataContext в OverlayWindow; логика отображения для оверлея сосредоточена в OverlayViewModel + XAML; текущий UI использует ScrollViewer с MaxHeight=500 и один вертикальный список
- Dependencies: WPF (.NET Framework 4.8), MSTest (tests/WhatKey.Tests), без добавления внешних UI-библиотек

## Development Approach
- **Testing approach**: Regular (code first, then tests)
- Complete each task fully before moving to the next
- Использовать существующий MVVM-подход: вычисляемые свойства/чистая логика во ViewModel (или отдельном helper), минимальная логика в code-behind
- **CRITICAL: every task MUST include new/updated tests**
- **CRITICAL: all tests must pass before starting next task**

## Implementation Steps

### Task 1: Вынести и покрыть тестами правило расчета числа колонок

**Files:**
- Modify: `ViewModels/OverlayViewModel.cs`
- Create: `tests/WhatKey.Tests/OverlayLayoutTests.cs`

- [x] Добавить явную логику определения числа колонок (1/2/3) на основе количества hotkeys и ограничений высоты оверлея
- [x] Сделать логику детерминированной и тестируемой без UI-рантайма (через чистый метод/helper)
- [x] Определить пороги для перехода 1->2->3 колонок, соответствующие UX-цели "без вертикального скролла в обычных длинных списках"
- [x] write tests for this task
- [x] run project test suite - must pass before task 2

### Task 2: Перестроить XAML оверлея на 2-3 колонки вместо одного вертикального списка

**Files:**
- Modify: `Views/OverlayWindow.xaml`

- [x] Заменить текущий одно-колоночный ItemsControl в ScrollViewer на layout с колонками (например, UniformGrid/ItemsControl с панелью и биндингом числа колонок)
- [x] Сохранить текущий визуальный стиль строки hotkey (badge клавиш + описание) и пустое состояние
- [x] Ограничить габариты оверлея так, чтобы вертикальный скролл не был основным сценарием при длинных списках
- [x] write tests for this task
- [x] run project test suite - must pass before task 3

### Task 3: Подключить runtime-обновление компоновки при показе оверлея

**Files:**
- Modify: `Views/OverlayWindow.xaml.cs`
- Modify: `ViewModels/OverlayViewModel.cs`

- [x] При ShowWithHotkeys рассчитывать и применять целевое число колонок с учетом фактического списка hotkeys
- [x] Убедиться, что при смене активного приложения (разное количество hotkeys) layout корректно пересчитывается
- [x] Проверить, что центрирование/анимация/показ оверлея не ломаются после изменений layout
- [x] write tests for this task
- [x] run project test suite - must pass before task 4

### Task 4: Verify acceptance criteria

- [x] manual test: открыть приложение с длинным списком hotkeys и убедиться, что оверлей показывает 2-3 колонки без необходимости вертикальной прокрутки
- [x] manual test: открыть приложение с коротким списком и проверить, что UI остается читаемым (1 колонка)
- [x] manual test: проверить, что пустой список по-прежнему показывает Empty state
- [x] run full test suite (use project-specific command): dotnet test whatkey.sln
- [x] run linter (use project-specific command): dotnet format --verify-no-changes
- [x] verify test coverage meets 80%+

### Task 5: Update documentation

- [x] update README.md if user-facing changes
- [x] update CLAUDE.md if internal patterns changed
- [x] move this plan to `docs/plans/completed/`

## Post-Review Fixes (2026-03-05)
- Синхронизирован лимит высоты списка: `OverlayViewModel.DefaultHotkeysListMaxHeight` теперь соответствует UI-ограничению и используется в XAML через биндинг `HotkeysListMaxHeight`.
- Обновлены тесты `OverlayLayoutTests` для проверки единого источника ограничения высоты между ViewModel и XAML.
- Добавлен учет доступной ширины экрана в расчете числа колонок (`OverlayViewModel.CalculateOverlayColumns`), чтобы не выбирать 3 колонки там, где они не помещаются.
- В `OverlayWindow.ShowWithHotkeys` добавлены ограничение `MaxWidth` по рабочей области монитора и кламп позиции `Left/Top`, чтобы оверлей не выходил за границы экрана.

## Post-Review Fixes (2026-03-05, pass 2)
- Исправлено ограничение ширины в `OverlayViewModel.CalculateOverlayColumns`: результат расчета по `availableWidth/minColumnWidth` теперь дополнительно ограничивается `maxColumns`, чтобы не выходить за контракт 1/2/3 колонок.
- Добавлен тест `CalculateOverlayColumns_WithWideFiniteWidth_StillCapsAtThreeColumns` в `tests/WhatKey.Tests/OverlayLayoutTests.cs`, фиксирующий регрессию с потенциальным выбором 4+ колонок.
