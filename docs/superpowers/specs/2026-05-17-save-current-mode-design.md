# Save Current Mode & Scrollable Mode List — Design

**Date:** 2026-05-17
**Status:** Approved (design phase)

## Problem

Two pain points in the current ModeSwitcher UI:

1. **Mode list overflow.** `pnlModes` in [MainForm.Designer.cs:49-52](../../../src/ModeSwitcher.UI/MainForm.Designer.cs#L49-L52) has a fixed `Size(360, 150)` with no scroll. With 5+ modes, radio buttons render outside the panel and are unreachable.
2. **No way to capture the current state as a new mode.** Today, to add a mode you must manually create `production/modes/<Folder>/`, copy `settings.json` into it, and hand-edit `modeswitcher.json`. There's no in-app flow.

## Goals

- Mode list scrolls and survives window resizing.
- One-click "Save current mode" from the main form: pick files, name it, commit.
- Reasonable defaults so the user almost never has to type a name from scratch.

## Non-Goals

- Editing / deleting / renaming existing modes (separate future work).
- Saving from tray menu (form-only for v1).
- Migrating to a different config format.

## High-Level Approach

**Approach A — Minimal UI upgrade + new `ModeSaver` service in Core.**
Two new buttons-worth of UI plus a modal save dialog. Core gets a `ModeSaver` service (copies selected files into a new mode folder), a `ConfigWriter` (writes `SwitcherConfig` back to JSON), and a `ModeNameSuggester` (extracts default name from `settings.json`). `ICodeSwitcher` gains `SaveCurrentAsModeAsync` and `Reload`.

Rejected alternatives:
- **B. Full refactor into `IModeManager` (CRUD).** Cleaner long-term but oversized for one feature; revisit when edit/delete is added.
- **C. Inline shortcut in the Form using `OpenFileDialog` + manual JSON edit.** Mixes UI with file-system logic, untestable, and would still need to be redone when edit/delete arrives.

## UI Changes

### 1. Scrollable mode list + resizable window

In [MainForm.Designer.cs](../../../src/ModeSwitcher.UI/MainForm.Designer.cs) and [MainForm.cs](../../../src/ModeSwitcher.UI/MainForm.cs):

- `FormBorderStyle = Sizable`, `MaximizeBox = true`.
- `pnlModes.AutoScroll = true`. Scrollbar appears only when content overflows — kept implicit per user preference ("сделаю форму ресайзабл а скролл пусть будет всегда — так проще"; AutoScroll + resizable form is the simpler path).
- Anchors:
  - `lblCurrentMode` — `Top | Left`
  - `pnlModes` — `Top | Bottom | Left | Right`
  - `btnApply`, `btnSaveCurrent` — `Bottom | Left`
  - `btnRefresh`, `btnAbout`, `btnExit` — `Bottom | Left` (or `Right` for `btnExit` if preferred)
  - `statusStrip` — anchored to bottom automatically
- In `RenderModes()` ([MainForm.cs:92-138](../../../src/ModeSwitcher.UI/MainForm.cs#L92-L138)), radio width uses `pnlModes.ClientSize.Width - 20` so scrollbar (when shown) doesn't clip the labels.

### 2. New "Сохранить текущий…" button

Added to the main form's bottom button row, between `btnApply` and `btnRefresh`. Layout becomes:

```
┌──────────────────────────────────────────┐
│ Агент: Жанклод ✓                         │
│ ┌──────────────────────────────────────┐ │
│ │ ◉ Жанклод (активен)                  │ │
│ │ ○ Zidan                              │ │
│ │ ○ Qwen 14b                           │ │
│ │   ...                              [▲]│ │
│ │                                    [▼]│ │
│ └──────────────────────────────────────┘ │
│ [Применить] [Сохранить текущий…]         │
│ [Обновить] [?] [Выход]                   │
│ Готово                                   │
└──────────────────────────────────────────┘
```

`btnSaveCurrent.Click` opens `SaveCurrentModeDialog`. On `DialogResult.OK`, calls `_switcher.SaveCurrentAsModeAsync(...)` then `LoadData()` to refresh.

### 3. `SaveCurrentModeDialog`

New form at [src/ModeSwitcher.UI/SaveCurrentModeDialog.cs](../../../src/ModeSwitcher.UI/SaveCurrentModeDialog.cs).

Layout:

```
┌──────────────────────────────────────────┐
│ Активный мод: Жанклод                    │
│                                          │
│ Имя:    [localhost (qwen2.5-coder:14b)]  │
│ Папка:  [localhost_qwen2.5-coder_14b]    │
│                                          │
│ Файлы:                                   │
│ ┌──────────────────────────────────────┐ │
│ │ ☑ settings.json                      │ │
│ │ ☑ agents/zidan.md                    │ │
│ │ ☐ CLAUDE.md            (новый)       │ │
│ │ ☐ keybindings.json     (новый)       │ │
│ └──────────────────────────────────────┘ │
│ ✓ — уже входит в активный мод            │
│                                          │
│             [Сохранить]  [Отмена]        │
└──────────────────────────────────────────┘
```

Inputs:
- Constructed with `SaveCandidates` (from `ModeSaver`), suggested name/folder (from `ModeNameSuggester`), and `currentModeName` (string, may be null).
- `txtName` and `txtFolder` are editable. `txtFolder` auto-updates from `txtName` (via `ToFolderName`) **until** the user manually edits `txtFolder` — tracked with `_folderManuallyEdited` flag.
- File list = `CheckedListBox`. Files from current mode listed first, checked. New top-level files in `TargetPath` listed below, unchecked, with " (новый)" suffix.

Validation before `OK`:
- `txtName` non-empty
- `txtFolder` non-empty and contains only path-safe characters (regex: `^[A-Za-z0-9._\-]+$`)
- At least one file checked
- On failure: show inline error label and keep dialog open

`OK` returns selected `(name, folder, relativePaths[])` via public properties.

## Core Changes

### `ConfigWriter`

New service at [src/ModeSwitcher.Core/Services/ConfigWriter.cs](../../../src/ModeSwitcher.Core/Services/ConfigWriter.cs):

```csharp
public class ConfigWriter(IFileSystem fs) {
    public void Save(string configPath, SwitcherConfig config);
}
```

Uses `JsonSerializer` with `WriteIndented = true`. Writes via a stream from `_fileSystem.OpenWrite(configPath)`.

### `ModeSaver`

New service at [src/ModeSwitcher.Core/Services/ModeSaver.cs](../../../src/ModeSwitcher.Core/Services/ModeSaver.cs):

```csharp
public record FileCandidate(string RelativePath, bool InCurrentMode);
public record SaveCandidates(List<FileCandidate> Files);

public class ModeSaver(IFileSystem fs) {
    public SaveCandidates GetCandidates(string targetPath, string? currentModePath);
    public Task SaveAsync(string targetPath, string newModePath, IEnumerable<string> relativePaths);
}
```

`GetCandidates`:
- If `currentModePath` is non-null and exists: recursively list its files → relative paths → `InCurrentMode = true`.
- List top-level files of `targetPath` (NOT recursive) that are not already in the current-mode set → `InCurrentMode = false`.
- Combine, sort: current-mode files first (alphabetical), then new files (alphabetical).

`SaveAsync`:
- Create `newModePath` if missing.
- For each relative path: compute source = `targetPath/<rel>`, dest = `newModePath/<rel>`, ensure parent dir exists, copy, preserve `LastWriteTime` (same as [FileCopier.cs:33-48](../../../src/ModeSwitcher.Core/Services/FileCopier.cs#L33-L48)).
- Wrapped in `Task.Run`.

### `ModeNameSuggester`

New static class at [src/ModeSwitcher.Core/Services/ModeNameSuggester.cs](../../../src/ModeSwitcher.Core/Services/ModeNameSuggester.cs):

```csharp
public static class ModeNameSuggester {
    public static string? SuggestFromSettings(string settingsJsonPath, IFileSystem fs);
    public static string ToFolderName(string displayName);
}
```

`SuggestFromSettings`:
- Read & parse `settings.json` as `JsonDocument`.
- Extract `env.ANTHROPIC_BASE_URL` → parse as `Uri` → `Host` (no port).
- Extract `env.model` (fallback to top-level `model`).
- Return `"{host} ({model})"` if both available, else null.
- All failure modes (missing file, malformed JSON, missing fields) → null, no exceptions thrown.

`ToFolderName`:
- Replace any character outside `[A-Za-z0-9._\-]` with `_`.
- Collapse runs of `_` into one.
- Trim leading/trailing `_`.
- If the result is empty (e.g. name was all Cyrillic), return empty string — the dialog will fail validation and the user must type the folder manually. (Auto-generated names from `settings.json` are ASCII via `Uri.Host`, so this case only arises if the user manually enters a fully non-ASCII display name.)

### `ICodeSwitcher` additions

In [src/ModeSwitcher.Core/IModeSwitcher.cs](../../../src/ModeSwitcher.Core/IModeSwitcher.cs):

```csharp
Task SaveCurrentAsModeAsync(string modeName, string folderName, IEnumerable<string> relativePaths, bool overwrite);
void Reload();
```

`SaveCurrentAsModeAsync` in `CodeSwitcher`:
1. Load config.
2. Compute `newModePath = Path.Combine(_modesBasePath, folderName)`.
3. Call `_modeSaver.SaveAsync(config.TargetPath, newModePath, relativePaths)`.
4. If a mode with this `Name` already exists in `config.Modes`, update its `Folder`; otherwise append new `ModeDefinition`.
5. `_configWriter.Save(_configPath, config)`.
6. `Reload()` (invalidates `_cachedConfig`).

`Reload`: `_cachedConfig = null`.

`CodeSwitcher` constructor gains optional `ModeSaver` and `ConfigWriter` parameters (defaulted, mirroring existing pattern at [ModeSwitcher.cs:28-40](../../../src/ModeSwitcher.Core/ModeSwitcher.cs#L28-L40)).

### `IFileSystem` additions

Already present in [IFileSystem.cs](../../../src/ModeSwitcher.Core/FileSystem/IFileSystem.cs): `DeleteDirectory`, `GetAllFiles` (supports `SearchOption.TopDirectoryOnly` for the new-files scan), `OpenRead`, `CopyFile`, `Get/SetLastWriteTime`.

Add:
- `Stream OpenWrite(string path)` — for `ConfigWriter`.

## Save Flow End-to-End

User clicks "Сохранить текущий…":

1. `MainForm` builds candidates: `_modeSaver.GetCandidates(config.TargetPath, currentModePath)`.
   - If no active mode → `currentModePath = null`, only top-level new files shown, `settings.json` pre-checked if present.
2. `MainForm` computes suggested name: locate `settings.json` in target, run `ModeNameSuggester.SuggestFromSettings(...)`.
3. `MainForm` opens `SaveCurrentModeDialog(candidates, suggestedName, currentModeDisplayName, existingNames, existingFolders)`.
4. User adjusts checkboxes, name, folder. Clicks "Сохранить".
5. **Conflict check happens inside the dialog's OK handler** (before `DialogResult.OK` is set):
   - If `existingFolders` contains `folder` OR `existingNames` contains `name` → `MessageBox.Show("...", YesNoCancel)`:
     - **Yes** (overwrite): set `OverwriteRequested = true` and close dialog with `OK`.
     - **No** (rename): focus `txtName`, leave dialog open.
     - **Cancel**: close dialog with `Cancel`.
   - No conflict → close with `OK`.
6. Main form, on `OK`: calls `_switcher.SaveCurrentAsModeAsync(name, folder, selectedRelPaths, overwrite)`. When `overwrite == true`, `ModeSaver` deletes the existing folder recursively before copying; `CodeSwitcher` updates the matching `ModeDefinition` in place instead of appending.
7. On success: `LoadData()` refreshes list + tray menu; status bar shows confirmation.
8. On error: `MessageBox` + status bar; partial folder NOT auto-cleaned (user decides).

## Error Handling

- All Core operations wrapped in try/catch at the UI boundary (consistent with existing `BtnApply_Click` at [MainForm.cs:210-248](../../../src/ModeSwitcher.UI/MainForm.cs#L210-L248)).
- `ConfigWriter` writes via temp file + rename to avoid corrupting `modeswitcher.json` on crash mid-write.
- `ModeNameSuggester` swallows all exceptions and returns null — defaults are best-effort.

## Testing

Unit tests (xUnit, against `FakeFileSystem` — same harness as existing tests):
- `ModeSaverTests`:
  - `GetCandidates_WithCurrentMode_ReturnsCurrentFilesCheckedPlusNewTopLevelFiles`
  - `GetCandidates_NoCurrentMode_ReturnsOnlyTopLevelFiles`
  - `GetCandidates_DeduplicatesFilesPresentInBoth`
  - `SaveAsync_CopiesSelectedFilesPreservingTimestamps`
  - `SaveAsync_CreatesNestedDirectories`
- `ConfigWriterTests`:
  - `Save_RoundTripsConfig`
  - `Save_WritesIndentedJson`
- `ModeNameSuggesterTests`:
  - `SuggestFromSettings_ExtractsHostAndModel`
  - `SuggestFromSettings_MissingFile_ReturnsNull`
  - `SuggestFromSettings_MalformedJson_ReturnsNull`
  - `SuggestFromSettings_NoBaseUrl_ReturnsNull`
  - `ToFolderName_StripsInvalidChars`
  - `ToFolderName_CollapsesUnderscores`
- `CodeSwitcherTests` extensions:
  - `SaveCurrentAsModeAsync_AddsNewModeToConfig`
  - `SaveCurrentAsModeAsync_WithOverwriteTrue_ReplacesExistingFolderAndUpdatesDefinition`
  - `SaveCurrentAsModeAsync_WithOverwriteFalse_AppendsNewDefinition`
  - `Reload_InvalidatesCache`

UI (`SaveCurrentModeDialog`, `MainForm`) is not unit-tested, consistent with current project conventions.

## Out of Scope

- Editing existing modes from UI.
- Deleting modes from UI.
- Re-detecting current mode after save (already handled by `LoadData()`).
- Localization of new strings (Russian inline, matching existing UI).
