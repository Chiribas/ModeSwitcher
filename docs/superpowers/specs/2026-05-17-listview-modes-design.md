# ListView Modes with Checkbox and Delete Icon — Design

**Date:** 2026-05-17
**Status:** Approved (design phase)

## Problem

Current mode list uses `RadioButton` controls with a separate `×` button per row. This has UX issues:
- The `×` button is small (25×25) and awkward to click.
- Adding a delete action per row manually requires awkward alignment code (`modeForClosure`, magic numbers for positioning).
- The list doesn't scale well visually when modes grow beyond ~5.

## Goals

Replace the `Panel` + `RadioButton` + `×` approach with a single `ListView` control that:
- Shows two columns: Mode name (left) + Delete icon (right).
- Displays active state via a checkbox in the first column.
- Click on the delete icon column triggers deletion with confirmation.
- Checkboxes are read-only (indicator only); row click = selection only.

## Non-Goals

- Drag-and-drop reordering of modes.
- Multi-selection (SingleSelect = false).
- Context menu (deletion is direct via icon).
- Edit mode name inline.

## Architecture

**Approach A:** `ListView` with two columns, `ImageList` for icons, `CheckBoxes = true`, `ColumnClick` handler for deletion.

Rejected:
- **B. DataGridView** — overhead for just two columns; ListView is simpler for pure list + icon.
- **C. Custom CheckedListBox renderer** — more code, higher risk.

## UI Design

### ListView Setup

In [MainForm.Designer.cs](src/ModeSwitcher.UI/MainForm.Designer.cs):

- Remove `pnlModes` (Panel and its controls).
- Add `ListView lvModes`:
  - `Location = (20, 60)`
  - `Size = (360, 150)` — same as old panel.
  - `Anchor = Top | Bottom | Left | Right` — resizable form support.
  - `CheckBoxes = true` — checkboxes shown in first column.
  - `MultiSelect = false` — single row selection.
  - `View = View.Details` — required for columns.
  - `FullRowSelect = true` — click anywhere in row selects it.
  - `GridLines = true` (optional, for visual separation).
  - `HideSelection = false` — show selection even when not focused.
  - `ColumnClick += LvModes_ColumnClick` — handle delete column clicks.
- Add `ImageList imgList` (SmallImageList):
  - `ColorDepth = ColorDepth.Depth32Bit`
  - `ImageSize = 16×16`
  - `TransparentColor = Color.Transparent`
  - `lvModes.SmallImageList = imgList`

**Columns:**
- Column 0 — "Режим", Width = `ClientSize.Width - 40` (leaves space for Column 1), TextAlign = Left.
- Column 1 — "", Width = 40, TextAlign = Center.

**ImageList icons (in constructor or after InitializeComponent):**
```csharp
imgList.Images.Clear();
imgList.Images.Add(SystemIcons.Application);    // 0: unchecked
imgList.Images.Add(SystemIcons.Information);     // 1: checked
imgList.Images.Add(SystemIcons.Question);        // 2: delete icon (trash)
```
(Note: SystemIcons choices can be adjusted; the key is 3 distinct icons at indices 0, 1, 2.)

### Data Rendering

New method `RenderModesToList()` replaces `RenderModes()`:

```csharp
private void RenderModesToList()
{
    lvModes!.Items.Clear();

    if (_modes is null || _modes.Count == 0)
    {
        var emptyItem = new ListViewItem("Нет доступных режимов")
        {
            ForeColor = Color.Gray,
            Enabled = false,
            ImageIndex = -1
        };
        lvModes.Items.Add(emptyItem);
        return;
    }

    var currentMode = _switcher.DetectCurrentMode();
    foreach (var mode in _modes)
    {
        var displayName = GetDisplayName(mode.Name);
        var checkedIcon = (currentMode?.ModeName == mode.Name) ? 1 : 0;
        var trashIcon = 2;

        // ListViewItem with 2 subitems; ImageIndex sets the checkbox icon
        var item = new ListViewItem(new[] { displayName, "" }, checkedIcon)
        {
            Tag = mode
        };

        // Set delete icon in second column via SubItem.ImageIndex
        item.SubItems[1].ImageIndex = trashIcon;

        // Checkboxes are read-only; revert any manual changes
        item.CheckedChanged += (s, e) =>
        {
            var sender = s as ListViewItem;
            if (sender is not null)
            {
                sender.Checked = (sender.Tag as ModeInfo)?.IsActive == true;
            }
        };

        lvModes.Items.Add(item);
    }
}
```

`LoadData()` calls `RenderModesToList()` instead of `RenderModes()`.

### Delete Handling

```csharp
private void LvModes_ColumnClick(object? sender, ColumnClickEventArgs e)
{
    // Column 1 is the delete column (index 1, since Column 0 is name)
    if (e.Column == 1 && lvModes!.SelectedItems.Count > 0)
    {
        var item = lvModes.SelectedItems[0];
        var mode = item.Tag as ModeInfo;
        if (mode is not null)
        {
            HandleDeleteModeAsync(mode);
        }
    }
}

private async Task HandleDeleteModeAsync(ModeInfo mode)
{
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
}
```

Note: `HandleDeleteModeAsync` can be merged with the existing `DeleteModeAsync(ModeInfo)` handler from the previous delete-mode feature, reusing its logic.

## Testing

Manual smoke test (no unit tests for WinForms UI):

1. Build and run `production/ModeSwitcher.UI.exe`.
2. Verify ListView shows modes with checkboxes and delete icons.
3. Verify active mode has checked checkbox and checked icon; inactive has unchecked.
4. Clicking a checkbox manually — it reverts back (read-only).
5. Clicking delete icon — YesNo dialog appears.
6. Delete non-active mode → folder deleted + config updated → list refreshed.
7. Delete active mode → allowed, folder deleted, after delete "Агент: не выбран" shows.
8. "Отмена" in dialog — nothing changes.
9. Window resize — ListView grows, columns proportionally, delete icon stays at right.
10. Restart app — deleted modes stay gone.

## Files Changed

- `src/ModeSwitcher.UI/MainForm.Designer.cs` — remove `pnlModes`, add `lvModes` + `imgList`.
- `src/ModeSwitcher.UI/MainForm.cs` — replace `RenderModes()` with `RenderModesToList()`, add `LvModes_ColumnClick`, integrate delete handler.

No Core changes needed.