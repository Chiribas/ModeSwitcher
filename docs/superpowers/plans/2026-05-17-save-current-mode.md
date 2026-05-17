# Save Current Mode & Scrollable Mode List — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a "Save current mode" button that captures TargetPath state into a new mode folder + JSON entry, and make the mode list scroll inside a resizable window.

**Architecture:** Approach A from [the design doc](../specs/2026-05-17-save-current-mode-design.md) — minimal UI upgrade plus three new Core services (`ConfigWriter`, `ModeSaver`, `ModeNameSuggester`) and two new methods on `ICodeSwitcher` (`SaveCurrentAsModeAsync`, `Reload`).

**Tech Stack:** .NET 8, WinForms, xUnit + FluentAssertions + NSubstitute for testing, `System.Text.Json` for config IO.

**Repo notes:**
- Build: `dotnet build src/ModeSwitcher.sln`
- Run tests: `dotnet test src/ModeSwitcher.sln` (or filter per task)
- Path conventions in tests use Windows separators (`\\`) — match existing test style.
- The spec mentions "FakeFileSystem" in its testing section; actual codebase uses `Substitute.For<IFileSystem>()` (NSubstitute). This plan uses the real pattern.

---

## Task 1: Add `OpenWrite` and `MoveFile` to `IFileSystem`

`ConfigWriter` will write via a temp file + rename to avoid corrupting `modeswitcher.json` on crash mid-write. That needs two new ops on `IFileSystem`.

**Files:**
- Modify: `src/ModeSwitcher.Core/FileSystem/IFileSystem.cs`
- Modify: `src/ModeSwitcher.Core/FileSystem/RealFileSystem.cs`
- Modify: `tests/ModeSwitcher.Core.Tests/FileSystemTests.cs`

- [ ] **Step 1.1: Write the failing tests**

Append to `tests/ModeSwitcher.Core.Tests/FileSystemTests.cs` (inside the existing class):

```csharp
[Fact]
public void OpenWrite_CreatesFileAndReturnsWritableStream()
{
    // Arrange
    var fs = new RealFileSystem();
    var tempPath = Path.Combine(Path.GetTempPath(), $"openwrite_test_{Guid.NewGuid():N}.txt");

    try
    {
        // Act
        using (var stream = fs.OpenWrite(tempPath))
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes("hello");
            stream.Write(bytes, 0, bytes.Length);
        }

        // Assert
        File.Exists(tempPath).Should().BeTrue();
        File.ReadAllText(tempPath).Should().Be("hello");
    }
    finally
    {
        if (File.Exists(tempPath)) File.Delete(tempPath);
    }
}

[Fact]
public void MoveFile_WithOverwrite_ReplacesExistingFile()
{
    // Arrange
    var fs = new RealFileSystem();
    var src = Path.Combine(Path.GetTempPath(), $"move_src_{Guid.NewGuid():N}.txt");
    var dst = Path.Combine(Path.GetTempPath(), $"move_dst_{Guid.NewGuid():N}.txt");
    File.WriteAllText(src, "fresh");
    File.WriteAllText(dst, "stale");

    try
    {
        // Act
        fs.MoveFile(src, dst, overwrite: true);

        // Assert
        File.Exists(src).Should().BeFalse();
        File.ReadAllText(dst).Should().Be("fresh");
    }
    finally
    {
        if (File.Exists(src)) File.Delete(src);
        if (File.Exists(dst)) File.Delete(dst);
    }
}
```

- [ ] **Step 1.2: Run tests — expect FAIL (methods missing)**

```bash
dotnet test src/ModeSwitcher.sln --filter "FullyQualifiedName~FileSystemTests.OpenWrite|FullyQualifiedName~FileSystemTests.MoveFile"
```

Expected: compile errors.

- [ ] **Step 1.3: Add `OpenWrite` and `MoveFile` to `IFileSystem`**

In `src/ModeSwitcher.Core/FileSystem/IFileSystem.cs`, add to the interface (after `OpenRead`):

```csharp
System.IO.Stream OpenWrite(string path);
void MoveFile(string source, string dest, bool overwrite);
```

- [ ] **Step 1.4: Implement both in `RealFileSystem`**

In `src/ModeSwitcher.Core/FileSystem/RealFileSystem.cs`, add (after `OpenRead`):

```csharp
public System.IO.Stream OpenWrite(string path) => File.Create(path);

public void MoveFile(string source, string dest, bool overwrite)
    => File.Move(source, dest, overwrite);
```

(`File.Create` truncates an existing file — fine since we're writing to a temp path that we control. `File.Move(..., overwrite: true)` is .NET Core 3.0+, available in our .NET 8 target.)

- [ ] **Step 1.5: Run tests — expect PASS**

```bash
dotnet test src/ModeSwitcher.sln --filter "FullyQualifiedName~FileSystemTests.OpenWrite|FullyQualifiedName~FileSystemTests.MoveFile"
```

Expected: 2 passed.

- [ ] **Step 1.6: Commit**

```bash
git add src/ModeSwitcher.Core/FileSystem/IFileSystem.cs src/ModeSwitcher.Core/FileSystem/RealFileSystem.cs tests/ModeSwitcher.Core.Tests/FileSystemTests.cs
git commit -m "feat(core): add OpenWrite and MoveFile to IFileSystem"
```

---

## Task 2: `ConfigWriter` service

**Files:**
- Create: `src/ModeSwitcher.Core/Services/ConfigWriter.cs`
- Create: `tests/ModeSwitcher.Core.Tests/Services/ConfigWriterTests.cs`

- [ ] **Step 2.1: Write failing tests**

Create `tests/ModeSwitcher.Core.Tests/Services/ConfigWriterTests.cs`:

```csharp
using System.Text;
using System.Text.Json;
using FluentAssertions;
using ModeSwitcher.Core.FileSystem;
using ModeSwitcher.Core.Models;
using ModeSwitcher.Core.Services;
using NSubstitute;
using Xunit;

namespace ModeSwitcher.Core.Tests.Services;

public class ConfigWriterTests
{
    [Fact]
    public void Save_RoundTripsConfig()
    {
        // Arrange
        var fsMock = Substitute.For<IFileSystem>();
        var writeStream = new MemoryStream();
        fsMock.OpenWrite("out.json.tmp").Returns(writeStream);

        var writer = new ConfigWriter(fsMock);
        var config = new SwitcherConfig
        {
            TargetPath = "C:\\Target",
            Modes = new List<ModeDefinition>
            {
                new() { Name = "Prod", Folder = "prod" },
                new() { Name = "Dev",  Folder = "dev"  }
            }
        };

        // Act
        writer.Save("out.json", config);

        // Assert: parse bytes back and compare
        var json = Encoding.UTF8.GetString(writeStream.ToArray());
        var roundTripped = JsonSerializer.Deserialize<SwitcherConfig>(json);
        roundTripped.Should().NotBeNull();
        roundTripped!.TargetPath.Should().Be("C:\\Target");
        roundTripped.Modes.Should().HaveCount(2);
        roundTripped.Modes[0].Name.Should().Be("Prod");
        roundTripped.Modes[1].Folder.Should().Be("dev");

        // And: temp file was atomically moved into place
        fsMock.Received(1).MoveFile("out.json.tmp", "out.json", true);
    }

    [Fact]
    public void Save_WritesIndentedJson()
    {
        // Arrange
        var fsMock = Substitute.For<IFileSystem>();
        var writeStream = new MemoryStream();
        fsMock.OpenWrite(Arg.Any<string>()).Returns(writeStream);
        var writer = new ConfigWriter(fsMock);
        var config = new SwitcherConfig
        {
            TargetPath = "X",
            Modes = new List<ModeDefinition> { new() { Name = "A", Folder = "a" } }
        };

        // Act
        writer.Save("out.json", config);

        // Assert
        var json = Encoding.UTF8.GetString(writeStream.ToArray());
        json.Should().Contain("\n"); // indented JSON has newlines
        json.Should().Contain("  ");  // and indentation
    }
}
```

- [ ] **Step 2.2: Run tests — expect FAIL (ConfigWriter not defined)**

```bash
dotnet test src/ModeSwitcher.sln --filter "FullyQualifiedName~ConfigWriterTests"
```

Expected: compile error "ConfigWriter not found".

- [ ] **Step 2.3: Implement `ConfigWriter`**

Create `src/ModeSwitcher.Core/Services/ConfigWriter.cs`:

```csharp
using System.Text.Json;
using ModeSwitcher.Core.FileSystem;
using ModeSwitcher.Core.Models;

namespace ModeSwitcher.Core.Services;

public class ConfigWriter
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    private readonly IFileSystem _fileSystem;

    public ConfigWriter(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public void Save(string configPath, SwitcherConfig config)
    {
        // Write to a temp file then move atomically — protects modeswitcher.json
        // from a half-written state if the process dies mid-write.
        var tempPath = configPath + ".tmp";
        using (var stream = _fileSystem.OpenWrite(tempPath))
        {
            JsonSerializer.Serialize(stream, config, Options);
        }
        _fileSystem.MoveFile(tempPath, configPath, overwrite: true);
    }
}
```

- [ ] **Step 2.4: Run tests — expect PASS**

```bash
dotnet test src/ModeSwitcher.sln --filter "FullyQualifiedName~ConfigWriterTests"
```

Expected: 2 passed.

- [ ] **Step 2.5: Commit**

```bash
git add src/ModeSwitcher.Core/Services/ConfigWriter.cs tests/ModeSwitcher.Core.Tests/Services/ConfigWriterTests.cs
git commit -m "feat(core): add ConfigWriter for persisting SwitcherConfig"
```

---

## Task 3: `ModeNameSuggester` — `ToFolderName`

Split into two tasks (one per static method) for tighter TDD loops. This task does sanitisation only.

**Files:**
- Create: `src/ModeSwitcher.Core/Services/ModeNameSuggester.cs`
- Create: `tests/ModeSwitcher.Core.Tests/Services/ModeNameSuggesterTests.cs`

- [ ] **Step 3.1: Write failing tests**

Create `tests/ModeSwitcher.Core.Tests/Services/ModeNameSuggesterTests.cs`:

```csharp
using FluentAssertions;
using ModeSwitcher.Core.Services;
using Xunit;

namespace ModeSwitcher.Core.Tests.Services;

public class ModeNameSuggesterTests
{
    [Theory]
    [InlineData("localhost (qwen2.5-coder:14b)", "localhost_qwen2.5-coder_14b")]
    [InlineData("api.openai.com (gpt-4)", "api.openai.com_gpt-4")]
    [InlineData("hello", "hello")]
    [InlineData("a/b\\c?d*e", "a_b_c_d_e")]
    [InlineData("multiple   spaces", "multiple_spaces")]
    [InlineData("__leading_and_trailing__", "leading_and_trailing")]
    public void ToFolderName_SanitisesAndCollapses(string input, string expected)
    {
        ModeNameSuggester.ToFolderName(input).Should().Be(expected);
    }

    [Fact]
    public void ToFolderName_AllNonAscii_ReturnsEmpty()
    {
        ModeNameSuggester.ToFolderName("Жанклод").Should().Be("");
    }
}
```

- [ ] **Step 3.2: Run tests — expect FAIL (ModeNameSuggester not defined)**

```bash
dotnet test src/ModeSwitcher.sln --filter "FullyQualifiedName~ModeNameSuggesterTests"
```

Expected: compile error.

- [ ] **Step 3.3: Implement `ToFolderName` (minimal class)**

Create `src/ModeSwitcher.Core/Services/ModeNameSuggester.cs`:

```csharp
using System.Text;
using System.Text.RegularExpressions;

namespace ModeSwitcher.Core.Services;

public static class ModeNameSuggester
{
    private static readonly Regex InvalidChars = new(@"[^A-Za-z0-9._\-]", RegexOptions.Compiled);
    private static readonly Regex MultipleUnderscores = new("_+", RegexOptions.Compiled);

    public static string ToFolderName(string displayName)
    {
        if (string.IsNullOrEmpty(displayName)) return "";
        var replaced = InvalidChars.Replace(displayName, "_");
        var collapsed = MultipleUnderscores.Replace(replaced, "_");
        return collapsed.Trim('_');
    }
}
```

- [ ] **Step 3.4: Run tests — expect PASS**

```bash
dotnet test src/ModeSwitcher.sln --filter "FullyQualifiedName~ModeNameSuggesterTests.ToFolderName"
```

Expected: 7 passed.

- [ ] **Step 3.5: Commit**

```bash
git add src/ModeSwitcher.Core/Services/ModeNameSuggester.cs tests/ModeSwitcher.Core.Tests/Services/ModeNameSuggesterTests.cs
git commit -m "feat(core): add ModeNameSuggester.ToFolderName"
```

---

## Task 4: `ModeNameSuggester` — `SuggestFromSettings`

**Files:**
- Modify: `src/ModeSwitcher.Core/Services/ModeNameSuggester.cs`
- Modify: `tests/ModeSwitcher.Core.Tests/Services/ModeNameSuggesterTests.cs`

- [ ] **Step 4.1: Write failing tests**

Append to `tests/ModeSwitcher.Core.Tests/Services/ModeNameSuggesterTests.cs` inside the existing class:

```csharp
[Fact]
public void SuggestFromSettings_ExtractsHostAndModel()
{
    // Arrange
    var fsMock = Substitute.For<IFileSystem>();
    var json = """
        {
          "env": {
            "ANTHROPIC_BASE_URL": "http://localhost:11434",
            "model": "qwen2.5-coder:14b"
          }
        }
        """;
    fsMock.OpenRead("settings.json")
        .Returns(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)));

    // Act
    var result = ModeNameSuggester.SuggestFromSettings("settings.json", fsMock);

    // Assert
    result.Should().Be("localhost (qwen2.5-coder:14b)");
}

[Fact]
public void SuggestFromSettings_FallsBackToTopLevelModel()
{
    var fsMock = Substitute.For<IFileSystem>();
    var json = """
        {
          "env": { "ANTHROPIC_BASE_URL": "https://api.openai.com" },
          "model": "gpt-4"
        }
        """;
    fsMock.OpenRead(Arg.Any<string>())
        .Returns(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)));

    ModeNameSuggester.SuggestFromSettings("s.json", fsMock)
        .Should().Be("api.openai.com (gpt-4)");
}

[Fact]
public void SuggestFromSettings_MissingFile_ReturnsNull()
{
    var fsMock = Substitute.For<IFileSystem>();
    fsMock.OpenRead(Arg.Any<string>()).Returns(x => throw new FileNotFoundException());

    ModeNameSuggester.SuggestFromSettings("missing.json", fsMock).Should().BeNull();
}

[Fact]
public void SuggestFromSettings_MalformedJson_ReturnsNull()
{
    var fsMock = Substitute.For<IFileSystem>();
    fsMock.OpenRead(Arg.Any<string>())
        .Returns(new MemoryStream(System.Text.Encoding.UTF8.GetBytes("{ not json")));

    ModeNameSuggester.SuggestFromSettings("bad.json", fsMock).Should().BeNull();
}

[Fact]
public void SuggestFromSettings_NoBaseUrl_ReturnsNull()
{
    var fsMock = Substitute.For<IFileSystem>();
    var json = """{ "env": { "model": "x" } }""";
    fsMock.OpenRead(Arg.Any<string>())
        .Returns(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)));

    ModeNameSuggester.SuggestFromSettings("s.json", fsMock).Should().BeNull();
}

[Fact]
public void SuggestFromSettings_NoModel_ReturnsNull()
{
    var fsMock = Substitute.For<IFileSystem>();
    var json = """{ "env": { "ANTHROPIC_BASE_URL": "http://x" } }""";
    fsMock.OpenRead(Arg.Any<string>())
        .Returns(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)));

    ModeNameSuggester.SuggestFromSettings("s.json", fsMock).Should().BeNull();
}
```

Add `using`s at top if not already there:

```csharp
using ModeSwitcher.Core.FileSystem;
using NSubstitute;
```

- [ ] **Step 4.2: Run tests — expect FAIL (method missing)**

```bash
dotnet test src/ModeSwitcher.sln --filter "FullyQualifiedName~ModeNameSuggesterTests.SuggestFromSettings"
```

Expected: compile error.

- [ ] **Step 4.3: Implement `SuggestFromSettings`**

Modify `src/ModeSwitcher.Core/Services/ModeNameSuggester.cs` — add at top:

```csharp
using System.Text.Json;
using ModeSwitcher.Core.FileSystem;
```

Inside the class add:

```csharp
public static string? SuggestFromSettings(string settingsJsonPath, IFileSystem fileSystem)
{
    try
    {
        using var stream = fileSystem.OpenRead(settingsJsonPath);
        using var doc = JsonDocument.Parse(stream);
        var root = doc.RootElement;

        var url = TryGetStringDeep(root, "env", "ANTHROPIC_BASE_URL");
        if (url is null) return null;

        var model = TryGetStringDeep(root, "env", "model")
                    ?? TryGetStringDeep(root, "model");
        if (model is null) return null;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return null;
        return $"{uri.Host} ({model})";
    }
    catch
    {
        return null;
    }
}

private static string? TryGetStringDeep(JsonElement element, params string[] path)
{
    var current = element;
    foreach (var key in path)
    {
        if (current.ValueKind != JsonValueKind.Object) return null;
        if (!current.TryGetProperty(key, out var next)) return null;
        current = next;
    }
    return current.ValueKind == JsonValueKind.String ? current.GetString() : null;
}
```

- [ ] **Step 4.4: Run tests — expect PASS**

```bash
dotnet test src/ModeSwitcher.sln --filter "FullyQualifiedName~ModeNameSuggesterTests"
```

Expected: 13 passed (7 from Task 3 + 6 new).

- [ ] **Step 4.5: Commit**

```bash
git add src/ModeSwitcher.Core/Services/ModeNameSuggester.cs tests/ModeSwitcher.Core.Tests/Services/ModeNameSuggesterTests.cs
git commit -m "feat(core): add ModeNameSuggester.SuggestFromSettings"
```

---

## Task 5: `ModeSaver` — models + `GetCandidates`

**Files:**
- Create: `src/ModeSwitcher.Core/Services/ModeSaver.cs`
- Create: `tests/ModeSwitcher.Core.Tests/Services/ModeSaverTests.cs`

- [ ] **Step 5.1: Write failing tests**

Create `tests/ModeSwitcher.Core.Tests/Services/ModeSaverTests.cs`:

```csharp
using FluentAssertions;
using ModeSwitcher.Core.FileSystem;
using ModeSwitcher.Core.Services;
using NSubstitute;
using Xunit;

namespace ModeSwitcher.Core.Tests.Services;

public class ModeSaverTests
{
    [Fact]
    public void GetCandidates_WithCurrentMode_ReturnsCurrentFilesPlusNewTopLevelFiles()
    {
        // Arrange
        var fsMock = Substitute.For<IFileSystem>();
        var saver = new ModeSaver(fsMock);

        // Current mode files (recursive scan)
        fsMock.DirectoryExists("modes\\current").Returns(true);
        fsMock.GetAllFiles("modes\\current", "*", SearchOption.AllDirectories)
            .Returns(new[] {
                "modes\\current\\settings.json",
                "modes\\current\\agents\\zidan.md"
            });

        // Target top-level files
        fsMock.DirectoryExists("C:\\Target").Returns(true);
        fsMock.GetAllFiles("C:\\Target", "*", SearchOption.TopDirectoryOnly)
            .Returns(new[] {
                "C:\\Target\\settings.json",  // already in current mode
                "C:\\Target\\CLAUDE.md",       // new
                "C:\\Target\\keybindings.json" // new
            });

        // Act
        var result = saver.GetCandidates("C:\\Target", "modes\\current");

        // Assert: 4 entries — 2 in-current (checked), 2 new
        result.Files.Should().HaveCount(4);

        var inCurrent = result.Files.Where(f => f.InCurrentMode).ToList();
        inCurrent.Select(f => f.RelativePath)
            .Should().BeEquivalentTo(new[] { "agents\\zidan.md", "settings.json" });

        var newFiles = result.Files.Where(f => !f.InCurrentMode).ToList();
        newFiles.Select(f => f.RelativePath)
            .Should().BeEquivalentTo(new[] { "CLAUDE.md", "keybindings.json" });

        // Order: in-current first (alphabetical), then new (alphabetical)
        result.Files.Select(f => f.RelativePath).Should().Equal(
            "agents\\zidan.md",
            "settings.json",
            "CLAUDE.md",
            "keybindings.json"
        );
    }

    [Fact]
    public void GetCandidates_NoCurrentMode_ReturnsOnlyTopLevelFiles()
    {
        // Arrange
        var fsMock = Substitute.For<IFileSystem>();
        var saver = new ModeSaver(fsMock);

        fsMock.DirectoryExists("C:\\Target").Returns(true);
        fsMock.GetAllFiles("C:\\Target", "*", SearchOption.TopDirectoryOnly)
            .Returns(new[] {
                "C:\\Target\\settings.json",
                "C:\\Target\\CLAUDE.md"
            });

        // Act
        var result = saver.GetCandidates("C:\\Target", currentModePath: null);

        // Assert
        result.Files.Should().HaveCount(2);
        result.Files.All(f => !f.InCurrentMode).Should().BeTrue();
        result.Files.Select(f => f.RelativePath)
            .Should().BeEquivalentTo(new[] { "CLAUDE.md", "settings.json" });
    }

    [Fact]
    public void GetCandidates_CurrentModePathMissingOnDisk_TreatsAsNoCurrentMode()
    {
        var fsMock = Substitute.For<IFileSystem>();
        var saver = new ModeSaver(fsMock);

        fsMock.DirectoryExists("modes\\gone").Returns(false);
        fsMock.DirectoryExists("C:\\Target").Returns(true);
        fsMock.GetAllFiles("C:\\Target", "*", SearchOption.TopDirectoryOnly)
            .Returns(new[] { "C:\\Target\\settings.json" });

        var result = saver.GetCandidates("C:\\Target", "modes\\gone");

        result.Files.Should().HaveCount(1);
        result.Files[0].InCurrentMode.Should().BeFalse();
    }

    [Fact]
    public void GetCandidates_TargetMissing_ReturnsCurrentModeFilesOnly()
    {
        var fsMock = Substitute.For<IFileSystem>();
        var saver = new ModeSaver(fsMock);

        fsMock.DirectoryExists("modes\\current").Returns(true);
        fsMock.GetAllFiles("modes\\current", "*", SearchOption.AllDirectories)
            .Returns(new[] { "modes\\current\\settings.json" });
        fsMock.DirectoryExists("C:\\Target").Returns(false);

        var result = saver.GetCandidates("C:\\Target", "modes\\current");

        result.Files.Should().HaveCount(1);
        result.Files[0].RelativePath.Should().Be("settings.json");
        result.Files[0].InCurrentMode.Should().BeTrue();
    }
}
```

Note: tests use `SearchOption` directly — add `using System.IO;` if needed.

- [ ] **Step 5.2: Run tests — expect FAIL (ModeSaver not defined)**

```bash
dotnet test src/ModeSwitcher.sln --filter "FullyQualifiedName~ModeSaverTests.GetCandidates"
```

Expected: compile error.

- [ ] **Step 5.3: Implement models + `GetCandidates`**

Create `src/ModeSwitcher.Core/Services/ModeSaver.cs`:

```csharp
using ModeSwitcher.Core.FileSystem;

namespace ModeSwitcher.Core.Services;

public record FileCandidate(string RelativePath, bool InCurrentMode);
public record SaveCandidates(List<FileCandidate> Files);

public class ModeSaver
{
    private readonly IFileSystem _fileSystem;

    public ModeSaver(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public SaveCandidates GetCandidates(string targetPath, string? currentModePath)
    {
        var currentModeRels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (currentModePath is not null && _fileSystem.DirectoryExists(currentModePath))
        {
            foreach (var file in _fileSystem.GetAllFiles(currentModePath, "*", SearchOption.AllDirectories))
            {
                var rel = file.Substring(currentModePath.Length).TrimStart(Path.DirectorySeparatorChar);
                currentModeRels.Add(rel);
            }
        }

        var newFiles = new List<string>();
        if (_fileSystem.DirectoryExists(targetPath))
        {
            foreach (var file in _fileSystem.GetAllFiles(targetPath, "*", SearchOption.TopDirectoryOnly))
            {
                var rel = file.Substring(targetPath.Length).TrimStart(Path.DirectorySeparatorChar);
                if (!currentModeRels.Contains(rel))
                {
                    newFiles.Add(rel);
                }
            }
        }

        var combined = currentModeRels
            .OrderBy(r => r, StringComparer.OrdinalIgnoreCase)
            .Select(r => new FileCandidate(r, true))
            .Concat(newFiles
                .OrderBy(r => r, StringComparer.OrdinalIgnoreCase)
                .Select(r => new FileCandidate(r, false)))
            .ToList();

        return new SaveCandidates(combined);
    }
}
```

- [ ] **Step 5.4: Run tests — expect PASS**

```bash
dotnet test src/ModeSwitcher.sln --filter "FullyQualifiedName~ModeSaverTests.GetCandidates"
```

Expected: 4 passed.

- [ ] **Step 5.5: Commit**

```bash
git add src/ModeSwitcher.Core/Services/ModeSaver.cs tests/ModeSwitcher.Core.Tests/Services/ModeSaverTests.cs
git commit -m "feat(core): add ModeSaver.GetCandidates"
```

---

## Task 6: `ModeSaver.SaveAsync`

**Files:**
- Modify: `src/ModeSwitcher.Core/Services/ModeSaver.cs`
- Modify: `tests/ModeSwitcher.Core.Tests/Services/ModeSaverTests.cs`

- [ ] **Step 6.1: Write failing tests**

Append to `ModeSaverTests`:

```csharp
[Fact]
public async Task SaveAsync_CopiesSelectedFilesPreservingTimestamps()
{
    var fsMock = Substitute.For<IFileSystem>();
    var saver = new ModeSaver(fsMock);

    fsMock.DirectoryExists("modes\\new").Returns(false);
    var ts = new DateTime(2024, 1, 1, 12, 0, 0);
    fsMock.GetLastWriteTime(Arg.Any<string>()).Returns(ts);

    var rels = new[] { "settings.json", "CLAUDE.md" };

    await saver.SaveAsync("C:\\Target", "modes\\new", rels);

    fsMock.Received(1).CreateDirectory("modes\\new");
    fsMock.Received(1).CopyFile("C:\\Target\\settings.json", "modes\\new\\settings.json", true);
    fsMock.Received(1).CopyFile("C:\\Target\\CLAUDE.md", "modes\\new\\CLAUDE.md", true);
    fsMock.Received(1).SetLastWriteTime("modes\\new\\settings.json", ts);
    fsMock.Received(1).SetLastWriteTime("modes\\new\\CLAUDE.md", ts);
}

[Fact]
public async Task SaveAsync_CreatesNestedDirectories()
{
    var fsMock = Substitute.For<IFileSystem>();
    var saver = new ModeSaver(fsMock);

    fsMock.DirectoryExists("modes\\new").Returns(false);
    fsMock.DirectoryExists("modes\\new\\agents").Returns(false);

    await saver.SaveAsync("C:\\Target", "modes\\new", new[] { "agents\\zidan.md" });

    fsMock.Received().CreateDirectory("modes\\new");
    fsMock.Received().CreateDirectory("modes\\new\\agents");
    fsMock.Received(1).CopyFile(
        "C:\\Target\\agents\\zidan.md",
        "modes\\new\\agents\\zidan.md",
        true);
}

[Fact]
public async Task SaveAsync_TargetFolderExists_DoesNotRecreate()
{
    var fsMock = Substitute.For<IFileSystem>();
    var saver = new ModeSaver(fsMock);

    fsMock.DirectoryExists("modes\\new").Returns(true);

    await saver.SaveAsync("C:\\Target", "modes\\new", new[] { "settings.json" });

    fsMock.DidNotReceive().CreateDirectory("modes\\new");
    fsMock.Received(1).CopyFile(
        "C:\\Target\\settings.json",
        "modes\\new\\settings.json",
        true);
}
```

- [ ] **Step 6.2: Run tests — expect FAIL (SaveAsync missing)**

```bash
dotnet test src/ModeSwitcher.sln --filter "FullyQualifiedName~ModeSaverTests.SaveAsync"
```

Expected: compile error.

- [ ] **Step 6.3: Implement `SaveAsync`**

Add inside `ModeSaver` class:

```csharp
public Task SaveAsync(string targetPath, string newModePath, IEnumerable<string> relativePaths)
{
    return Task.Run(() =>
    {
        if (!_fileSystem.DirectoryExists(newModePath))
        {
            _fileSystem.CreateDirectory(newModePath);
        }

        foreach (var rel in relativePaths)
        {
            var source = Path.Combine(targetPath, rel);
            var dest = Path.Combine(newModePath, rel);

            var destDir = Path.GetDirectoryName(dest);
            if (!string.IsNullOrEmpty(destDir) && !_fileSystem.DirectoryExists(destDir))
            {
                _fileSystem.CreateDirectory(destDir);
            }

            _fileSystem.CopyFile(source, dest, overwrite: true);
            var ts = _fileSystem.GetLastWriteTime(source);
            _fileSystem.SetLastWriteTime(dest, ts);
        }
    });
}
```

- [ ] **Step 6.4: Run tests — expect PASS**

```bash
dotnet test src/ModeSwitcher.sln --filter "FullyQualifiedName~ModeSaverTests"
```

Expected: 7 passed (4 from Task 5 + 3 new).

- [ ] **Step 6.5: Commit**

```bash
git add src/ModeSwitcher.Core/Services/ModeSaver.cs tests/ModeSwitcher.Core.Tests/Services/ModeSaverTests.cs
git commit -m "feat(core): add ModeSaver.SaveAsync"
```

---

## Task 7: `ICodeSwitcher.Reload`

**Files:**
- Modify: `src/ModeSwitcher.Core/IModeSwitcher.cs`
- Modify: `src/ModeSwitcher.Core/ModeSwitcher.cs`
- Modify: `tests/ModeSwitcher.Core.Tests/ModeSwitcherTests.cs`

- [ ] **Step 7.1: Write failing test**

Append to `CodeSwitcherTests`:

```csharp
[Fact]
public void Reload_AfterFirstLoad_RereadsConfigFromDisk()
{
    // Arrange: two different configs on consecutive reads
    var fsMock = Substitute.For<IFileSystem>();
    var configLoader = new ConfigLoader(fsMock);
    var fileComparer = new FileComparer(fsMock);
    var fileCopier = new FileCopier(fsMock);

    var configV1 = new SwitcherConfig
    {
        TargetPath = "C:\\Target",
        Modes = new List<ModeDefinition> { new() { Name = "A", Folder = "a" } }
    };
    var configV2 = new SwitcherConfig
    {
        TargetPath = "C:\\Target",
        Modes = new List<ModeDefinition>
        {
            new() { Name = "A", Folder = "a" },
            new() { Name = "B", Folder = "b" }
        }
    };

    var jsonV1 = System.Text.Json.JsonSerializer.Serialize(configV1);
    var jsonV2 = System.Text.Json.JsonSerializer.Serialize(configV2);

    fsMock.OpenRead(Arg.Any<string>()).Returns(
        _ => new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonV1)),
        _ => new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonV2))
    );

    var switcher = new ModeSwitcher.Core.CodeSwitcher("test.json", fsMock, configLoader, fileComparer, fileCopier);

    // Act
    var before = switcher.GetModes();   // reads v1, caches
    switcher.Reload();
    var after = switcher.GetModes();    // re-reads v2

    // Assert
    before.Should().HaveCount(1);
    after.Should().HaveCount(2);
}
```

- [ ] **Step 7.2: Run test — expect FAIL (Reload missing)**

```bash
dotnet test src/ModeSwitcher.sln --filter "FullyQualifiedName~CodeSwitcherTests.Reload"
```

Expected: compile error.

- [ ] **Step 7.3: Add `Reload` to interface and implementation**

In `src/ModeSwitcher.Core/IModeSwitcher.cs`, inside the interface:

```csharp
void Reload();
```

In `src/ModeSwitcher.Core/ModeSwitcher.cs`, inside the `CodeSwitcher` class:

```csharp
public void Reload()
{
    _cachedConfig = null;
}
```

- [ ] **Step 7.4: Run test — expect PASS**

```bash
dotnet test src/ModeSwitcher.sln --filter "FullyQualifiedName~CodeSwitcherTests.Reload"
```

Expected: 1 passed.

- [ ] **Step 7.5: Commit**

```bash
git add src/ModeSwitcher.Core/IModeSwitcher.cs src/ModeSwitcher.Core/ModeSwitcher.cs tests/ModeSwitcher.Core.Tests/ModeSwitcherTests.cs
git commit -m "feat(core): add ICodeSwitcher.Reload"
```

---

## Task 8: `ICodeSwitcher.SaveCurrentAsModeAsync` — wiring

**Files:**
- Modify: `src/ModeSwitcher.Core/IModeSwitcher.cs`
- Modify: `src/ModeSwitcher.Core/ModeSwitcher.cs`
- Modify: `tests/ModeSwitcher.Core.Tests/ModeSwitcherTests.cs`

- [ ] **Step 8.1: Write failing tests**

Append to `CodeSwitcherTests`:

```csharp
[Fact]
public async Task SaveCurrentAsModeAsync_NewMode_AddsDefinitionAndPersistsConfig()
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
        Modes = new List<ModeDefinition> { new() { Name = "Existing", Folder = "existing" } }
    };

    fsMock.OpenRead(Arg.Any<string>())
        .Returns(_ => new MemoryStream(System.Text.Encoding.UTF8.GetBytes(
            System.Text.Json.JsonSerializer.Serialize(initial))));

    var writeStream = new MemoryStream();
    fsMock.OpenWrite("test.json.tmp").Returns(writeStream);

    var switcher = new ModeSwitcher.Core.CodeSwitcher(
        "test.json", fsMock, configLoader, fileComparer, fileCopier, modeSaver, configWriter);

    // Act
    await switcher.SaveCurrentAsModeAsync(
        modeName: "NewMode",
        folderName: "newmode",
        relativePaths: new[] { "settings.json" },
        overwrite: false);

    // Assert
    var saved = System.Text.Json.JsonSerializer.Deserialize<SwitcherConfig>(writeStream.ToArray());
    saved!.Modes.Should().HaveCount(2);
    saved.Modes.Last().Name.Should().Be("NewMode");
    saved.Modes.Last().Folder.Should().Be("newmode");

    fsMock.Received().CopyFile(
        "C:\\Target\\settings.json",
        Path.Combine(Path.GetDirectoryName("test.json")!, "modes", "newmode", "settings.json"),
        true);
}

[Fact]
public async Task SaveCurrentAsModeAsync_OverwriteByName_UpdatesExistingDefinitionAndDeletesFolderFirst()
{
    var fsMock = Substitute.For<IFileSystem>();
    var configLoader = new ConfigLoader(fsMock);
    var fileComparer = new FileComparer(fsMock);
    var fileCopier = new FileCopier(fsMock);
    var modeSaver = new ModeSaver(fsMock);
    var configWriter = new ConfigWriter(fsMock);

    var initial = new SwitcherConfig
    {
        TargetPath = "C:\\Target",
        Modes = new List<ModeDefinition> { new() { Name = "Existing", Folder = "oldfolder" } }
    };

    fsMock.OpenRead(Arg.Any<string>())
        .Returns(_ => new MemoryStream(System.Text.Encoding.UTF8.GetBytes(
            System.Text.Json.JsonSerializer.Serialize(initial))));

    var writeStream = new MemoryStream();
    fsMock.OpenWrite("test.json.tmp").Returns(writeStream);

    var newFolderPath = Path.Combine(Path.GetDirectoryName("test.json")!, "modes", "newfolder");
    fsMock.DirectoryExists(newFolderPath).Returns(true);

    var switcher = new ModeSwitcher.Core.CodeSwitcher(
        "test.json", fsMock, configLoader, fileComparer, fileCopier, modeSaver, configWriter);

    // Act
    await switcher.SaveCurrentAsModeAsync(
        modeName: "Existing",
        folderName: "newfolder",
        relativePaths: new[] { "settings.json" },
        overwrite: true);

    // Assert: still one mode, folder updated, old target folder wiped
    var saved = System.Text.Json.JsonSerializer.Deserialize<SwitcherConfig>(writeStream.ToArray());
    saved!.Modes.Should().HaveCount(1);
    saved.Modes[0].Name.Should().Be("Existing");
    saved.Modes[0].Folder.Should().Be("newfolder");

    fsMock.Received(1).DeleteDirectory(newFolderPath, true);
}

[Fact]
public async Task SaveCurrentAsModeAsync_InvalidatesConfigCache()
{
    var fsMock = Substitute.For<IFileSystem>();
    var configLoader = new ConfigLoader(fsMock);
    var fileComparer = new FileComparer(fsMock);
    var fileCopier = new FileCopier(fsMock);
    var modeSaver = new ModeSaver(fsMock);
    var configWriter = new ConfigWriter(fsMock);

    var initial = new SwitcherConfig
    {
        TargetPath = "C:\\Target",
        Modes = new List<ModeDefinition>()
    };
    var jsonInitial = System.Text.Json.JsonSerializer.Serialize(initial);

    // Each OpenRead call returns a fresh stream
    fsMock.OpenRead(Arg.Any<string>()).Returns(
        _ => new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonInitial)));

    var writeStream = new MemoryStream();
    fsMock.OpenWrite("test.json.tmp").Returns(writeStream);

    var switcher = new ModeSwitcher.Core.CodeSwitcher(
        "test.json", fsMock, configLoader, fileComparer, fileCopier, modeSaver, configWriter);

    _ = switcher.GetModes(); // primes cache
    await switcher.SaveCurrentAsModeAsync("X", "x", Array.Empty<string>(), overwrite: false);

    // After save, GetModes should re-read config (>= 2 reads total)
    _ = switcher.GetModes();

    fsMock.Received().OpenRead(Arg.Any<string>());
    // At least 2 reads: one before save, one after Reload triggered by save
    fsMock.ReceivedCalls()
        .Count(c => c.GetMethodInfo().Name == nameof(IFileSystem.OpenRead))
        .Should().BeGreaterThanOrEqualTo(2);
}
```

- [ ] **Step 8.2: Run tests — expect FAIL**

```bash
dotnet test src/ModeSwitcher.sln --filter "FullyQualifiedName~CodeSwitcherTests.SaveCurrentAsModeAsync"
```

Expected: compile errors — missing method, missing constructor params.

- [ ] **Step 8.3: Extend interface**

In `src/ModeSwitcher.Core/IModeSwitcher.cs`:

```csharp
Task SaveCurrentAsModeAsync(string modeName, string folderName, IEnumerable<string> relativePaths, bool overwrite);
```

- [ ] **Step 8.4: Extend `CodeSwitcher` constructor and implement method**

In `src/ModeSwitcher.Core/ModeSwitcher.cs`, update the internal constructor and add fields:

```csharp
private readonly ModeSaver _modeSaver;
private readonly ConfigWriter _configWriter;

internal CodeSwitcher(
    string configPath,
    IFileSystem fileSystem,
    ConfigLoader? configLoader = null,
    FileComparer? fileComparer = null,
    FileCopier? fileCopier = null,
    ModeSaver? modeSaver = null,
    ConfigWriter? configWriter = null)
{
    _configPath = configPath;
    _modesBasePath = Path.Combine(Path.GetDirectoryName(configPath)!, "modes");
    _configLoader = configLoader ?? new ConfigLoader(fileSystem);
    _fileComparer = fileComparer ?? new FileComparer(fileSystem);
    _fileCopier = fileCopier ?? new FileCopier(fileSystem);
    _modeSaver = modeSaver ?? new ModeSaver(fileSystem);
    _configWriter = configWriter ?? new ConfigWriter(fileSystem);
    _fileSystem = fileSystem;
}
```

Also add the field at the top of the class:

```csharp
private readonly IFileSystem _fileSystem;
```

(Needed for `DeleteDirectory` on overwrite.)

Update the public `CodeSwitcher(string configPath)` constructor — no change needed, it chains through `new RealFileSystem()`.

Add method:

```csharp
public async Task SaveCurrentAsModeAsync(
    string modeName,
    string folderName,
    IEnumerable<string> relativePaths,
    bool overwrite)
{
    var config = LoadConfig();
    if (config is null)
    {
        throw new InvalidOperationException("Config could not be loaded.");
    }

    var newModePath = Path.Combine(_modesBasePath, folderName);

    if (overwrite && _fileSystem.DirectoryExists(newModePath))
    {
        _fileSystem.DeleteDirectory(newModePath, recursive: true);
    }

    await _modeSaver.SaveAsync(config.TargetPath, newModePath, relativePaths);

    var existing = config.Modes.FirstOrDefault(m => m.Name == modeName);
    if (existing is not null)
    {
        existing.Folder = folderName;
    }
    else
    {
        config.Modes.Add(new ModeDefinition { Name = modeName, Folder = folderName });
    }

    _configWriter.Save(_configPath, config);
    Reload();
}
```

- [ ] **Step 8.5: Run tests — expect PASS**

```bash
dotnet test src/ModeSwitcher.sln --filter "FullyQualifiedName~CodeSwitcherTests.SaveCurrentAsModeAsync"
```

Expected: 3 passed.

- [ ] **Step 8.6: Run full Core test suite — expect all green**

```bash
dotnet test src/ModeSwitcher.sln
```

Expected: existing tests still pass, no regressions.

- [ ] **Step 8.7: Commit**

```bash
git add src/ModeSwitcher.Core/IModeSwitcher.cs src/ModeSwitcher.Core/ModeSwitcher.cs tests/ModeSwitcher.Core.Tests/ModeSwitcherTests.cs
git commit -m "feat(core): add SaveCurrentAsModeAsync"
```

---

## Task 9: UI — make mode list scroll and form resizable

**Files:**
- Modify: `src/ModeSwitcher.UI/MainForm.Designer.cs`
- Modify: `src/ModeSwitcher.UI/MainForm.cs`

- [ ] **Step 9.1: Update Designer**

In `src/ModeSwitcher.UI/MainForm.Designer.cs`:

Replace the `pnlModes` block ([MainForm.Designer.cs:49-52](../../../src/ModeSwitcher.UI/MainForm.Designer.cs#L49-L52)):

```csharp
// pnlModes
this.pnlModes.Location = new Point(20, 60);
this.pnlModes.Name = "pnlModes";
this.pnlModes.Size = new Size(360, 150);
this.pnlModes.BorderStyle = BorderStyle.FixedSingle;
this.pnlModes.AutoScroll = true;
this.pnlModes.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
```

Replace the `lblCurrentMode` block:

```csharp
// lblCurrentMode
this.lblCurrentMode.AutoSize = true;
this.lblCurrentMode.Location = new Point(20, 20);
this.lblCurrentMode.Name = "lblCurrentMode";
this.lblCurrentMode.Size = new Size(150, 20);
this.lblCurrentMode.Text = "Текущий режим: ...";
this.lblCurrentMode.Anchor = AnchorStyles.Top | AnchorStyles.Left;
```

Replace each button block (`btnApply`, `btnRefresh`, `btnAbout`, `btnExit`) — add `Anchor = AnchorStyles.Bottom | AnchorStyles.Left`. Example for `btnApply`:

```csharp
// btnApply
this.btnApply.Location = new Point(20, 230);
this.btnApply.Name = "btnApply";
this.btnApply.Size = new Size(200, 35);
this.btnApply.Text = "Применить выбранный режим";
this.btnApply.UseVisualStyleBackColor = true;
this.btnApply.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
this.btnApply.Click += new EventHandler(this.BtnApply_Click);
```

Do the same for `btnRefresh`, `btnAbout`, `btnExit` — each gets `this.<btn>.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;` added before the `Click` line.

Replace the `MainForm` block:

```csharp
// MainForm
this.Icon = new Icon(typeof(MainForm).Assembly.GetManifestResourceStream("ModeSwitcher.UI.AppIcon.ico") ?? throw new InvalidOperationException("Icon not found"));
this.ClientSize = new Size(400, 350);
this.MinimumSize = new Size(360, 300);
this.Controls.Add(this.lblCurrentMode);
this.Controls.Add(this.pnlModes);
this.Controls.Add(this.btnApply);
this.Controls.Add(this.btnRefresh);
this.Controls.Add(this.btnAbout);
this.Controls.Add(this.btnExit);
this.Controls.Add(this.statusStrip);
this.FormBorderStyle = FormBorderStyle.Sizable;
this.MaximizeBox = true;
this.Name = "MainForm";
this.StartPosition = FormStartPosition.CenterScreen;
this.Text = "Code Switcher v1.0";
```

(Changes vs. existing: `FormBorderStyle = Sizable`, `MaximizeBox = true`, added `MinimumSize`.)

- [ ] **Step 9.2: Make radio width responsive**

In `src/ModeSwitcher.UI/MainForm.cs`, update `RenderModes` ([MainForm.cs:113-120](../../../src/ModeSwitcher.UI/MainForm.cs#L113-L120)):

```csharp
var radio = new RadioButton
{
    Text = mode.IsActive ? $"{displayName} (активен)" : displayName,
    Location = new Point(10, y),
    Width = pnlModes.ClientSize.Width - 30,  // account for scrollbar when visible
    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
    Checked = mode.IsActive,
    Tag = mode,
    Font = new Font("Segoe UI", 10F, mode.IsActive ? FontStyle.Bold : FontStyle.Regular)
};
```

- [ ] **Step 9.3: Build and run manually**

```bash
dotnet build src/ModeSwitcher.sln
```

Expected: builds clean.

Then run the UI:

```bash
dotnet run --project src/ModeSwitcher.UI/ModeSwitcher.UI.csproj
```

Manual checks:
- Window can be resized by dragging its edges.
- When you shrink the form so the mode panel can't fit all 5 modes, a scrollbar appears on the right.
- When you enlarge the form, the mode panel grows.
- All buttons stick to the bottom of the window when resized.

Close the app.

- [ ] **Step 9.4: Commit**

```bash
git add src/ModeSwitcher.UI/MainForm.Designer.cs src/ModeSwitcher.UI/MainForm.cs
git commit -m "feat(ui): make MainForm resizable with scrollable mode list"
```

---

## Task 10: UI — `SaveCurrentModeDialog`

**Files:**
- Create: `src/ModeSwitcher.UI/SaveCurrentModeDialog.cs`
- Create: `src/ModeSwitcher.UI/SaveCurrentModeDialog.Designer.cs`

- [ ] **Step 10.1: Create Designer file**

Create `src/ModeSwitcher.UI/SaveCurrentModeDialog.Designer.cs`:

```csharp
#nullable disable
namespace ModeSwitcher.UI;

partial class SaveCurrentModeDialog
{
    private System.ComponentModel.IContainer components = null;
    private Label lblCurrentMode;
    private Label lblName;
    private TextBox txtName;
    private Label lblFolder;
    private TextBox txtFolder;
    private Label lblFiles;
    private CheckedListBox clbFiles;
    private Label lblLegend;
    private Label lblError;
    private Button btnOk;
    private Button btnCancel;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null)) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();
        this.lblCurrentMode = new Label();
        this.lblName = new Label();
        this.txtName = new TextBox();
        this.lblFolder = new Label();
        this.txtFolder = new TextBox();
        this.lblFiles = new Label();
        this.clbFiles = new CheckedListBox();
        this.lblLegend = new Label();
        this.lblError = new Label();
        this.btnOk = new Button();
        this.btnCancel = new Button();
        this.SuspendLayout();

        // lblCurrentMode
        this.lblCurrentMode.AutoSize = true;
        this.lblCurrentMode.Location = new Point(15, 15);
        this.lblCurrentMode.Text = "Активный мод: —";

        // lblName
        this.lblName.AutoSize = true;
        this.lblName.Location = new Point(15, 50);
        this.lblName.Text = "Имя:";

        // txtName
        this.txtName.Location = new Point(80, 47);
        this.txtName.Size = new Size(385, 23);
        this.txtName.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        // lblFolder
        this.lblFolder.AutoSize = true;
        this.lblFolder.Location = new Point(15, 80);
        this.lblFolder.Text = "Папка:";

        // txtFolder
        this.txtFolder.Location = new Point(80, 77);
        this.txtFolder.Size = new Size(385, 23);
        this.txtFolder.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        // lblFiles
        this.lblFiles.AutoSize = true;
        this.lblFiles.Location = new Point(15, 115);
        this.lblFiles.Text = "Файлы:";

        // clbFiles
        this.clbFiles.Location = new Point(15, 135);
        this.clbFiles.Size = new Size(450, 180);
        this.clbFiles.CheckOnClick = true;
        this.clbFiles.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        // lblLegend
        this.lblLegend.AutoSize = true;
        this.lblLegend.Location = new Point(15, 320);
        this.lblLegend.Text = "✓ — уже входит в активный мод";
        this.lblLegend.ForeColor = Color.Gray;
        this.lblLegend.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

        // lblError
        this.lblError.AutoSize = true;
        this.lblError.Location = new Point(15, 345);
        this.lblError.Text = "";
        this.lblError.ForeColor = Color.Red;
        this.lblError.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

        // btnOk
        this.btnOk.Location = new Point(290, 370);
        this.btnOk.Size = new Size(85, 30);
        this.btnOk.Text = "Сохранить";
        this.btnOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        this.btnOk.Click += new EventHandler(this.BtnOk_Click);

        // btnCancel
        this.btnCancel.Location = new Point(385, 370);
        this.btnCancel.Size = new Size(80, 30);
        this.btnCancel.Text = "Отмена";
        this.btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        this.btnCancel.DialogResult = DialogResult.Cancel;

        // dialog
        this.ClientSize = new Size(480, 415);
        this.MinimumSize = new Size(460, 400);
        this.FormBorderStyle = FormBorderStyle.Sizable;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.ShowInTaskbar = false;
        this.StartPosition = FormStartPosition.CenterParent;
        this.Text = "Сохранить текущий режим";
        this.AcceptButton = this.btnOk;
        this.CancelButton = this.btnCancel;
        this.Controls.Add(this.lblCurrentMode);
        this.Controls.Add(this.lblName);
        this.Controls.Add(this.txtName);
        this.Controls.Add(this.lblFolder);
        this.Controls.Add(this.txtFolder);
        this.Controls.Add(this.lblFiles);
        this.Controls.Add(this.clbFiles);
        this.Controls.Add(this.lblLegend);
        this.Controls.Add(this.lblError);
        this.Controls.Add(this.btnOk);
        this.Controls.Add(this.btnCancel);
        this.ResumeLayout(false);
        this.PerformLayout();
    }
}
```

- [ ] **Step 10.2: Create dialog code-behind**

Create `src/ModeSwitcher.UI/SaveCurrentModeDialog.cs`:

```csharp
using System.Text.RegularExpressions;
using ModeSwitcher.Core.Services;

namespace ModeSwitcher.UI;

public partial class SaveCurrentModeDialog : Form
{
    private static readonly Regex ValidFolder = new(@"^[A-Za-z0-9._\-]+$", RegexOptions.Compiled);

    private readonly HashSet<string> _existingNames;
    private readonly HashSet<string> _existingFolders;
    private bool _folderManuallyEdited;
    private bool _syncing;

    public string ModeName => txtName!.Text.Trim();
    public string FolderName => txtFolder!.Text.Trim();
    public IReadOnlyList<string> SelectedRelativePaths { get; private set; } = Array.Empty<string>();
    public bool OverwriteRequested { get; private set; }

    public SaveCurrentModeDialog(
        SaveCandidates candidates,
        string? suggestedName,
        string? currentModeDisplayName,
        IEnumerable<string> existingNames,
        IEnumerable<string> existingFolders)
    {
        InitializeComponent();

        _existingNames = new HashSet<string>(existingNames, StringComparer.OrdinalIgnoreCase);
        _existingFolders = new HashSet<string>(existingFolders, StringComparer.OrdinalIgnoreCase);

        lblCurrentMode!.Text = currentModeDisplayName is null
            ? "Активный мод: —"
            : $"Активный мод: {currentModeDisplayName}";

        txtName!.Text = suggestedName ?? "";
        txtFolder!.Text = ModeNameSuggester.ToFolderName(txtName.Text);

        foreach (var file in candidates.Files)
        {
            var label = file.InCurrentMode ? file.RelativePath : $"{file.RelativePath}    (новый)";
            clbFiles!.Items.Add(new FileItem(file.RelativePath, label), file.InCurrentMode);
        }
        clbFiles!.DisplayMember = nameof(FileItem.Label);

        txtName.TextChanged += (s, e) =>
        {
            if (_syncing) return;
            if (_folderManuallyEdited) return;
            _syncing = true;
            txtFolder.Text = ModeNameSuggester.ToFolderName(txtName.Text);
            _syncing = false;
        };

        txtFolder.TextChanged += (s, e) =>
        {
            if (_syncing) return;
            _folderManuallyEdited = true;
        };
    }

    private void BtnOk_Click(object? sender, EventArgs e)
    {
        lblError!.Text = "";

        if (string.IsNullOrWhiteSpace(ModeName))
        {
            lblError.Text = "Введите имя режима.";
            txtName!.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(FolderName))
        {
            lblError.Text = "Введите имя папки (только латиница, цифры, . _ -).";
            txtFolder!.Focus();
            return;
        }

        if (!ValidFolder.IsMatch(FolderName))
        {
            lblError.Text = "В имени папки разрешены только: A-Z a-z 0-9 . _ -";
            txtFolder!.Focus();
            return;
        }

        var selected = new List<string>();
        foreach (var item in clbFiles!.CheckedItems)
        {
            selected.Add(((FileItem)item).RelativePath);
        }
        if (selected.Count == 0)
        {
            lblError.Text = "Выберите хотя бы один файл.";
            return;
        }
        SelectedRelativePaths = selected;

        var nameConflict = _existingNames.Contains(ModeName);
        var folderConflict = _existingFolders.Contains(FolderName);

        if (nameConflict || folderConflict)
        {
            var msg = $"Режим \"{ModeName}\" или папка \"{FolderName}\" уже существует.\n\n" +
                      "Да — перезаписать\nНет — изменить имя\nОтмена — отменить";
            var choice = MessageBox.Show(this, msg, "Конфликт",
                MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);

            switch (choice)
            {
                case DialogResult.Yes:
                    OverwriteRequested = true;
                    DialogResult = DialogResult.OK;
                    Close();
                    return;
                case DialogResult.No:
                    txtName!.Focus();
                    return;
                default:
                    DialogResult = DialogResult.Cancel;
                    Close();
                    return;
            }
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    private record FileItem(string RelativePath, string Label);
}
```

- [ ] **Step 10.3: Build to verify it compiles**

```bash
dotnet build src/ModeSwitcher.sln
```

Expected: builds clean (dialog not yet wired into MainForm, but compiles standalone).

- [ ] **Step 10.4: Commit**

```bash
git add src/ModeSwitcher.UI/SaveCurrentModeDialog.cs src/ModeSwitcher.UI/SaveCurrentModeDialog.Designer.cs
git commit -m "feat(ui): add SaveCurrentModeDialog"
```

---

## Task 11: UI — wire "Сохранить текущий…" button into MainForm

**Files:**
- Modify: `src/ModeSwitcher.UI/MainForm.Designer.cs`
- Modify: `src/ModeSwitcher.UI/MainForm.cs`

- [ ] **Step 11.1: Add the button to Designer**

In `src/ModeSwitcher.UI/MainForm.Designer.cs`:

Add a field declaration near the other buttons:

```csharp
private Button? btnSaveCurrent;
```

In `InitializeComponent()`, after the `btnApply` block, add:

```csharp
// btnSaveCurrent
this.btnSaveCurrent = new Button();
this.btnSaveCurrent.Location = new Point(230, 230);
this.btnSaveCurrent.Name = "btnSaveCurrent";
this.btnSaveCurrent.Size = new Size(150, 35);
this.btnSaveCurrent.Text = "Сохранить текущий…";
this.btnSaveCurrent.UseVisualStyleBackColor = true;
this.btnSaveCurrent.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
this.btnSaveCurrent.Click += new EventHandler(this.BtnSaveCurrent_Click);
```

Add to `Controls.Add(...)` list after `this.Controls.Add(this.btnApply);`:

```csharp
this.Controls.Add(this.btnSaveCurrent);
```

- [ ] **Step 11.2: Implement the click handler in MainForm**

In `src/ModeSwitcher.UI/MainForm.cs`, add this method (after `BtnApply_Click`):

```csharp
private async void BtnSaveCurrent_Click(object? sender, EventArgs e)
{
    try
    {
        // Build inputs for the dialog
        var current = _switcher.DetectCurrentMode();
        var config = LoadConfigOrNull();
        if (config is null)
        {
            MessageBox.Show("Конфиг не загружен.", "Ошибка",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var modesBasePath = Path.Combine(Path.GetDirectoryName(_switcher.ConfigPath)!, "modes");
        string? currentModePath = null;
        if (current is not null)
        {
            var def = config.Modes.FirstOrDefault(m => m.Name == current.ModeName);
            if (def is not null)
            {
                currentModePath = Path.Combine(modesBasePath, def.Folder);
            }
        }

        var modeSaver = new ModeSaver(new RealFileSystem());
        var candidates = modeSaver.GetCandidates(config.TargetPath, currentModePath);

        var settingsPath = Path.Combine(config.TargetPath, "settings.json");
        var suggestedName = ModeNameSuggester.SuggestFromSettings(settingsPath, new RealFileSystem());

        using var dialog = new SaveCurrentModeDialog(
            candidates,
            suggestedName,
            currentModeDisplayName: current?.ModeName,
            existingNames: config.Modes.Select(m => m.Name),
            existingFolders: config.Modes.Select(m => m.Folder));

        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        SetStatus("Сохранение режима...");
        await _switcher.SaveCurrentAsModeAsync(
            dialog.ModeName,
            dialog.FolderName,
            dialog.SelectedRelativePaths,
            dialog.OverwriteRequested);

        SetStatus($"Режим \"{dialog.ModeName}\" сохранён.");
        LoadData();
    }
    catch (Exception ex)
    {
        SetStatus($"Ошибка: {ex.Message}");
        MessageBox.Show($"Не удалось сохранить режим:\n{ex.Message}", "Ошибка",
            MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}

private SwitcherConfig? LoadConfigOrNull()
{
    try
    {
        var loader = new ConfigLoader(new RealFileSystem());
        return loader.Load(_switcher.ConfigPath);
    }
    catch
    {
        return null;
    }
}
```

Add the needed using directives at the top of `MainForm.cs`:

```csharp
using ModeSwitcher.Core.FileSystem;
using ModeSwitcher.Core.Services;
```

(`ModeSwitcher.Core.Models` is already imported.)

- [ ] **Step 11.3: Build**

```bash
dotnet build src/ModeSwitcher.sln
```

Expected: builds clean.

- [ ] **Step 11.4: Run app and smoke-test the full flow**

```bash
dotnet run --project src/ModeSwitcher.UI/ModeSwitcher.UI.csproj
```

Manual checks (golden path):
1. Click "Сохранить текущий…" — dialog opens.
2. "Активный мод" label shows the current mode (or "—" if none).
3. Name field is pre-filled like `localhost (qwen2.5-coder:14b)` if `settings.json` has those fields.
4. Folder field follows: `localhost_qwen2.5-coder_14b`.
5. Files list shows current-mode files (checked) and any new top-level files (unchecked).
6. Editing Name updates Folder; editing Folder stops the sync.
7. Click "Сохранить" — new mode appears in the list, status bar shows "Режим X сохранён."
8. Restart the app — new mode persists in `modeswitcher.json` and appears at startup.

Edge cases:
9. Empty name → red error label, dialog stays open.
10. Folder with bad chars (e.g. `bad name!`) → red error.
11. No files checked → red error.
12. Duplicate name → `YesNoCancel` MessageBox; pick "Yes" → overwrites existing folder + updates config in place; pick "No" → returns to name field; pick "Cancel" → closes.

- [ ] **Step 11.5: Commit**

```bash
git add src/ModeSwitcher.UI/MainForm.Designer.cs src/ModeSwitcher.UI/MainForm.cs
git commit -m "feat(ui): wire SaveCurrentModeDialog into MainForm"
```

---

## Task 12: Final verification

- [ ] **Step 12.1: Run all tests**

```bash
dotnet test src/ModeSwitcher.sln
```

Expected: all tests pass (existing + new).

- [ ] **Step 12.2: Verify integration tests still pass / are still skipped as configured**

(Per recent commit `643ebf5`: integration tests are skipped in CI but should run locally if not in CI env. Check the test output for any unexpected failures.)

- [ ] **Step 12.3: Smoke test in the production build**

```bash
dotnet publish src/ModeSwitcher.UI/ModeSwitcher.UI.csproj -c Release -o publish-test
```

Run `publish-test/ModeSwitcher.UI.exe`, repeat checks from Step 11.4.

- [ ] **Step 12.4: Clean up**

```bash
rm -rf publish-test
```

(Do NOT touch `publish/`, `temp-publish/`, `temp-publish2/` — those are pre-existing.)

---

## Summary of files touched

**Created:**
- `src/ModeSwitcher.Core/Services/ConfigWriter.cs`
- `src/ModeSwitcher.Core/Services/ModeSaver.cs`
- `src/ModeSwitcher.Core/Services/ModeNameSuggester.cs`
- `src/ModeSwitcher.UI/SaveCurrentModeDialog.cs`
- `src/ModeSwitcher.UI/SaveCurrentModeDialog.Designer.cs`
- `tests/ModeSwitcher.Core.Tests/Services/ConfigWriterTests.cs`
- `tests/ModeSwitcher.Core.Tests/Services/ModeSaverTests.cs`
- `tests/ModeSwitcher.Core.Tests/Services/ModeNameSuggesterTests.cs`

**Modified:**
- `src/ModeSwitcher.Core/FileSystem/IFileSystem.cs`
- `src/ModeSwitcher.Core/FileSystem/RealFileSystem.cs`
- `src/ModeSwitcher.Core/IModeSwitcher.cs`
- `src/ModeSwitcher.Core/ModeSwitcher.cs`
- `src/ModeSwitcher.UI/MainForm.cs`
- `src/ModeSwitcher.UI/MainForm.Designer.cs`
- `tests/ModeSwitcher.Core.Tests/FileSystemTests.cs`
- `tests/ModeSwitcher.Core.Tests/ModeSwitcherTests.cs`
