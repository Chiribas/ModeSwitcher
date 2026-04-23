using FluentAssertions;
using ModeSwitcher.Core.FileSystem;
using ModeSwitcher.Core.Models;
using ModeSwitcher.Core.Services;
using NSubstitute;
using Xunit;

namespace ModeSwitcher.Core.Tests.Services;

public class FileComparerTests
{
    [Fact]
    public void DetectCurrentMode_MatchingFiles_ReturnsModeName()
    {
        // Arrange
        var fsMock = Substitute.For<IFileSystem>();
        var comparer = new FileComparer(fsMock);

        fsMock.DirectoryExists("target").Returns(true);
        fsMock.DirectoryExists("modes\\prod").Returns(true);
        fsMock.DirectoryExists("modes\\dev").Returns(true);

        // Target has 2 files
        fsMock.GetAllFiles("target", "*", System.IO.SearchOption.AllDirectories)
            .Returns(new[] { "target\\app.config", "target\\settings.json" });
        fsMock.GetFileSize("target\\app.config").Returns(100);
        fsMock.GetLastWriteTime("target\\app.config").Returns(new DateTime(2024, 1, 1, 12, 0, 0));
        fsMock.GetFileSize("target\\settings.json").Returns(200);
        fsMock.GetLastWriteTime("target\\settings.json").Returns(new DateTime(2024, 1, 1, 12, 0, 0));

        // Prod mode has same files
        fsMock.GetAllFiles("modes\\prod", "*", System.IO.SearchOption.AllDirectories)
            .Returns(new[] { "modes\\prod\\app.config", "modes\\prod\\settings.json" });
        fsMock.GetFileSize("modes\\prod\\app.config").Returns(100);
        fsMock.GetLastWriteTime("modes\\prod\\app.config").Returns(new DateTime(2024, 1, 1, 12, 0, 0));
        fsMock.GetFileSize("modes\\prod\\settings.json").Returns(200);
        fsMock.GetLastWriteTime("modes\\prod\\settings.json").Returns(new DateTime(2024, 1, 1, 12, 0, 0));

        // Dev mode has different file
        fsMock.GetAllFiles("modes\\dev", "*", System.IO.SearchOption.AllDirectories)
            .Returns(new[] { "modes\\dev\\app.config" });

        var modes = new List<ModeDefinition>
        {
            new() { Name = "Production", Folder = "prod" },
            new() { Name = "Development", Folder = "dev" }
        };

        // Act
        var result = comparer.DetectCurrentMode("target", modes, "modes");

        // Assert
        result.Should().NotBeNull();
        result!.ModeName.Should().Be("Production");
    }

    [Fact]
    public void DetectCurrentMode_NoMatch_ReturnsNull()
    {
        // Arrange
        var fsMock = Substitute.For<IFileSystem>();
        var comparer = new FileComparer(fsMock);

        fsMock.GetAllFiles(Arg.Any<string>(), "*", System.IO.SearchOption.AllDirectories)
            .Returns(Array.Empty<string>());

        var modes = new List<ModeDefinition>
        {
            new() { Name = "Production", Folder = "prod" }
        };

        // Act
        var result = comparer.DetectCurrentMode("target", modes, "modes");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void DetectCurrentMode_TargetDoesNotExist_ReturnsNull()
    {
        // Arrange
        var fsMock = Substitute.For<IFileSystem>();
        var comparer = new FileComparer(fsMock);

        fsMock.DirectoryExists("target").Returns(false);

        var modes = new List<ModeDefinition>
        {
            new() { Name = "Production", Folder = "prod" }
        };

        // Act
        var result = comparer.DetectCurrentMode("target", modes, "modes");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void DetectCurrentMode_ModeFolderDoesNotExist_SkipsMode()
    {
        // Arrange
        var fsMock = Substitute.For<IFileSystem>();
        var comparer = new FileComparer(fsMock);

        fsMock.DirectoryExists("target").Returns(true);
        fsMock.GetAllFiles("target", "*", System.IO.SearchOption.AllDirectories)
            .Returns(new[] { "target\\app.config" });
        fsMock.GetFileSize("target\\app.config").Returns(100);
        fsMock.GetLastWriteTime("target\\app.config").Returns(new DateTime(2024, 1, 1));

        fsMock.DirectoryExists("modes\\prod").Returns(false);
        fsMock.DirectoryExists("modes\\dev").Returns(true);
        fsMock.GetAllFiles("modes\\dev", "*", System.IO.SearchOption.AllDirectories)
            .Returns(new[] { "modes\\dev\\app.config" });
        fsMock.GetFileSize("modes\\dev\\app.config").Returns(100);
        fsMock.GetLastWriteTime("modes\\dev\\app.config").Returns(new DateTime(2024, 1, 1));

        var modes = new List<ModeDefinition>
        {
            new() { Name = "Production", Folder = "prod" },
            new() { Name = "Development", Folder = "dev" }
        };

        // Act
        var result = comparer.DetectCurrentMode("target", modes, "modes");

        // Assert
        result.Should().NotBeNull();
        result!.ModeName.Should().Be("Development");
    }

    [Fact]
    public void DetectCurrentMode_EmptyTargetDirectory_CanMatchEmptyMode()
    {
        // Arrange
        var fsMock = Substitute.For<IFileSystem>();
        var comparer = new FileComparer(fsMock);

        fsMock.DirectoryExists("target").Returns(true);
        fsMock.GetAllFiles("target", "*", System.IO.SearchOption.AllDirectories)
            .Returns(Array.Empty<string>());

        fsMock.DirectoryExists("modes\\empty").Returns(true);
        fsMock.GetAllFiles("modes\\empty", "*", System.IO.SearchOption.AllDirectories)
            .Returns(Array.Empty<string>());

        var modes = new List<ModeDefinition>
        {
            new() { Name = "Empty", Folder = "empty" }
        };

        // Act
        var result = comparer.DetectCurrentMode("target", modes, "modes");

        // Assert
        result.Should().NotBeNull();
        result!.ModeName.Should().Be("Empty");
    }
}
