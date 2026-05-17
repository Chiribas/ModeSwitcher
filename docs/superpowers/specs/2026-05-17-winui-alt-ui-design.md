# Альтернативный UI на WinUI 3

**Статус:** Design approved, ожидает implementation plan
**Дата:** 2026-05-17
**Автор:** brainstorm-сессия

## Цель

Создать второй UI для ModeSwitcher на WinUI 3 (Windows App SDK) с **полным паритетом** функционала текущего WinForms-приложения. WinForms-версия остаётся живой; новый проект существует параллельно. Цель — оценить WinUI 3 на реальной задаче и получить современный Fluent-дизайн с Mica-фоном и системной темой.

## Контекст

Сейчас в репо:

- `ModeSwitcher.Core` — чистая логика, `net10.0`, без UI-зависимостей. Тестирован.
- `ModeSwitcher.UI` — WinForms приложение на `net10.0-windows`. Главное окно со списком режимов, удалением, кнопкой "Применить", диалогом "Сохранить текущий…", системным треем с меню переключения, About.
- Тесты: `ModeSwitcher.Core.Tests` и `ModeSwitcher.UI.Tests`.

Core полностью переиспользуется новым UI через `ProjectReference` и интерфейс `ICodeSwitcher`.

## Решения

| Вопрос | Решение |
|---|---|
| Отношение к WinForms | Параллельный проект, WinForms живёт дальше |
| Packaging | Unpackaged + self-contained (WindowsAppSDKSelfContained) |
| Scope первой итерации | Полный паритет с WinForms |
| UI-архитектура | MVVM с CommunityToolkit.Mvvm |
| Тема и стиль окна | Mica + System theme (dark/light по системе) |
| Тесты | Да, unit-тесты ViewModelов в новом проекте |

## Архитектура

### Структура solution

```
src/
├── ModeSwitcher.sln                  ← добавляется ModeSwitcher.WinUI
├── ModeSwitcher.Core/                ← без изменений
├── ModeSwitcher.UI/                  ← WinForms, без изменений
└── ModeSwitcher.WinUI/               ← новое
    ├── App.xaml / App.xaml.cs
    ├── MainWindow.xaml / .cs
    ├── Views/
    │   ├── SaveCurrentModeDialog.xaml
    │   └── AboutDialog.xaml
    ├── ViewModels/
    │   ├── MainViewModel.cs
    │   ├── ModeItemViewModel.cs
    │   ├── SaveCurrentModeViewModel.cs
    │   └── AboutViewModel.cs
    ├── Services/
    │   ├── ITrayService.cs / TrayService.cs
    │   └── IDialogService.cs / DialogService.cs
    ├── Converters/
    │   └── (по необходимости)
    ├── Assets/
    │   └── AppIcon.ico
    └── ModeSwitcher.WinUI.csproj

tests/
└── ModeSwitcher.WinUI.Tests/         ← новое
```

### Стек

- `Microsoft.WindowsAppSDK` 1.6.x (Windows App SDK с WinUI 3)
- `CommunityToolkit.Mvvm` 8.x (`[ObservableProperty]`, `[RelayCommand]`)
- `H.NotifyIcon.WinUI` 2.x (системный трей — WinUI 3 нативно не умеет)
- TargetFramework: `net10.0-windows10.0.19041.0`
- `UseWinUI=true`, `WindowsPackageType=None`
- `RuntimeIdentifiers=win-x64`

## UI/UX главного окна

Окно ~480x420, Mica-фон, системный titlebar, минимальные размеры заданы (нельзя сжать так, чтобы кнопки уехали).

```
┌─ ModeSwitcher ───────────────────── ─ □ × ┐
│                                            │
│  Текущий агент:  🧠 Claude  ✓              │
│  ─────────────────────────────────────────  │
│                                            │
│  ┌────────────────────────────────────┐    │
│  │ ◉ 🤖 Z                          🗑 │    │
│  │ ● 🧠 Claude (активен)           🗑 │    │← ListView c DataTemplate
│  │   📦 NewMode                    🗑 │    │
│  └────────────────────────────────────┘    │
│                                            │
│  [ Применить ]   [ Сохранить текущий… ]    │
│                                            │
│  [↻ Обновить]  [?]              [Выход]    │
│                                            │
│ ─────────────────────────────────────────  │
│  Готово                                    │← статус снизу
└────────────────────────────────────────────┘
```

**Контролы:**

- Шапка с текущим режимом — `TextBlock` с `BodyStrongTextBlockStyle`, активный режим выделяется галочкой.
- Список режимов — `ListView` с `DataTemplate`. Каждая строка содержит: индикатор активности (`FontIcon` checkmark), имя режима (с эмодзи-префиксом для известных), кнопка удалить (`Button` с иконкой Delete, появляется на hover).
- Кнопка "Применить" — `AccentButtonStyle`.
- Кнопка "Сохранить текущий…" — обычная `Button`.
- Нижний ряд: "Обновить" (с иконкой Refresh), "?" (About), "Выход" (справа).
- Статус снизу — `TextBlock` в `Border`, занимает ширину окна.

**Поведение (паритет с WinForms):**

- Клик по строке списка → `SelectedMode` в ViewModel.
- "Применить" → копирует файлы выбранного режима → перерисовка списка.
- "Сохранить текущий…" → открывает диалог.
- 🗑 на строке → ContentDialog подтверждения → удаление.
- Закрытие окна (X) → `Hide()`, приложение остаётся в трее.
- "Выход" — единственный способ полностью закрыть приложение (через кнопку или меню трея).

### Диалоги

- **SaveCurrentModeDialog** — `ContentDialog`. Содержит: поле имени, поле папки, чекбокс "Overwrite", `ListView` с чекбоксами файлов-кандидатов. Биндится к `SaveCurrentModeViewModel`. Кнопки "Сохранить" / "Отмена".
- **AboutDialog** — `ContentDialog` со статичным контентом (название, версия, ссылка).
- **Подтверждение удаления** — `ContentDialog` с текстом и кнопками "Да"/"Нет".

## MVVM — ViewModels и поток данных

### ViewModels

```csharp
public partial class MainViewModel : ObservableObject
{
    private readonly ICodeSwitcher _switcher;
    private readonly IDialogService _dialogs;
    private readonly ITrayService _tray;

    [ObservableProperty] private string currentModeText = "Агент: ...";
    [ObservableProperty] private string statusText = "Готово";
    [ObservableProperty] private ModeItemViewModel? selectedMode;
    [ObservableProperty] private bool isBusy;

    public ObservableCollection<ModeItemViewModel> Modes { get; } = new();

    public void LoadData();

    [RelayCommand(CanExecute = nameof(CanApply))]
    private async Task ApplyAsync();
    private bool CanApply() => SelectedMode is not null && !IsBusy;

    [RelayCommand] private async Task SaveCurrentAsync();
    [RelayCommand] private async Task DeleteModeAsync(ModeItemViewModel mode);
    [RelayCommand] private void Refresh();
    [RelayCommand] private void ShowAbout();
    [RelayCommand] private void ShowWindow();
    [RelayCommand] private void Exit();
}

public partial class ModeItemViewModel : ObservableObject
{
    [ObservableProperty] private bool isActive;
    public string Name { get; }
    public string DisplayName { get; }   // "🤖 Z", "🧠 Claude", или сам Name
    public string Label => IsActive ? $"{DisplayName} (активен)" : DisplayName;
}

public partial class SaveCurrentModeViewModel : ObservableObject
{
    [ObservableProperty] private string modeName = string.Empty;
    [ObservableProperty] private string folderName = string.Empty;
    [ObservableProperty] private bool overwriteRequested;
    public ObservableCollection<FileCandidateViewModel> Candidates { get; } = new();

    public bool DialogResult { get; private set; }
    public IEnumerable<string> SelectedRelativePaths =>
        Candidates.Where(c => c.IsSelected).Select(c => c.RelativePath);

    [RelayCommand(CanExecute = nameof(CanSave))] private void Save();
    private bool CanSave();
}

public partial class FileCandidateViewModel : ObservableObject
{
    [ObservableProperty] private bool isSelected;
    public string RelativePath { get; init; } = string.Empty;
}
```

### Сервисы

```csharp
public interface IDialogService
{
    Task<bool> ConfirmAsync(string title, string message);
    Task<bool> ShowSaveCurrentModeAsync(SaveCurrentModeViewModel vm);
    Task ShowAboutAsync();
    Task ShowErrorAsync(string title, string message);
}

public interface ITrayService
{
    void Initialize();
    void UpdateModes(IReadOnlyList<ModeItemViewModel> modes, string? currentModeName);
    void UpdateTooltip(string text);
    void ShowBalloon(string title, string message);
    event EventHandler<string> ModeSelected;       // имя режима из меню трея
    event EventHandler ShowRequested;              // клик/двойной клик по иконке
    event EventHandler ExitRequested;              // "Выход" в меню трея
}
```

### DI и инициализация

- `App.xaml.cs` в `OnLaunched` создаёт `CodeSwitcher`, `DialogService`, `TrayService`, затем `MainViewModel`, передаёт всё через конструктор.
- DI делаем вручную (без `Microsoft.Extensions.DependencyInjection`) — приложение маленькое, граф объектов очевиден, тащить контейнер избыточно.
- `MainWindow` получает `MainViewModel` через свой конструктор и кладёт в публичное свойство `ViewModel`. XAML-биндинги обращаются к нему через `x:Bind ViewModel.Xxx` (на `Window` нет полноценного `DataContext`, поэтому используем именованное свойство).

### Поток "Сохранить текущий"

1. ViewModel получает текущий режим из `ICodeSwitcher.DetectCurrentMode()` и путь к `modes/<folder>` для сравнения.
2. Через `ModeSaver.GetCandidates` (из Core) собирает список файлов-кандидатов.
3. Через `ModeNameSuggester.SuggestFromSettings` получает предлагаемое имя.
4. Создаёт `SaveCurrentModeViewModel` с полями: `ModeName=suggested`, `Candidates` со всеми путями (по умолчанию `IsSelected=true`), список существующих имён/папок для валидации.
5. Вызывает `IDialogService.ShowSaveCurrentModeAsync(vm)`. Диалог биндится к этой VM напрямую.
6. Если результат — `true`, вызывает `ICodeSwitcher.SaveCurrentAsModeAsync(vm.ModeName, vm.FolderName, vm.SelectedRelativePaths, vm.OverwriteRequested)`.
7. `LoadData()` — обновление списка.

### Привязки

- Списки и текст — `x:Bind` (compile-time, быстрее и проверяется компилятором).
- Команды — `Command="{x:Bind ViewModel.ApplyCommand}"`.
- `SelectedMode` биндится `Mode=TwoWay` к `ListView.SelectedItem`.

### Асинхронность

- Все обращения к `ICodeSwitcher.ApplyModeAsync` / `SaveCurrentAsModeAsync` / `DeleteModeAsync` — через `await` в `RelayCommand`-методах.
- Флаг `IsBusy` блокирует параллельные операции и отключает кнопки через `CanExecute`.
- Возврат на UI-поток автоматически (CommunityToolkit обрабатывает).

## Системный трей

`H.NotifyIcon.WinUI` предоставляет `TaskbarIcon`-контрол, который объявляется прямо в XAML.

```xml
<tb:TaskbarIcon
    x:Name="TrayIcon"
    IconSource="/Assets/AppIcon.ico"
    ToolTipText="ModeSwitcher"
    LeftClickCommand="{x:Bind ViewModel.ShowWindowCommand}">
    <tb:TaskbarIcon.ContextFlyout>
        <MenuFlyout x:Name="TrayMenu">
            <!-- режимы добавляются программно -->
            <MenuFlyoutSeparator/>
            <MenuFlyoutItem Text="👁️ Открыть" Command="{x:Bind ViewModel.ShowWindowCommand}"/>
            <MenuFlyoutItem Text="❌ Выход" Command="{x:Bind ViewModel.ExitCommand}"/>
        </MenuFlyout>
    </tb:TaskbarIcon.ContextFlyout>
</tb:TaskbarIcon>
```

**Динамическое меню режимов:** `TrayService.UpdateModes` пересобирает верхние `MenuFlyoutItem`-ы во флайауте. Активный режим помечается `Icon` (FontIcon checkmark) и более жирным шрифтом.

**События:**

- Левый клик по иконке → `ShowRequested` → ViewModel показывает окно (`Activate()`).
- Правый клик → системный ContextFlyout с режимами.
- Клик по пункту режима → `ModeSelected` с именем → ViewModel применяет.
- "Выход" → `ExitRequested` → корректное завершение (`Application.Current.Exit()` или `Window.Close()` на главном окне после установки флага).

**Уведомление:** `ShowBalloon` оборачивает `ShowNotification` H.NotifyIcon — показывается тост после переключения из трея.

## Тесты

Новый проект `tests/ModeSwitcher.WinUI.Tests/`, xUnit (как в `ModeSwitcher.Core.Tests`).

**TargetFramework:** `net10.0-windows10.0.19041.0` (чтобы видеть типы из WinUI проекта без runtime-вызовов).

**Покрытие:**

- `MainViewModelTests`
  - `LoadData_FillsModesAndCurrentMode`
  - `LoadData_NoCurrentMode_ShowsPlaceholder`
  - `ApplyCommand_Disabled_WhenNoSelection`
  - `ApplyCommand_CallsSwitcher_AndReloads`
  - `ApplyCommand_HandlesError_ShowsStatus`
  - `DeleteCommand_ShowsConfirm_CallsSwitcher`
  - `DeleteCommand_UserCancels_NoOp`
  - `SaveCurrent_ShowsDialog_CallsSwitcher`
  - `ExitCommand_RaisesExitRequested`
  - `Refresh_ReloadsModesFromSwitcher`
- `ModeItemViewModelTests`
  - `Label_WhenActive_IncludesActivePrefix`
  - `DisplayName_KnownModes_UsesEmojis`
- `SaveCurrentModeViewModelTests`
  - `SaveCommand_Disabled_WhenNameOrFolderEmpty`
  - `SaveCommand_Disabled_WhenNoCandidatesSelected`
  - `Validation_DetectsExistingName_Conflict`

**Моки:** ручные фейки в `Tests/Fakes/` — под стиль существующего `ModeSwitcher.Core.Tests` (там тоже без mock-фреймворков). Фейки реализуют интерфейсы: `FakeCodeSwitcher`, `FakeDialogService`, `FakeTrayService`. Это даёт явный контроль ответов и не тащит ещё одну зависимость.

**Что НЕ тестируем:**

- XAML-биндинги (нет надёжных headless-тестов для WinUI 3).
- Реальный `TrayService` (зависимость от H.NotifyIcon, UI-поток).
- Файловую систему (покрыто в `Core.Tests`).

## Сборка и распространение

### csproj основное

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0-windows10.0.19041.0</TargetFramework>
    <TargetPlatformMinVersion>10.0.17763.0</TargetPlatformMinVersion>
    <UseWinUI>true</UseWinUI>
    <WindowsPackageType>None</WindowsPackageType>
    <EnableMsixTooling>true</EnableMsixTooling>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <ApplicationIcon>Assets\AppIcon.ico</ApplicationIcon>
    <RuntimeIdentifiers>win-x64</RuntimeIdentifiers>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.6.*" />
    <PackageReference Include="Microsoft.Windows.SDK.BuildTools" Version="10.0.*" />
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.*" />
    <PackageReference Include="H.NotifyIcon.WinUI" Version="2.*" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\ModeSwitcher.Core\ModeSwitcher.Core.csproj" />
  </ItemGroup>
</Project>
```

### Команды

```bash
dotnet build src/ModeSwitcher.sln
dotnet test src/ModeSwitcher.sln

dotnet publish src/ModeSwitcher.WinUI/ModeSwitcher.WinUI.csproj \
  -c Release -r win-x64 --self-contained \
  -p:WindowsPackageType=None \
  -p:WindowsAppSDKSelfContained=true
```

### Распространение

- Single-file для WinUI 3 unpackaged ненадёжно (native dlls Windows App SDK не запекаются). Публикуем папкой.
- Папка билда: `production/ModeSwitcher.WinUI/` — добавить в `.gitignore` (по аналогии с уже untracked `production/`).
- Размер: ~150-200 MB папкой (включает Windows App SDK runtime). Принимаем как trade-off за unpackaged-режим без установки.

## Риски

1. **Зависимость от Windows App SDK runtime.** Митигировано: `WindowsAppSDKSelfContained=true` запекает runtime в билд.
2. **`H.NotifyIcon.WinUI` — сторонний пакет.** Если будет заброшен — переписать на P/Invoke `Shell_NotifyIcon`. Митигация: трей за интерфейсом `ITrayService`, замена точечная.
3. **Нет полноценного XAML-дизайнера.** Используем Hot Reload в VS. Не критично для окна такого размера.
4. **Тест-проект на `net10.0-windows10.0.19041.0`.** Чуть тяжелее CI, но без этого ViewModelы не скомпилируются. Если в будущем понадобится — выделить ViewModelы в отдельный библиотечный проект без WinUI-зависимостей. На старте — overengineering, не делаем.

## Решённые открытые вопросы

- Эмодзи-префиксы для известных режимов (🤖 Z, 🧠 Claude) — **хардкод** в коде, как в WinForms.
- Локализация — **не вводим**, все строки на русском хардкодом.
- Автозапуск с Windows — **не делаем**, в WinForms-версии тоже нет.

## Из scope исключено

- MSIX-packaging и подписание (можно добавить отдельной задачей).
- Замена WinForms-версии (WinForms живёт параллельно).
- Single-file билд (ограничение WinUI 3 unpackaged).
- Локализация интерфейса.
- Автозапуск с Windows.
- Headless / UI-автоматизация тесты для WinUI слоя.
