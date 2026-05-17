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
                "C:\\Target\\settings.json",
                "C:\\Target\\CLAUDE.md",
                "C:\\Target\\keybindings.json"
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
        var fsMock = Substitute.For<IFileSystem>();
        var saver = new ModeSaver(fsMock);

        fsMock.DirectoryExists("C:\\Target").Returns(true);
        fsMock.GetAllFiles("C:\\Target", "*", SearchOption.TopDirectoryOnly)
            .Returns(new[] {
                "C:\\Target\\settings.json",
                "C:\\Target\\CLAUDE.md"
            });

        var result = saver.GetCandidates("C:\\Target", currentModePath: null);

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
