# DataGridView Modes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Заменить `ListView` режимов на `DataGridView` с нативной кнопкой "Удалить" и подсветкой активного режима жирным+зелёным.

**Architecture:** Поле `lvModes` (ListView) + `imgList` (ImageList) в [MainForm.Designer.cs](src/ModeSwitcher.UI/MainForm.Designer.cs) заменяется на `dgvModes` (DataGridView) с двумя колонками — `colMode` (TextBox, Fill) и `colDelete` (ButtonColumn, 90px). В [MainForm.cs](src/ModeSwitcher.UI/MainForm.cs) переписывается рендер и обработчики событий: `RenderModesToList` → `RenderModesToGrid(CurrentModeResult?)`, `LvModes_*` → `DgvModes_*`.

**Tech Stack:** .NET 10 WinForms, `System.Windows.Forms.DataGridView`.

**Commits:** Юзер коммитит сам в конце — шагов `git commit` в плане нет.

**Reference:** [docs/superpowers/specs/2026-05-17-datagridview-modes-design.md](../specs/2026-05-17-datagridview-modes-design.md)

---

## File Structure

- **Modify** `src/ModeSwitcher.UI/MainForm.Designer.cs` — заменить `lvModes`+`imgList` на `dgvModes` + колонки.
- **Modify** `src/ModeSwitcher.UI/MainForm.cs` — заменить рендер и обработчики.

Core-проект не трогаем.

---

## Task 1: Designer — заменить ListView на DataGridView

**Files:**
- Modify: `src/ModeSwitcher.UI/MainForm.Designer.cs`

- [ ] **Step 1: Удалить поле `imgList`**

В [src/ModeSwitcher.UI/MainForm.Designer.cs:9](src/ModeSwitcher.UI/MainForm.Designer.cs#L9) удалить строку:

```csharp
private ImageList? imgList;
```

- [ ] **Step 2: Переименовать поле `lvModes` → `dgvModes` и сменить тип**

[MainForm.Designer.cs:8](src/ModeSwitcher.UI/MainForm.Designer.cs#L8):

```csharp
private DataGridView? dgvModes;
```

- [ ] **Step 3: Удалить инициализацию `imgList` в InitializeComponent**

Удалить блок:

```csharp
this.imgList = new ImageList(this.components);
```

И блок настройки `imgList` (ColorDepth, ImageSize, TransparentColor).

- [ ] **Step 4: Заменить инициализацию `lvModes` на `dgvModes`**

Удалить весь блок `// lvModes` (строки 57-75 в текущем файле) и заменить на:

```csharp
// dgvModes
this.dgvModes = new DataGridView();
this.dgvModes.Name = "dgvModes";
this.dgvModes.Location = new Point(20, 60);
this.dgvModes.Size = new Size(360, 150);
this.dgvModes.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
this.dgvModes.AllowUserToAddRows = false;
this.dgvModes.AllowUserToDeleteRows = false;
this.dgvModes.AllowUserToResizeRows = false;
this.dgvModes.MultiSelect = false;
this.dgvModes.RowHeadersVisible = false;
this.dgvModes.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
this.dgvModes.ReadOnly = true;
this.dgvModes.EditMode = DataGridViewEditMode.EditProgrammatically;
this.dgvModes.AutoGenerateColumns = false;
this.dgvModes.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
this.dgvModes.BackgroundColor = SystemColors.Window;
this.dgvModes.BorderStyle = BorderStyle.FixedSingle;

var colMode = new DataGridViewTextBoxColumn
{
    Name = "colMode",
    HeaderText = "Режим",
    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
    ReadOnly = true,
    SortMode = DataGridViewColumnSortMode.NotSortable
};
var colDelete = new DataGridViewButtonColumn
{
    Name = "colDelete",
    HeaderText = "",
    Width = 90,
    AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
    Resizable = DataGridViewTriState.False,
    SortMode = DataGridViewColumnSortMode.NotSortable,
    Text = "Удалить",
    UseColumnTextForButtonValue = true,
    FlatStyle = FlatStyle.System
};
this.dgvModes.Columns.Add(colMode);
this.dgvModes.Columns.Add(colDelete);

this.dgvModes.CellContentClick += new DataGridViewCellEventHandler(this.DgvModes_CellContentClick);
this.dgvModes.SelectionChanged += new EventHandler(this.DgvModes_SelectionChanged);
```

- [ ] **Step 5: Заменить добавление в Controls**

В блоке `MainForm` (около строки 148):

```csharp
this.Controls.Add(this.dgvModes);
```

вместо `this.Controls.Add(this.lvModes)`.

- [ ] **Step 6: Проверить компиляцию**

Команда:
```bash
dotnet build src/ModeSwitcher.UI/ModeSwitcher.UI.csproj -c Release -nologo 2>&1 | grep -E "(error|Build succeeded|Build FAILED)" | head -20
```

Expected: ошибки про отсутствующие `DgvModes_CellContentClick`, `DgvModes_SelectionChanged`, `lvModes` и `imgList` (в MainForm.cs — их починим в Task 2). На этом этапе билд НЕ должен пройти — это нормально, продолжаем.

---

## Task 2: MainForm — заменить обработчики и рендер

**Files:**
- Modify: `src/ModeSwitcher.UI/MainForm.cs`

- [ ] **Step 1: Удалить инициализацию imgList из конструктора**

В [MainForm.cs:21-25](src/ModeSwitcher.UI/MainForm.cs#L21-L25) удалить блок:

```csharp
// Populate ImageList with icons
imgList!.Images.Clear();
imgList.Images.Add(SystemIcons.Application);    // 0: unchecked
imgList.Images.Add(SystemIcons.Information);     // 1: checked
imgList.Images.Add(SystemIcons.Question);        // 2: delete icon
```

Конструктор должен стать таким:

```csharp
public MainForm(ICodeSwitcher switcher)
{
    _switcher = switcher;
    InitializeComponent();
    LoadData();
}
```

- [ ] **Step 2: Изменить вызов рендера в LoadData**

В [MainForm.cs:80](src/ModeSwitcher.UI/MainForm.cs#L80) строку:

```csharp
RenderModesToList();
```

заменить на:

```csharp
RenderModesToGrid(currentMode);
```

- [ ] **Step 3: Заменить RenderModesToList на RenderModesToGrid**

Заменить весь метод `RenderModesToList()` ([MainForm.cs:102-129](src/ModeSwitcher.UI/MainForm.cs#L102-L129)) на:

```csharp
private void RenderModesToGrid(CurrentModeResult? currentMode)
{
    dgvModes!.Rows.Clear();

    if (_modes is null || _modes.Count == 0)
    {
        var emptyIndex = dgvModes.Rows.Add("Нет доступных режимов", string.Empty);
        var emptyRow = dgvModes.Rows[emptyIndex];
        emptyRow.DefaultCellStyle.ForeColor = Color.Gray;
        emptyRow.Tag = null;
        emptyRow.Cells[DELETE_COLUMN_INDEX] = new DataGridViewTextBoxCell { Value = string.Empty };
        return;
    }

    var currentModeName = currentMode?.ModeName;

    foreach (var mode in _modes)
    {
        var displayName = GetDisplayName(mode.Name);
        var isActive = currentModeName == mode.Name;
        var label = isActive ? $"{displayName} (активен)" : displayName;

        var rowIndex = dgvModes.Rows.Add(label, "Удалить");
        var row = dgvModes.Rows[rowIndex];
        row.Tag = mode;

        if (isActive)
        {
            row.DefaultCellStyle.Font = new Font(dgvModes.Font, FontStyle.Bold);
            row.DefaultCellStyle.BackColor = Color.FromArgb(220, 245, 220);
            row.DefaultCellStyle.ForeColor = Color.FromArgb(0, 100, 0);
            row.DefaultCellStyle.SelectionBackColor = Color.FromArgb(180, 230, 180);
            row.DefaultCellStyle.SelectionForeColor = Color.FromArgb(0, 60, 0);
        }
    }

    if (dgvModes.Rows.Count > 0)
    {
        dgvModes.ClearSelection();
    }
}
```

- [ ] **Step 4: Заменить LvModes_SelectedIndexChanged на DgvModes_SelectionChanged**

Заменить метод `LvModes_SelectedIndexChanged` ([MainForm.cs:131-137](src/ModeSwitcher.UI/MainForm.cs#L131-L137)) на:

```csharp
private void DgvModes_SelectionChanged(object? sender, EventArgs e)
{
    if (dgvModes!.SelectedRows.Count > 0)
    {
        _selectedMode = dgvModes.SelectedRows[0].Tag as ModeInfo;
    }
    else
    {
        _selectedMode = null;
    }
}
```

- [ ] **Step 5: Удалить LvModes_ItemCheck**

Удалить полностью метод `LvModes_ItemCheck` ([MainForm.cs:139-143](src/ModeSwitcher.UI/MainForm.cs#L139-L143)):

```csharp
private void LvModes_ItemCheck(object? sender, ItemCheckEventArgs e)
{
    var mode = lvModes!.Items[e.Index].Tag as ModeInfo;
    e.NewValue = (mode?.IsActive ?? false) ? CheckState.Checked : CheckState.Unchecked;
}
```

- [ ] **Step 6: Заменить LvModes_MouseClick на DgvModes_CellContentClick**

Заменить метод `LvModes_MouseClick` ([MainForm.cs:145-166](src/ModeSwitcher.UI/MainForm.cs#L145-L166)) на:

```csharp
private async void DgvModes_CellContentClick(object? sender, DataGridViewCellEventArgs e)
{
    if (e.RowIndex < 0 || e.ColumnIndex != DELETE_COLUMN_INDEX) return;
    if (dgvModes!.Rows[e.RowIndex].Tag is not ModeInfo mode) return;

    dgvModes.Enabled = false;
    try
    {
        await HandleDeleteModeAsync(mode);
    }
    finally
    {
        dgvModes.Enabled = true;
    }
}
```

- [ ] **Step 7: Проверить компиляцию**

Команда:
```bash
dotnet build src/ModeSwitcher.UI/ModeSwitcher.UI.csproj -c Release -nologo 2>&1 | grep -E "(error|Build succeeded|Build FAILED)" | head -20
```

Expected: `Build succeeded.`

---

## Task 3: Пересборка preview/ModeSwitcher.UI.exe

**Files:** нет изменений кода.

- [ ] **Step 1: Удалить старый exe**

```bash
rm -f preview/ModeSwitcher.UI.exe
```

- [ ] **Step 2: Publish single-file в preview**

```bash
dotnet publish src/ModeSwitcher.UI/ModeSwitcher.UI.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=embedded -o preview -v quiet -nologo
```

Expected: создан `preview/ModeSwitcher.UI.exe` (~50 MB).

- [ ] **Step 3: Проверить наличие exe**

```bash
ls -lah preview/ModeSwitcher.UI.exe
```

Expected: файл существует, размер около 50 MB.

---

## Task 4: Ручной smoke-тест

**Files:** нет изменений кода.

- [ ] **Step 1: Запуск приложения**

Запустить `preview/ModeSwitcher.UI.exe`. Окно открывается без исключений.

- [ ] **Step 2: Визуальная проверка таблицы**

Проверить:
- Таблица имеет две колонки: "Режим" (растягивается) и пустая (90px) с кнопками "Удалить".
- Чекбоксов слева нет.
- Системных иконок (Application/Information/Question) нет.
- Активный режим (соответствует тексту "Агент: ...✓" сверху) подсвечен бледно-зелёным фоном, жирным шрифтом, с суффиксом " (активен)".

- [ ] **Step 3: Проверка применения режима**

- Выделить НЕактивный режим (одиночный клик по строке).
- Нажать "Применить выбранный режим".
- После завершения: подсветка переезжает на новую строку, шапка "Агент: ..." обновляется.

- [ ] **Step 4: Проверка удаления**

- Нажать "Удалить" в строке неактивного режима.
- Появляется диалог подтверждения.
- "Да" → строка исчезает, таблица перерисована.
- "Нет" → ничего не меняется.

- [ ] **Step 5: Проверка кликабельности кнопок-ячеек**

- При hover курсор меняется (стандартный pointer для кнопки).
- При нажатии кнопка визуально продавливается (нативный FlatStyle.System).

- [ ] **Step 6: Проверка двойного клика и F2**

- Двойной клик по строке: ничего не происходит (нет inline-редактирования).
- F2 на выделенной строке: ничего не происходит.

- [ ] **Step 7: Проверка ресайза формы**

- Растянуть форму по ширине.
- Колонка "Режим" растягивается, колонка с кнопками остаётся 90px.

- [ ] **Step 8: Проверка пустого состояния (опционально)**

Если есть возможность удалить все режимы — после удаления последнего строка "Нет доступных режимов" появляется без кнопки.

---

## Self-Review

**1. Spec coverage:**
- DataGridView с 2 колонками → Task 1 Step 4 ✓
- ButtonColumn для удаления → Task 1 Step 4 ✓
- Подсветка активного → Task 2 Step 3 ✓
- Удаление imgList и SystemIcons → Task 1 Step 1, 3 + Task 2 Step 1 ✓
- CellContentClick для удаления → Task 2 Step 6 ✓
- SelectionChanged для `_selectedMode` → Task 2 Step 4 ✓
- Пустое состояние "Нет доступных режимов" → Task 2 Step 3 ✓
- Smoke-чек-лист из 12 пунктов спеки → Task 4 покрывает ключевые ✓

**2. Placeholder scan:** Плейсхолдеров (TBD/TODO/"add error handling") нет. Все code-блоки полные.

**3. Type consistency:**
- `DELETE_COLUMN_INDEX = 1` остаётся (Task 2 использует, совпадает с индексом `colDelete` после Task 1).
- `RenderModesToGrid(CurrentModeResult?)` сигнатура одинаковая в Step 2 и Step 3 Task 2.
- `DgvModes_CellContentClick`, `DgvModes_SelectionChanged` имена совпадают в Designer (Task 1 Step 4) и MainForm (Task 2 Steps 4, 6).
- `_modes`, `_selectedMode`, `_switcher`, `GetDisplayName` — существующие члены, не переименовываются.
- `HandleDeleteModeAsync(ModeInfo)` — существующий метод, не меняется.
