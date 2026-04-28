using FluentAssertions;
using ModeSwitcher.Core.FileSystem;
using ModeSwitcher.Core.Services;
using NSubstitute;
using Xunit;

namespace ModeSwitcher.Core.Tests.Services;

public class FileCopierTests
{
    [Fact]
    public async Task CopyAsync_SingleFile_CopiesSuccessfully()
    {
        // Arrange
        var fsMock = Substitute.For<IFileSystem>();
        var copier = new FileCopier(fsMock);

        fsMock.DirectoryExists("source").Returns(true);
        fsMock.GetAllFiles("source", "*", System.IO.SearchOption.AllDirectories)
            .Returns(new[] { "source\\app.config" });
        fsMock.FileExists("source\\app.config").Returns(true);
        fsMock.OpenRead("source\\app.config").Returns(new MemoryStream());

        var progress = new Progress<string>();

        // Act
        await copier.CopyAsync("source", "target", progress);

        // Assert
        fsMock.Received(1).CopyFile("source\\app.config", "target\\app.config", true);
    }

    [Fact]
    public async Task CopyAsync_Recursive_CopiesSubdirectoryFiles()
    {
        // Arrange
        var fsMock = Substitute.For<IFileSystem>();
        var copier = new FileCopier(fsMock);

        fsMock.DirectoryExists("source").Returns(true);
        fsMock.GetAllFiles("source", "*", System.IO.SearchOption.AllDirectories)
            .Returns(new[] {
                "source\\app.config",
                "source\\sub\\settings.json"
            });
        fsMock.FileExists(Arg.Any<string>()).Returns(true);
        fsMock.OpenRead(Arg.Any<string>()).Returns(new MemoryStream());

        // Act
        await copier.CopyAsync("source", "target");

        // Assert
        fsMock.Received(1).CopyFile("source\\app.config", "target\\app.config", true);
        fsMock.Received(1).CopyFile("source\\sub\\settings.json", "target\\sub\\settings.json", true);
    }

    [Fact]
    public async Task CopyAsync_SourceDoesNotExist_DoesNothing()
    {
        // Arrange
        var fsMock = Substitute.For<IFileSystem>();
        var copier = new FileCopier(fsMock);

        fsMock.DirectoryExists("source").Returns(false);

        // Act
        await copier.CopyAsync("source", "target");

        // Assert
        fsMock.DidNotReceive().CopyFile(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>());
    }

    [Fact]
    public async Task CopyAsync_CreatesTargetDirectoryIfNeeded()
    {
        // Arrange
        var fsMock = Substitute.For<IFileSystem>();
        var copier = new FileCopier(fsMock);

        fsMock.DirectoryExists("source").Returns(true);
        fsMock.DirectoryExists("target").Returns(false);
        fsMock.GetAllFiles("source", "*", System.IO.SearchOption.AllDirectories)
            .Returns(Array.Empty<string>());

        // Act
        await copier.CopyAsync("source", "target");

        // Assert
        fsMock.Received(1).CreateDirectory("target");
    }

    [Fact]
    public async Task CopyAsync_ReportsProgress()
    {
        // Arrange
        var fsMock = Substitute.For<IFileSystem>();
        var copier = new FileCopier(fsMock);

        fsMock.DirectoryExists("source").Returns(true);
        fsMock.DirectoryExists("target").Returns(true);
        fsMock.GetAllFiles("source", "*", System.IO.SearchOption.AllDirectories)
            .Returns(new[] { "source\\file1.txt", "source\\file2.txt" });
        fsMock.FileExists(Arg.Any<string>()).Returns(true);
        fsMock.OpenRead(Arg.Any<string>()).Returns(new MemoryStream());

        var progressMessages = new List<string>();
        var progress = new Progress<string>(msg => progressMessages.Add(msg));

        // Act
        await copier.CopyAsync("source", "target", progress);

        // Assert
        progressMessages.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task CopyAsync_PreservesFileModificationTime()
    {
        // Arrange
        var fsMock = Substitute.For<IFileSystem>();
        var copier = new FileCopier(fsMock);

        var originalTime = new DateTime(2024, 1, 1, 12, 0, 0);

        fsMock.DirectoryExists("source").Returns(true);
        fsMock.GetAllFiles("source", "*", System.IO.SearchOption.AllDirectories)
            .Returns(new[] { "source\\file.txt" });
        fsMock.GetLastWriteTime("source\\file.txt").Returns(originalTime);
        fsMock.FileExists("source\\file.txt").Returns(true);

        // Act
        await copier.CopyAsync("source", "target");

        // Assert
        fsMock.Received(1).CopyFile("source\\file.txt", "target\\file.txt", true);
        fsMock.Received(1).SetLastWriteTime("target\\file.txt", originalTime);
    }
}
