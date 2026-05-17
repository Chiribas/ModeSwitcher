# Delete Mode — Design

**Date:** 2026-05-17
**Status:** Approved (design phase)

## Problem

Once a mode exists in `modeswitcher.json` + `production/modes/<Folder>/`, there's no in-app way to remove it. Users have to manually delete the folder and hand-edit JSON — the same pain that the [Save Current Mode design](2026-05-17-save-current-mode-design.md) solved for the create flow.

## Goals

- One-click removal from the main form, per mode.
- Confirm before destroying anything.
- Deleting the currently-active mode is allowed and does NOT touch `TargetPath`.

## Non-Goals

- Renaming / reordering modes.
- Deleting from the tray menu.
- Recycle-bin / undo (files are gone after delete).
- Wiping `TargetPath` files when the active mode is deleted.

## Approach

Approach A from brainstorming: add `ICodeSwitcher.DeleteModeAsync(string name)` and render a small `×` button next to each radio in `RenderModes`. Rejected:
- **B. Dedicated "Manage modes" dialog** — over-engineered for one action; user wants inline `×`.
- **C. Inline `Directory.Delete` + JSON edit in `MainForm`** — mixes layers, untestable, repeats the mistake we avoided in the save flow.

## UI

In [MainForm.cs RenderModes()](../../../src/ModeSwitcher.UI/MainForm.cs#L92-L138):

- Radio width drops from `pnlModes.ClientSize.Width - 30` to `pnlModes.ClientSize.Width - 60` to leave room for the delete button.
- A `Button` (`Text = "×"`, `Size = 25×25`) is added at `Location = (pnlModes.ClientSize.Width - 35, y)`, `Anchor = Top | Right`.
- The button captures `mode` in its click handler closure.

Click handler:

```csharp
var confirm = MessageBox.Show(this,
    $"Удалить режим \"{mode.Name}\"?",
    "Подтверждение",
    MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
if (confirm != DialogResult.Yes) return;

try
{
    SetStatus($"Удаление режима \"{mode.Name}\"...");
    await _switcher.DeleteModeAsync(mode.Name);
    SetStatus($"Режим \"{mode.Name}\" удалён.");
    LoadData();
}
catch (Exception ex)
{
    SetStatus($"Ошибка: {ex.Message}");
    MessageBox.Show($"Не удалось удалить режим:\n{ex.Message}", "Ошибка",
        MessageBoxButtons.OK, MessageBoxIcon.Error);
}
```

After successful delete, `LoadData()` refreshes the list and the tray menu. If the deleted mode was active, `DetectCurrentMode()` will return null on the next pass (its folder is gone, no remaining mode can match by file hash), and the UI shows "Агент: не выбран".

## Core

In [src/ModeSwitcher.Core/IModeSwitcher.cs](../../../src/ModeSwitcher.Core/IModeSwitcher.cs):

```csharp
Task DeleteModeAsync(string modeName);
```

In `CodeSwitcher.DeleteModeAsync`:

1. `LoadConfig()`. If null → throw `InvalidOperationException("Config could not be loaded.")`.
2. Look up `ModeDefinition` by `Name`. If not found → return silently (idempotent — refreshing the list and clicking a stale entry shouldn't blow up).
3. `modePath = Path.Combine(_modesBasePath, def.Folder)`. If `_fileSystem.DirectoryExists(modePath)` → `_fileSystem.DeleteDirectory(modePath, recursive: true)`.
4. Remove the entry from `config.Modes`.
5. `_configWriter.Save(_configPath, config)`.
6. `Reload()`.

`TargetPath` is intentionally left alone. Even if the user deletes the currently-active mode, their working `.claude` directory is preserved. They can re-save it later via "Сохранить текущий…".

## Testing

`CodeSwitcherTests` (NSubstitute + FluentAssertions, mirroring existing patterns):

- `DeleteModeAsync_ExistingMode_RemovesFolderAndUpdatesConfig`
  - Mock returns a config with two modes; mock `DirectoryExists` true for the target folder.
  - Verify `DeleteDirectory(<modesBase>/<folder>, true)` called once.
  - Verify saved JSON has only the other mode left.
- `DeleteModeAsync_NonExistentMode_DoesNothing`
  - Config has one mode "A"; call `DeleteModeAsync("Ghost")`.
  - Verify `DeleteDirectory` NOT called and config NOT rewritten (`OpenWrite` not called).
- `DeleteModeAsync_FolderMissingOnDisk_StillUpdatesConfig`
  - Config has mode "A"; mock `DirectoryExists(...A folder)` returns false.
  - Verify `DeleteDirectory` NOT called but config IS rewritten without "A".
- `DeleteModeAsync_InvalidatesConfigCache`
  - After delete, next `GetModes()` triggers another `OpenRead`.

UI is not unit-tested (consistent with existing code).

## Out of Scope

- Rename mode.
- Reorder modes.
- Bulk delete.
- "Are you sure?" with typed-name confirmation.
- Undo / recycle-bin.
