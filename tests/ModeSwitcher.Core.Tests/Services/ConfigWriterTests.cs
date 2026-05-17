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
        json.Should().Contain("\n");
        json.Should().Contain("  ");
    }
}
