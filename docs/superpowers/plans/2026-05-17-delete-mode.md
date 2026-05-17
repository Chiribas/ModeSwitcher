# Delete Mode Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add `DeleteModeAsync` to Core and an `×` button per mode in the UI so users can remove a mode (folder + JSON entry) from inside the app.

**Architecture:** Approach A from [the design doc](../specs/2026-05-17-delete-mode-design.md). New `ICodeSwitcher.DeleteModeAsync(string)` composes existing `ConfigWriter` + `IFileSystem.DeleteDirectory` + `Reload`. UI renders a small `×` Button next to each radio in `RenderModes`.

**Tech Stack:** .NET 8, WinForms, xUnit + FluentAssertions + NSubstitute.

**Repo notes:**
- Build: `dotnet build src/ModeSwitcher.sln`
- Tests: `dotnet test tests/ModeSwitcher.Core.Tests/ModeSwitcher.Core.Tests.csproj`
- User does NOT want commits from the agent during execution; skip the `git commit` step at the end of each task. The user will commit themselves later. Each task still ends with a "Verify" step in place of "Commit".

---

## Task 1: Core — `DeleteModeAsync`

**Files:**
- Modify: `src/ModeSwitcher.Core/IModeSwitcher.cs`
- Modify: `src/ModeSwitcher.Core/ModeSwitcher.cs`
- Modify: `tests/ModeSwitcher.Core.Tests/ModeSwitcherTests.cs`

- [ ] **Step 1.1: Write the failing tests**

Append inside `CodeSwitcherTests` in `tests/ModeSwitcher.Core.Tests/ModeSwitcherTests.cs`:

```csharp
[Fact]
public async Task DeleteModeAsync_ExistingMode_RemovesFolderAndUpdatesConfig()
{
    // Arrange
    var fsMock = Substitute.For<IFileSystem>();
    var configLoader = new ConfigLoader(fsMock);
    var fileComparer = new FileComparer(fsMock);
    var fileCopier = new FileCopier(fsMock);
    var modeSaver = new ModeSaver(fsMock);
    var configWriter = new ConfigWriter(fsMock);

    var initial = new SwitcherConfig
    {
        TargetPath = "C:\\Target",
        Modes = new List<ModeDefinition>
        {
            new() { Name = "A", Folder = "a" },
            new() { Name = "B", Folder = "b" }
        }
    };

    fsMock.OpenRead(Arg.Any<string>())
        .Returns(_ => new MemoryStream(System.Text.Encoding.UTF8.GetBytes(
            System.Text.Json.JsonSerializer.Serialize(initial))));

    var writeStream = new MemoryStream();
    fsMock.OpenWrite("test.json.tmp").Returns(writeStream);

    var folderPath = Path.Combine(Path.GetDirectoryName("test.json")!, "modes", "a");
    fsMock.DirectoryExists(folderPath).Returns(true);

    var switcher = new ModeSwitcher.Core.CodeSwitcher(
        "test.json", fsMock, configLoader, fileComparer, fileCopier, modeSaver, configWriter);

    // Act
    await switcher.DeleteModeAsync("A");

    // Assert
    fsMock.Received(1).DeleteDirectory(folderPath, true);

    var saved = System.Text.Json.JsonSerializer.Deserialize<SwitcherConfig>(writeStream.ToArray());
    saved!.Modes.Should().HaveCount(1);
    saved.Modes[0].Name.Should().Be("B");
}

[Fact]
public async Task DeleteModeAsync_NonExistentMode_DoesNothing()
{
    // Arrange
    var fsMock = Substitute.For<IFileSystem>();
    var configLoader = new ConfigLoader(fsMock);
    var fileComparer = new FileComparer(fsMock);
    var fileCopier = new FileCopier(fsMock);
    var modeSaver = new ModeSaver(fsMock);
    var configWriter = new ConfigWriter(fsMock);

    var initial = new SwitcherConfig
    {
        TargetPath = "C:\\Target",
        Modes = new List<ModeDefinition> { new() { Name = "A", Folder = "a" } }
    };

    fsMock.OpenRead(Arg.Any<string>())
        .Returns(_ => new MemoryStream(System.Text.Encoding.UTF8.GetBytes(
            System.Text.Json.JsonSerializer.Serialize(initial))));

    var switcher = new ModeSwitcher.Core.CodeSwitcher(
        "test.json", fsMock, configLoader, fileComparer, fileCopier, modeSaver, configWriter);

    // Act
    await switcher.DeleteModeAsync("Ghost");

    // Assert: no folder deleted, no config write
    fsMock.DidNotReceive().DeleteDirectory(Arg.Any<string>(), Arg.Any<bool>());
    fsMock.DidNotReceive().OpenWrite(Arg.Any<string>());
}

[Fact]
public async Task DeleteModeAsync_FolderMissingOnDisk_StillUpdatesConfig()
{
    // Arrange
    var fsMock = Substitute.For<IFileSystem>();
    var configLoader = new ConfigLoader(fsMock);
    var fileComparer = new FileComparer(fsMock);
    var fileCopier = new FileCopier(fsMock);
    var modeSaver = new ModeSaver(fsMock);
    var configWriter = new ConfigWriter(fsMock);

    var initial = new SwitcherConfig
    {
        TargetPath = "C:\\Target",
        Modes = new List<ModeDefinition> { new() { Name = "A", Folder = "a" } }
    };

    fsMock.OpenRead(Arg.Any<string>())
        .Returns(_ => new MemoryStream(System.Text.Encoding.UTF8.GetBytes(
            System.Text.Json.JsonSerializer.Serialize(initial))));

    var writeStream = new MemoryStream();
    fsMock.OpenWrite("test.json.tmp").Returns(writeStream);

    var folderPath = Path.Combine(Path.GetDirectoryName("test.json")!, "modes", "a");
    fsMock.DirectoryExists(folderPath).Returns(false);

    var switcher = new ModeSwitcher.Core.CodeSwitcher(
        "test.json", fsMock, configLoader, fileComparer, fileCopier, modeSaver, configWriter);

    // Act
    await switcher.DeleteModeAsync("A");

    // Assert: no DeleteDirectory call, but config still rewritten
    fsMock.DidNotReceive().DeleteDirectory(Arg.Any<string>(), Arg.Any<bool>());

    var saved = System.Text.Json.JsonSerializer.Deserialize<SwitcherConfig>(writeStream.ToArray());
    saved!.Modes.Should().BeEmpty();
}

[Fact]
public async Task DeleteModeAsync_InvalidatesConfigCache()
{
    // Arrange
    var fsMock = Substitute.For<IFileSystem>();
    var configLoader = new ConfigLoader(fsMock);
    var fileComparer = new FileComparer(fsMock);
    var fileCopier = new FileCopier(fsMock);
    var modeSaver = new ModeSaver(fsMock);
    var configWriter = new ConfigWriter(fsMock);

    var initial = new SwitcherConfig
    {
        TargetPath = "C:\\Target",
        Modes = new List<ModeDefinition> { new() { Name = "A", Folder = "a" } }
    };
    var jsonInitial = System.Text.Json.JsonSerializer.Serialize(initial);

    fsMock.OpenRead(Arg.Any<string>())
        .Returns(_ => new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonInitial)));

    var writeStream = new MemoryStream();
    fsMock.OpenWrite("test.json.tmp").Returns(writeStream);

    var folderPath = Path.Combine(Path.GetDirectoryName("test.json")!, "modes", "a");
    fsMock.DirectoryExists(folderPath).Returns(true);

    var switcher = new ModeSwitcher.Core.CodeSwitcher(
        "test.json", fsMock, configLoader, fileComparer, fileCopier, modeSaver, configWriter);

    _ = switcher.GetModes(); // primes cache
    await switcher.DeleteModeAsync("A");
    _ = switcher.GetModes();

    fsMock.ReceivedCalls()
        .Count(c => c.GetMethodInfo().Name == nameof(IFileSystem.OpenRead))
        .Should().BeGreaterThanOrEqualTo(2);
}
```

- [ ] **Step 1.2: Run tests — expect FAIL (DeleteModeAsync missing)**

```bash
dotnet test tests/ModeSwitcher.Core.Tests/ModeSwitcher.Core.Tests.csproj --filter "FullyQualifiedName~CodeSwitcherTests.DeleteModeAsync" --nologo --verbosity quiet
```

Expected: compile error.

- [ ] **Step 1.3: Add the method to the interface**

In `src/ModeSwitcher.Core/IModeSwitcher.cs`, inside the interface, add after `SaveCurrentAsModeAsync`:

```csharp
Task DeleteModeAsync(string modeName);
```

- [ ] **Step 1.4: Implement the method in `CodeSwitcher`**

In `src/ModeSwitcher.Core/ModeSwitcher.cs`, add directly after `SaveCurrentAsModeAsync`:

```csharp
public Task DeleteModeAsync(string modeName)
{
    var config = LoadConfig();
    if (config is null)
    {
        throw new InvalidOperationException("Config could not be loaded.");
    }

    var def = config.Modes.FirstOrDefault(m => m.Name == modeName);
    if (def is null)
    {
        return Task.CompletedTask;
    }

    var modePath = Path.Combine(_modesBasePath, def.Folder);
    if (_fileSystem.DirectoryExists(modePath))
    {
        _fileSystem.DeleteDirectory(modePath, recursive: true);
    }

    config.Modes.Remove(def);
    _configWriter.Save(_configPath, config);
    Reload();

    return Task.CompletedTask;
}
```

- [ ] **Step 1.5: Run tests — expect PASS**

```bash
dotnet test tests/ModeSwitcher.Core.Tests/ModeSwitcher.Core.Tests.csproj --filter "FullyQualifiedName~CodeSwitcherTests.DeleteModeAsync" --nologo --verbosity quiet
```

Expected: 4 passed.

- [ ] **Step 1.6: Run full Core test suite — no regressions**

```bash
dotnet test tests/ModeSwitcher.Core.Tests/ModeSwitcher.Core.Tests.csproj --nologo --verbosity quiet
```

Expected: all previous tests still pass.

- [ ] **Step 1.7: Verify (no commit)**

Do NOT `git commit`. Per user request, all changes accumulate locally; user commits themselves.

---

## Task 2: UI — render `×` next to each mode

**Files:**
- Modify: `src/ModeSwitcher.UI/MainForm.cs`

- [ ] **Step 2.1: Update `RenderModes` to add a delete button per row**

In `src/ModeSwitcher.UI/MainForm.cs`, replace the entire `RenderModes()` method ([MainForm.cs:92-138](../../../src/ModeSwitcher.UI/MainForm.cs#L92-L138)) with:

```csharp
private void RenderModes()
{
    pnlModes!.Controls.Clear();

    if (_modes is null || _modes.Count == 0)
    {
        var lbl = new Label
        {
            Text = "Нет доступных режимов",
            Location = new Point(10, 10),
            AutoSize = true
        };
        pnlModes.Controls.Add(lbl);
        return;
    }

    var y = 10;
    foreach (var mode in _modes)
    {
        var displayName = GetDisplayName(mode.Name);
        var radio = new RadioButton
        {
            Text = mode.IsActive ? $"{displayName} (активен)" : displayName,
            Location = new Point(10, y),
            Width = pnlModes.ClientSize.Width - 60,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Checked = mode.IsActive,
            Tag = mode,
            Font = new Font("Segoe UI", 10F, mode.IsActive ? FontStyle.Bold : FontStyle.Regular)
        };

        radio.CheckedChanged += (s, e) =>
        {
            if (radio.Checked)
            {
                _selectedMode = mode;
            }
        };

        if (mode.IsActive)
        {
            _selectedMode = mode;
        }

        var modeForClosure = mode;
        var btnDelete = new Button
        {
            Text = "×",
            Size = new Size(25, 25),
            Location = new Point(pnlModes.ClientSize.Width - 35, y),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            TabStop = false
        };
        btnDelete.Click += async (s, e) => await DeleteModeAsync(modeForClosure);

        pnlModes.Controls.Add(radio);
        pnlModes.Controls.Add(btnDelete);
        y += 35;
    }
}
```

- [ ] **Step 2.2: Add the delete handler method**

In `src/ModeSwitcher.UI/MainForm.cs`, add a new method right after `RenderModes` (before `UpdateTrayMenu`):

```csharp
private async Task DeleteModeAsync(ModeInfo mode)
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

- [ ] **Step 2.3: Build**

```bash
dotnet build src/ModeSwitcher.sln --nologo --verbosity quiet
```

Expected: `Build succeeded. 0 Error(s)`.

- [ ] **Step 2.4: Manual smoke test**

Make sure the existing `production/ModeSwitcher.UI.exe` is closed, then publish & launch:

```bash
dotnet publish src/ModeSwitcher.UI/ModeSwitcher.UI.csproj -c Release -o production --nologo --verbosity quiet
```

Run `production/ModeSwitcher.UI.exe` and verify:
1. Each mode row has an `×` button on the right.
2. Clicking `×` on a non-active mode → YesNo dialog → Yes deletes folder + entry; list refreshes; status bar shows "Режим X удалён."
3. Clicking `×` on the active mode → same dialog → Yes works; list shows remaining modes; "Агент: не выбран" appears.
4. No → dialog closes, nothing changes.
5. After delete, restart the app — the mode stays gone (config persisted).
6. Window can still be resized; the `×` stays at the right edge as the panel grows.

Close the app.

- [ ] **Step 2.5: Verify (no commit)**

Do NOT `git commit`. Per user request, changes stay local.

---

## Task 3: Final verification

- [ ] **Step 3.1: Full test suite**

```bash
dotnet test src/ModeSwitcher.sln --nologo --verbosity quiet
```

Expected: 0 failures (existing tests + 4 new `DeleteModeAsync` tests).

- [ ] **Step 3.2: Report status**

Summarise: Core changes, UI changes, test counts, that nothing has been committed and the user owns the final commit decision.

---

## Summary of files touched

**Modified:**
- `src/ModeSwitcher.Core/IModeSwitcher.cs`
- `src/ModeSwitcher.Core/ModeSwitcher.cs`
- `src/ModeSwitcher.UI/MainForm.cs`
- `tests/ModeSwitcher.Core.Tests/ModeSwitcherTests.cs`

No new files created — feature reuses `ConfigWriter`, `IFileSystem.DeleteDirectory`, and `Reload` from prior work.
