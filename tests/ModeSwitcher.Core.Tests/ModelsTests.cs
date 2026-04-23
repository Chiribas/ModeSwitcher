using FluentAssertions;
using ModeSwitcher.Core.Models;
using Xunit;

namespace ModeSwitcher.Core.Tests;

public class ModelsTests
{
    [Fact]
    public void SwitcherConfig_CanBeInstantiated()
    {
        // Arrange & Act
        var config = new SwitcherConfig
        {
            TargetPath = "C:\\Test",
            Modes = new List<ModeDefinition>
            {
                new() { Name = "Prod", Folder = "prod" }
            }
        };

        // Assert
        config.TargetPath.Should().Be("C:\\Test");
        config.Modes.Should().HaveCount(1);
        config.Modes[0].Name.Should().Be("Prod");
        config.Modes[0].Folder.Should().Be("prod");
    }

    [Fact]
    public void ModeInfo_CanBeInstantiated()
    {
        // Arrange & Act
        var mode = new ModeInfo
        {
            Name = "Prod",
            IsActive = true
        };

        // Assert
        mode.Name.Should().Be("Prod");
        mode.IsActive.Should().BeTrue();
    }

    [Fact]
    public void CurrentModeResult_CanBeNullModeName()
    {
        // Arrange & Act
        var result = new CurrentModeResult
        {
            ModeName = null
        };

        // Assert
        result.ModeName.Should().BeNull();
    }

    [Fact]
    public void CurrentModeResult_CanHaveModeName()
    {
        // Arrange & Act
        var result = new CurrentModeResult
        {
            ModeName = "Prod"
        };

        // Assert
        result.ModeName.Should().Be("Prod");
    }
}
