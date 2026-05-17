# DataGridView for Modes — Design

**Date:** 2026-05-17
**Status:** Approved (design phase)

## Problem

Текущая реализация на `ListView` (см. [2026-05-17-listview-modes-design.md](2026-05-17-listview-modes-design.md)) имеет три практических проблемы:

1. **Активный режим визуально не отображается.** В `RenderModesToList` строится `checkedIcon = mode.IsActive ? 1 : 0`, но `ModeInfo.IsActive` нигде не выставляется при загрузке. Чекбокс активного режима не отмечен.
2. **Системные иконки выглядят чужеродно.** `SystemIcons.Application/Information/Question` — это OS-иконки общего назначения; в контексте "режимы агента" они выглядят как мусор.
3. **Чекбокс кликается, но ничего не делает.** `LvModes_ItemCheck` форсит состояние обратно (read-only по дизайну), что ломает интуицию: юзер видит чекбокс, ожидает поведения toggle, получает игнор.

Дополнительно: иконка "🗑️" в ячейке колонки — это эмодзи-символ, а не настоящая кнопка. Hover/pressed состояний нет, кликабельная зона неочевидна.

## Goals

Заменить `ListView` на `DataGridView`, при этом:

- Активный режим визуально выделяется (жирный шрифт + зелёная заливка строки + суффикс "(активен)").
- Удаление режима — настоящая кнопка `DataGridViewButtonCell` с нативными состояниями.
- Применение режима — только через кнопку "Применить выбранный режим" (как сейчас). Клик по строке только выделяет.
- Никаких системных иконок, никакого `ImageList`.

## Non-Goals

- Drag-and-drop сортировка.
- Multi-select.
- Inline-редактирование имени режима.
- Применение режима кликом по строке / двойным кликом.
- Context menu.

## Architecture

### Контрол: DataGridView

Замена `ListView lvModes` на `DataGridView dgvModes`. Конфигурация:

- `AllowUserToAddRows = false`
- `AllowUserToDeleteRows = false`
- `AllowUserToResizeRows = false`
- `MultiSelect = false`
- `RowHeadersVisible = false`
- `SelectionMode = DataGridViewSelectionMode.FullRowSelect`
- `ReadOnly = true` — целиком read-only; кнопочная колонка отрабатывает событие отдельно
- `EditMode = DataGridViewEditMode.EditProgrammatically` — отключает редактирование F2/двойным кликом
- `AutoGenerateColumns = false`
- `ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing`
- `BackgroundColor = SystemColors.Window` — убирает грязно-серый дефолт
- `BorderStyle = BorderStyle.FixedSingle`
- `Anchor = Top | Bottom | Left | Right` (как у `lvModes`)
- `Location = (20, 60)`, `Size = (360, 150)` — сохранить геометрию

### Колонки

1. **colMode** — `DataGridViewTextBoxColumn`
   - `HeaderText = "Режим"`
   - `AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill`
   - `ReadOnly = true`
   - `SortMode = DataGridViewColumnSortMode.NotSortable`

2. **colDelete** — `DataGridViewButtonColumn`
   - `HeaderText = ""` (пустая шапка)
   - `Width = 90`
   - `AutoSizeMode = DataGridViewAutoSizeColumnMode.None`
   - `Resizable = DataGridViewTriState.False`
   - `SortMode = DataGridViewColumnSortMode.NotSortable`
   - `Text = "Удалить"`, `UseColumnTextForButtonValue = true` — текст один на всю колонку
   - `FlatStyle = FlatStyle.System` — родная для OS отрисовка кнопки

### Data binding

Не используем `DataSource` — ручное наполнение, как было с `ListView`. Это согласуется с уже сложившейся практикой в проекте и не тянет за собой `BindingList<ModeInfo>` / `INotifyPropertyChanged`.

```csharp
private void RenderModesToGrid()
{
    dgvModes!.Rows.Clear();

    if (_modes is null || _modes.Count == 0)
    {
        // Пустое состояние: добавляем dummy-строку с текстом, заблокированную для удаления
        var emptyIndex = dgvModes.Rows.Add("Нет доступных режимов", string.Empty);
        var emptyRow = dgvModes.Rows[emptyIndex];
        emptyRow.DefaultCellStyle.ForeColor = Color.Gray;
        emptyRow.Tag = null; // нет ModeInfo → клик по Удалить игнорируется
        // Кнопка в пустой строке: скрываем её
        emptyRow.Cells[DELETE_COLUMN_INDEX] = new DataGridViewTextBoxCell { Value = string.Empty };
        return;
    }

    var currentModeName = _switcher.DetectCurrentMode()?.ModeName;

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
}
```

`LoadData` вызывает `RenderModesToGrid()` вместо `RenderModesToList()`. Метод `GetDisplayName` уже существует и не меняется. Поле `ModeInfo.IsActive` больше не используется UI — фактическое определение активного происходит через `_switcher.DetectCurrentMode()` (уже вызывается в `LoadData`, переиспользуем).

> **Note:** `LoadData` уже вызывает `DetectCurrentMode` один раз для шапки/трея. Можно либо вызвать второй раз внутри `RenderModesToGrid` (просто), либо передать результат параметром (чуть оптимальнее). Дизайн выбирает параметр: `RenderModesToGrid(CurrentModeResult? currentMode)` — один вызов на `LoadData`.

### События

```csharp
// Designer.cs
this.dgvModes.CellContentClick += new DataGridViewCellEventHandler(this.DgvModes_CellContentClick);
this.dgvModes.SelectionChanged += new EventHandler(this.DgvModes_SelectionChanged);
```

```csharp
// MainForm.cs
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

`HandleDeleteModeAsync` и `DeleteModeAsync(ModeInfo)` остаются без изменений.

### Что удаляется

В [src/ModeSwitcher.UI/MainForm.cs](src/ModeSwitcher.UI/MainForm.cs):
- `LvModes_MouseClick` — нет.
- `LvModes_ItemCheck` — нет.
- `LvModes_SelectedIndexChanged` — заменяется на `DgvModes_SelectionChanged`.
- `RenderModesToList` — заменяется на `RenderModesToGrid(CurrentModeResult?)`.
- В конструкторе: блок с `imgList.Images.Add(SystemIcons...)` — удалить целиком.

В [src/ModeSwitcher.UI/MainForm.Designer.cs](src/ModeSwitcher.UI/MainForm.Designer.cs):
- Поле `private ListView? lvModes;` → `private DataGridView? dgvModes;`
- Поле `private ImageList? imgList;` — удалить.
- Весь блок инициализации `lvModes` + колонок + `ImageList` — заменить на инициализацию `dgvModes` + 2 колонок.
- `this.Controls.Add(this.lvModes)` → `this.Controls.Add(this.dgvModes)`.

### Константа DELETE_COLUMN_INDEX

Остаётся как `private const int DELETE_COLUMN_INDEX = 1;` — значение совпадает с индексом `colDelete` в `dgvModes.Columns`.

## Visual Reference

```
┌──────────────────────────────────────────┐
│ Агент: 🧠 Claude ✓                       │
├──────────────────────────────┬───────────┤
│ Режим                        │           │   ← шапка колонок
├──────────────────────────────┼───────────┤
│ 🤖 Z                         │ [Удалить] │
│▓🧠 Claude (активен)          │ [Удалить] │   ← bold + green bg
│ Cursor                       │ [Удалить] │
└──────────────────────────────┴───────────┘
[Применить выбранный режим] [Сохранить текущий…]
[Обновить] [?] [Выход]
[Готово                                     ]
```

## Error Handling

- Клик по кнопке "Удалить" в пустой строке (state "Нет доступных режимов") — `row.Tag` равен `null`, обработчик возвращается раньше (`is not ModeInfo` early-return).
- Все исключения из `_switcher.DeleteModeAsync` ловятся в `HandleDeleteModeAsync` (логика без изменений: `MessageBox` + статус-бар).
- Удаление активного режима по-прежнему разрешено (контракт Core). После удаления `LoadData` перерисует грид; активной строки не будет, шапка покажет "Агент: не выбран".

## Testing

Юнит-тесты на UI отсутствуют (как и для предыдущей итерации). Ручной smoke-test:

1. Сборка и запуск `preview/ModeSwitcher.UI.exe`.
2. Грид содержит N строк по числу режимов. Колонок две: "Режим" и пустая шапка с кнопками.
3. Активный режим — жирный, зелёный фон, суффикс "(активен)".
4. Кнопка "Удалить" в каждой строке — нативная, реагирует на hover/press.
5. Клик по "Удалить" → диалог подтверждения → удаление → грид перерисован.
6. Клик по тексту режима выделяет строку, кнопка "Применить выбранный режим" использует её.
7. Клик по "Применить выбранный режим" — переключает режим, активной становится новая строка.
8. Двойной клик по строке ничего не делает (нет inline-редактирования, нет применения).
9. F2 на строке ничего не делает (`EditProgrammatically`).
10. Ресайз формы — колонка "Режим" растягивается, "Удалить" остаётся 90px.
11. Удаление последнего режима → состояние "Нет доступных режимов", кнопок нет.
12. Перезапуск приложения — удалённые режимы не возвращаются.

## Files Changed

- [src/ModeSwitcher.UI/MainForm.Designer.cs](src/ModeSwitcher.UI/MainForm.Designer.cs) — замена `lvModes`+`imgList` на `dgvModes` + 2 колонки.
- [src/ModeSwitcher.UI/MainForm.cs](src/ModeSwitcher.UI/MainForm.cs) — замена `RenderModesToList` / `LvModes_*` на `RenderModesToGrid` / `DgvModes_*`. Удаление инициализации `imgList`.

Core-проект не меняется.
