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
}
