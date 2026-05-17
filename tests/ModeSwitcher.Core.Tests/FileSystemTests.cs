using FluentAssertions;
using ModeSwitcher.Core.FileSystem;
using Xunit;

namespace ModeSwitcher.Core.Tests;

public class FileSystemTests
{
    [Fact]
    public void RealFileSystem_DirectoryExists_ReturnsCorrectResult()
    {
        // Arrange
        var fs = new RealFileSystem();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        // Act
        var exists = fs.DirectoryExists(tempDir);
        var notExists = fs.DirectoryExists(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));

        // Assert
        exists.Should().BeTrue();
        notExists.Should().BeFalse();

        // Cleanup
        Directory.Delete(tempDir);
    }

    [Fact]
    public void RealFileSystem_FileExists_ReturnsCorrectResult()
    {
        // Arrange
        var fs = new RealFileSystem();
        var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".txt");
        File.WriteAllText(tempFile, "test");

        // Act
        var exists = fs.FileExists(tempFile);
        var notExists = fs.FileExists(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".txt"));

        // Assert
        exists.Should().BeTrue();
        notExists.Should().BeFalse();

        // Cleanup
        File.Delete(tempFile);
    }

    [Fact]
    public void RealFileSystem_CreateDirectory_CreatesDirectory()
    {
        // Arrange
        var fs = new RealFileSystem();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act
        fs.CreateDirectory(tempDir);

        // Assert
        Directory.Exists(tempDir).Should().BeTrue();

        // Cleanup
        Directory.Delete(tempDir);
    }

    [Fact]
    public void RealFileSystem_GetAllFiles_ReturnsFiles()
    {
        // Arrange
        var fs = new RealFileSystem();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        File.WriteAllText(Path.Combine(tempDir, "test1.txt"), "content1");
        File.WriteAllText(Path.Combine(tempDir, "test2.txt"), "content2");

        // Act
        var files = fs.GetAllFiles(tempDir, "*", System.IO.SearchOption.TopDirectoryOnly);

        // Assert
        files.Should().HaveCount(2);

        // Cleanup
        Directory.Delete(tempDir, true);
    }

    [Fact]
    public void RealFileSystem_CopyFile_CopiesFile()
    {
        // Arrange
        var fs = new RealFileSystem();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var sourceFile = Path.Combine(tempDir, "source.txt");
        var destFile = Path.Combine(tempDir, "dest.txt");
        File.WriteAllText(sourceFile, "test content");

        // Act
        fs.CopyFile(sourceFile, destFile, overwrite: true);

        // Assert
        File.Exists(destFile).Should().BeTrue();
        File.ReadAllText(destFile).Should().Be("test content");

        // Cleanup
        Directory.Delete(tempDir, true);
    }

    [Fact]
    public void RealFileSystem_GetLastWriteTime_ReturnsCorrectTime()
    {
        // Arrange
        var fs = new RealFileSystem();
        var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".txt");
        File.WriteAllText(tempFile, "test");
        var expectedTime = File.GetLastWriteTime(tempFile);

        // Act
        var result = fs.GetLastWriteTime(tempFile);

        // Assert
        result.Should().BeCloseTo(expectedTime, TimeSpan.FromMilliseconds(100));

        // Cleanup
        File.Delete(tempFile);
    }

    [Fact]
    public void RealFileSystem_GetFileSize_ReturnsCorrectSize()
    {
        // Arrange
        var fs = new RealFileSystem();
        var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".txt");
        var content = "test content";
        File.WriteAllText(tempFile, content);

        // Act
        var size = fs.GetFileSize(tempFile);

        // Assert
        size.Should().Be(content.Length);

        // Cleanup
        File.Delete(tempFile);
    }

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
}
