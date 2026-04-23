using FluentAssertions;
using ModeSwitcher.Core.FileSystem;
using ModeSwitcher.Core.Models;
using ModeSwitcher.Core.Services;
using NSubstitute;
using System.Text;
using Xunit;

namespace ModeSwitcher.Core.Tests.Services;

public class ConfigLoaderTests
{
    [Fact]
    public void Load_ValidJson_ReturnsConfig()
    {
        // Arrange
        var fsMock = Substitute.For<IFileSystem>();
        var jsonContent = """
            {
              "TargetPath": "C:\\MyApp\\Config",
              "Modes": [
                { "Name": "Production", "Folder": "prod" },
                { "Name": "Development", "Folder": "dev" }
              ]
            }
            """;
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonContent));
        fsMock.OpenRead(Arg.Any<string>()).Returns(stream);

        var loader = new ConfigLoader(fsMock);

        // Act
        var config = loader.Load("test.json");

        // Assert
        config.Should().NotBeNull();
        config.TargetPath.Should().Be("C:\\MyApp\\Config");
        config.Modes.Should().HaveCount(2);
        config.Modes[0].Name.Should().Be("Production");
        config.Modes[0].Folder.Should().Be("prod");
        config.Modes[1].Name.Should().Be("Development");
        config.Modes[1].Folder.Should().Be("dev");
    }

    [Fact]
    public void Load_InvalidJson_ReturnsNull()
    {
        // Arrange
        var fsMock = Substitute.For<IFileSystem>();
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("{ invalid json"));
        fsMock.OpenRead(Arg.Any<string>()).Returns(stream);

        var loader = new ConfigLoader(fsMock);

        // Act
        var config = loader.Load("test.json");

        // Assert
        config.Should().BeNull();
    }

    [Fact]
    public void Load_FileDoesNotExist_ReturnsNull()
    {
        // Arrange
        var fsMock = Substitute.For<IFileSystem>();
        fsMock.OpenRead(Arg.Any<string>()).Returns(x => throw new FileNotFoundException());

        var loader = new ConfigLoader(fsMock);

        // Act
        var config = loader.Load("nonexistent.json");

        // Assert
        config.Should().BeNull();
    }

    [Fact]
    public void Load_EmptyModes_ReturnsConfigWithEmptyModes()
    {
        // Arrange
        var fsMock = Substitute.For<IFileSystem>();
        var jsonContent = """
            {
              "TargetPath": "C:\\MyApp\\Config",
              "Modes": []
            }
            """;
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonContent));
        fsMock.OpenRead(Arg.Any<string>()).Returns(stream);

        var loader = new ConfigLoader(fsMock);

        // Act
        var config = loader.Load("test.json");

        // Assert
        config.Should().NotBeNull();
        config.Modes.Should().BeEmpty();
    }
}
