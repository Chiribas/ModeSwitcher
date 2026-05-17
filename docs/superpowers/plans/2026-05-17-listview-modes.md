# ListView Modes with Checkbox and Delete Icon — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the `RadioButton` + `×` button mode list with a `ListView` that shows modes in two columns (name + delete icon) with read-only checkboxes.

**Architecture:** `ListView` with `View.Details`, two columns, `CheckBoxes = true`, `SmallImageList` for checkbox and delete icons. Deletion via `ColumnClick` on the delete column. No Core changes.

**Tech Stack:** .NET 8 WinForms, `SystemIcons` for built-in icons, manual smoke testing.

**Repo notes:**
- Build: `dotnet build src/ModeSwitcher.sln`
- User does NOT want commits from the agent during execution; skip the `git commit` step at the end of each task. The user will commit themselves later. Each task still ends with a "Verify" step in place of "Commit".

---

## Task 1: Replace Panel with ListView in Designer

**Files:**
- Modify: `src/ModeSwitcher.UI/MainForm.Designer.cs`

- [ ] **Step 1.1: Remove `pnlModes` and its controls**

In `src/ModeSwitcher.UI/MainForm.Designer.cs`, delete these fields (lines ~7-9):
```csharp
private Panel? pnlModes;
```

In `InitializeComponent()`, delete these blocks (lines ~48-52 panel initialization and line ~110 adding pnlModes to Controls).

- [ ] **Step 1.2: Add ListView and ImageList fields**

At the top of the class (where `lblCurrentMode` is), add:
```csharp
private ListView? lvModes;
private ImageList? imgList;
```

- [ ] **Step 1.3: Initialize ImageList**

In `InitializeComponent()`, after `this.statusStrip.SuspendLayout()`, add:

```csharp
this.imgList = new ImageList(this.components);
this.imgList.ColorDepth = ColorDepth.Depth32Bit;
this.imgList.ImageSize = new Size(16, 16);
this.imgList.TransparentColor = Color.Transparent;
```

- [ ] **Step 1.4: Initialize ListView**

In `InitializeComponent()`, after the `imgList` block, add:

```csharp
this.lvModes = new ListView();
this.lvModes.Location = new Point(20, 60);
this.lvModes.Name = "lvModes";
this.lvModes.Size = new Size(360, 150);
this.lvModes.CheckBoxes = true;
this.lvModes.FullRowSelect = true;
this.lvModes.GridLines = true;
this.lvModes.HideSelection = false;
this.lvModes.MultiSelect = false;
this.lvModes.View = View.Details;
this.lvModes.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
this.lvModes.SmallImageList = this.imgList;
this.lvModes.ColumnClick += new ColumnClickEventHandler(this.LvModes_ColumnClick);

// Columns
this.lvModes.Columns.Add(new ColumnHeader { Text = "Режим", Width = this.lvModes.ClientSize.Width - 40, TextAlign = HorizontalAlignment.Left });
this.lvModes.Columns.Add(new ColumnHeader { Text = "", Width = 40, TextAlign = HorizontalAlignment.Center });
```

- [ ] **Step 1.5: Replace pnlModes with lvModes in Controls**

In `InitializeComponent()`, replace:
```csharp
this.Controls.Add(this.pnlModes);
```
with:
```csharp
this.Controls.Add(this.lvModes);
```

- [ ] **Step 1.6: Update Form.ClientSize to accommodate column widths**

Change:
```csharp
this.ClientSize = new Size(400, 350);
```
to:
```csharp
this.ClientSize = new Size(440, 350);  // +40 for delete column
```

- [ ] **Step 1.7: Build**

```bash
dotnet build src/ModeSwitcher.sln --nologo --verbosity quiet
```

Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 1.8: Verify (no commit)**

---

## Task 2: Populate ImageList with icons

**Files:**
- Modify: `src/ModeSwitcher.UI/MainForm.cs`

- [ ] **Step 2.1: Add ImageList initialization in constructor**

In `MainForm` constructor, after `InitializeComponent()` and before `LoadData()`, add:

```csharp
// Populate ImageList with icons
imgList!.Images.Clear();
imgList.Images.Add(SystemIcons.Application);    // 0: unchecked
imgList.Images.Add(SystemIcons.Information);     // 1: checked
imgList.Images.Add(SystemIcons.Question);        // 2: delete icon
```

- [ ] **Step 2.2: Build**

```bash
dotnet build src/ModeSwitcher.sln --nologo --verbosity quiet
```

Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 2.3: Verify (no commit)**

---

## Task 3: Replace RenderModes with RenderModesToList

**Files:**
- Modify: `src/ModeSwitcher.UI/MainForm.cs`

- [ ] **Step 3.1: Replace RenderModes() method entirely**

Find `private void RenderModes()` (around lines 94-155) and replace the entire method with:

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
        var checkedIcon = (currentMode?.ModeName == mode.Name) ? 1 : 0; // 1 = checked, 0 = unchecked
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

- [ ] **Step 3.2: Update LoadData() to call RenderModesToList()**

In `LoadData()` (around line 72), change:
```csharp
RenderModes();
```
to:
```csharp
RenderModesToList();
```

- [ ] **Step 3.3: Build**

```bash
dotnet build src/ModeSwitcher.sln --nologo --verbosity quiet
```

Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 3.4: Verify (no commit)**

---

## Task 4: Add delete handler and column click handler

**Files:**
- Modify: `src/ModeSwitcher.UI/MainForm.cs`

- [ ] **Step 4.1: Add column click handler**

In `MainForm`, add this method after `RenderModesToList()` (before `UpdateTrayMenu`):

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
```

- [ ] **Step 4.2: Add/delete handler method (reuses existing pattern)**

Note: There may already be a `DeleteModeAsync(ModeInfo mode)` method from the previous delete-mode feature. If it exists, reuse it by calling it from `LvModes_ColumnClick`. If not, add this method:

```csharp
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

If `DeleteModeAsync(ModeInfo mode)` already exists, replace its body with `HandleDeleteModeAsync` and update all callers to use the new method name, or keep the existing and call it directly from `LvModes_ColumnClick`.

- [ ] **Step 4.3: Build**

```bash
dotnet build src/ModeSwitcher.sln --nologo --verbosity quiet
```

Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 4.4: Verify (no commit)**

---

## Task 5: Manual smoke test (no commits)

- [ ] **Step 5.1: Publish and run**

Close any running `production/ModeSwitcher.UI.exe`, then:

```bash
dotnet publish src/ModeSwitcher.UI/ModeSwitcher.UI.csproj -c Release -o production --nologo --verbosity quiet
```

Run `production/ModeSwitcher.UI.exe`.

- [ ] **Step 5.2: Verify ListView displays correctly**

1. Modes appear in a list with two columns.
2. Active mode has a checked checkbox; inactive modes have unchecked.
3. Delete icon (Question/SystemIcons icon) appears in the right column.

- [ ] **Step 5.3: Verify read-only checkboxes**

Click a checkbox manually — it should revert back to its original state immediately (read-only indicator).

- [ ] **Step 5.4: Verify row selection**

Click anywhere in a row (name or delete column) — the row becomes selected. Only one row can be selected at a time.

- [ ] **Step 5.5: Verify delete non-active mode**

Click the delete icon on a non-active mode → YesNo dialog appears. Click Yes → mode is deleted (list refreshes, config updated). Click No → nothing changes.

- [ ] **Step 5.6: Verify delete active mode**

Click the delete icon on the active mode → YesNo dialog appears. Click Yes → mode is deleted, "Агент: не выбран" appears.

- [ ] **Step 5.7: Verify resize**

Drag window to resize — ListView grows/shrinks, columns proportionally adjust, delete icon stays at the right edge.

- [ ] **Step 5.8: Verify persistence**

Restart the app — deleted modes stay gone; active state persists across restarts.

- [ ] **Step 5.9: Final verification (no commit)**

---

## Summary of files touched

**Modified:**
- `src/ModeSwitcher.UI/MainForm.Designer.cs` — remove `pnlModes`, add `lvModes` + `imgList`, setup columns, wire `ColumnClick`.
- `src/ModeSwitcher.UI/MainForm.cs` — populate `imgList`, replace `RenderModes()` with `RenderModesToList()`, add column click handler, integrate/delete handler.

No new files created; no Core changes.