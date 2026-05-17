using FluentAssertions;
using ModeSwitcher.Core;
using ModeSwitcher.Core.FileSystem;
using ModeSwitcher.Core.Models;
using ModeSwitcher.Core.Services;
using NSubstitute;
using Xunit;

namespace ModeSwitcher.Core.Tests;

public class CodeSwitcherTests
{
    [Fact]
    public void GetModes_WithValidConfig_ReturnsAllModes()
    {
        // Arrange
        var fsMock = Substitute.For<IFileSystem>();
        var configLoader = new ConfigLoader(fsMock);
        var fileComparer = new FileComparer(fsMock);
        var fileCopier = new FileCopier(fsMock);

        var config = new SwitcherConfig
        {
            TargetPath = "C:\\Target",
            Modes = new List<ModeDefinition>
            {
                new() { Name = "Prod", Folder = "prod" },
                new() { Name = "Dev", Folder = "dev" }
            }
        };

        // Setup config file to return valid config
        var configJson = System.Text.Json.JsonSerializer.Serialize(config);
        var configStream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(configJson));
        fsMock.OpenRead(Arg.Any<string>()).Returns(configStream);

        // Setup file comparer to detect Prod mode
        fsMock.DirectoryExists("C:\\Target").Returns(true);
        fsMock.DirectoryExists(Arg.Any<string>()).Returns(true);
        fsMock.GetAllFiles("C:\\Target", "*", System.IO.SearchOption.AllDirectories)
            .Returns(new[] { "C:\\Target\\test.txt" });
        fsMock.GetAllFiles(Arg.Is<string>(p => p.Contains("prod")), "*", System.IO.SearchOption.AllDirectories)
            .Returns(new[] { "modes\\prod\\test.txt" });
        fsMock.GetAllFiles(Arg.Is<string>(p => p.Contains("dev")), "*", System.IO.SearchOption.AllDirectories)
            .Returns(new[] { "modes\\dev\\test.txt" });
        fsMock.GetFileSize(Arg.Any<string>()).Returns(100L);
        fsMock.GetLastWriteTime(Arg.Any<string>()).Returns(DateTime.Now);

        var switcher = new ModeSwitcher.Core.CodeSwitcher("test.json", fsMock, configLoader, fileComparer, fileCopier);

        // Act
        var modes = switcher.GetModes();

        // Assert
        modes.Should().HaveCount(2);
        modes.Any(m => m.Name == "Prod").Should().BeTrue();
        modes.Any(m => m.Name == "Dev").Should().BeTrue();
    }

    [Fact]
    public void GetModes_WithInvalidConfig_ReturnsEmptyList()
    {
        // Arrange
        var fsMock = Substitute.For<IFileSystem>();
        var configLoader = new ConfigLoader(fsMock);
        var fileComparer = new FileComparer(fsMock);
        var fileCopier = new FileCopier(fsMock);

        // Setup config file to return null (invalid JSON or file not found)
        fsMock.OpenRead(Arg.Any<string>()).Returns((System.IO.Stream?)null);

        var switcher = new ModeSwitcher.Core.CodeSwitcher("test.json", fsMock, configLoader, fileComparer, fileCopier);

        // Act
        var modes = switcher.GetModes();

        // Assert
        modes.Should().BeEmpty();
    }

    [Fact]
    public void DetectCurrentMode_WithMatchingMode_ReturnsModeName()
    {
        // Arrange
        var fsMock = Substitute.For<IFileSystem>();
        var configLoader = new ConfigLoader(fsMock);
        var fileComparer = new FileComparer(fsMock);
        var fileCopier = new FileCopier(fsMock);

        var config = new SwitcherConfig
        {
            TargetPath = "C:\\Target",
            Modes = new List<ModeDefinition>
            {
                new() { Name = "Prod", Folder = "prod" }
            }
        };

        var configJson = System.Text.Json.JsonSerializer.Serialize(config);
        var configStream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(configJson));
        fsMock.OpenRead(Arg.Any<string>()).Returns(configStream);

        fsMock.DirectoryExists("C:\\Target").Returns(true);
        fsMock.DirectoryExists("modes\\prod").Returns(true);
        fsMock.GetAllFiles("C:\\Target", "*", System.IO.SearchOption.AllDirectories)
            .Returns(new[] { "C:\\Target\\test.txt" });
        fsMock.GetAllFiles("modes\\prod", "*", System.IO.SearchOption.AllDirectories)
            .Returns(new[] { "modes\\prod\\test.txt" });
        fsMock.GetFileSize(Arg.Any<string>()).Returns(100L);
        fsMock.GetLastWriteTime(Arg.Any<string>()).Returns(DateTime.Now);

        var switcher = new ModeSwitcher.Core.CodeSwitcher("test.json", fsMock, configLoader, fileComparer, fileCopier);

        // Act
        var result = switcher.DetectCurrentMode();

        // Assert
        result.Should().NotBeNull();
        result!.ModeName.Should().Be("Prod");
    }

    [Fact]
    public void DetectCurrentMode_WithNoMatch_ReturnsNull()
    {
        // Arrange
        var fsMock = Substitute.For<IFileSystem>();
        var configLoader = new ConfigLoader(fsMock);
        var fileComparer = new FileComparer(fsMock);
        var fileCopier = new FileCopier(fsMock);

        var config = new SwitcherConfig
        {
            TargetPath = "C:\\Target",
            Modes = new List<ModeDefinition>()
        };

        var configJson = System.Text.Json.JsonSerializer.Serialize(config);
        var configStream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(configJson));
        fsMock.OpenRead(Arg.Any<string>()).Returns(configStream);

        var switcher = new ModeSwitcher.Core.CodeSwitcher("test.json", fsMock, configLoader, fileComparer, fileCopier);

        // Act
        var result = switcher.DetectCurrentMode();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ApplyModeAsync_WithValidMode_ReturnsTrue()
    {
        // Arrange
        var fsMock = Substitute.For<IFileSystem>();
        var configLoader = new ConfigLoader(fsMock);
        var fileComparer = new FileComparer(fsMock);
        var fileCopier = new FileCopier(fsMock);

        var config = new SwitcherConfig
        {
            TargetPath = "C:\\Target",
            Modes = new List<ModeDefinition>
            {
                new() { Name = "Prod", Folder = "prod" }
            }
        };

        var configJson = System.Text.Json.JsonSerializer.Serialize(config);
        var configStream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(configJson));
        fsMock.OpenRead(Arg.Any<string>()).Returns(configStream);

        var switcher = new ModeSwitcher.Core.CodeSwitcher("test.json", fsMock, configLoader, fileComparer, fileCopier);

        // Act
        var result = await switcher.ApplyModeAsync("Prod");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ApplyModeAsync_WithInvalidMode_ReturnsFalse()
    {
        // Arrange
        var fsMock = Substitute.For<IFileSystem>();
        var configLoader = new ConfigLoader(fsMock);
        var fileComparer = new FileComparer(fsMock);
        var fileCopier = new FileCopier(fsMock);

        var config = new SwitcherConfig
        {
            TargetPath = "C:\\Target",
            Modes = new List<ModeDefinition>()
        };

        var configJson = System.Text.Json.JsonSerializer.Serialize(config);
        var configStream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(configJson));
        fsMock.OpenRead(Arg.Any<string>()).Returns(configStream);

        var switcher = new ModeSwitcher.Core.CodeSwitcher("test.json", fsMock, configLoader, fileComparer, fileCopier);

        // Act
        var result = await switcher.ApplyModeAsync("NonExistent");

        // Assert
        result.Should().BeFalse();
    }

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
        var before = switcher.GetModes();
        switcher.Reload();
        var after = switcher.GetModes();

        // Assert
        before.Should().HaveCount(1);
        after.Should().HaveCount(2);
    }

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

        // Assert
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

        fsMock.OpenRead(Arg.Any<string>()).Returns(
            _ => new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonInitial)));

        var writeStream = new MemoryStream();
        fsMock.OpenWrite("test.json.tmp").Returns(writeStream);

        var switcher = new ModeSwitcher.Core.CodeSwitcher(
            "test.json", fsMock, configLoader, fileComparer, fileCopier, modeSaver, configWriter);

        _ = switcher.GetModes();
        await switcher.SaveCurrentAsModeAsync("X", "x", Array.Empty<string>(), overwrite: false);
        _ = switcher.GetModes();

        fsMock.ReceivedCalls()
            .Count(c => c.GetMethodInfo().Name == nameof(IFileSystem.OpenRead))
            .Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void ConfigPath_ReturnsConfigPath()
    {
        // Arrange
        var fsMock = Substitute.For<IFileSystem>();
        var configLoader = new ConfigLoader(fsMock);
        var fileComparer = new FileComparer(fsMock);
        var fileCopier = new FileCopier(fsMock);

        var switcher = new ModeSwitcher.Core.CodeSwitcher("test.json", fsMock, configLoader, fileComparer, fileCopier);

        // Act
        var path = switcher.ConfigPath;

        // Assert
        path.Should().Be("test.json");
    }

    [Fact]
    public async Task DeleteModeAsync_ExistingMode_RemovesFolderAndUpdatesConfig()
    {
        // Arrange
        var fsMock = Substitute.For<IFileSystem>();
        var configLoader = new ConfigLoader(fsMock);
        var fileComparer = new FileComparer(fsMock);
        var fileCopier = new FileCopier(fsMock);
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
            "test.json", fsMock, configLoader, fileComparer, fileCopier, configWriter: configWriter);

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
            "test.json", fsMock, configLoader, fileComparer, fileCopier, configWriter: configWriter);

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
            "test.json", fsMock, configLoader, fileComparer, fileCopier, configWriter: configWriter);

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
            "test.json", fsMock, configLoader, fileComparer, fileCopier, configWriter: configWriter);

        _ = switcher.GetModes(); // primes cache
        await switcher.DeleteModeAsync("A");
        _ = switcher.GetModes();

        fsMock.ReceivedCalls()
            .Count(c => c.GetMethodInfo().Name == nameof(IFileSystem.OpenRead))
            .Should().BeGreaterThanOrEqualTo(2);
    }
}
