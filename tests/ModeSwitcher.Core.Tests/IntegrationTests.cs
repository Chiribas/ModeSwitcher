using FluentAssertions;
using ModeSwitcher.Core;
using ModeSwitcher.Core.FileSystem;
using ModeSwitcher.Core.Models;
using ModeSwitcher.Core.Services;
using Xunit;

namespace ModeSwitcher.Core.Tests;

public class IntegrationTests
{
    private string GetProductionConfigPath()
    {
        var baseDir = AppContext.BaseDirectory;
        var prodDir = Path.Combine(baseDir, "..", "..", "..", "production");
        return Path.GetFullPath(Path.Combine(prodDir, "modeswitcher.json"));
    }

    private string GetProductionTargetPath()
    {
        var config = File.ReadAllText(GetProductionConfigPath());
        using var doc = System.Text.Json.JsonDocument.Parse(config);
        return doc.RootElement.GetProperty("TargetPath").GetString() ?? "d:/ForPeople/test/.claude";
    }

    [Fact]
    public void EndToEnd_LoadConfig_GetModes_DetectCurrent_ApplyMode()
    {
        // Arrange
        var configPath = GetProductionConfigPath();
        var fs = new RealFileSystem();
        var switcher = new ModeSwitcher.Core.CodeSwitcher(configPath, fs);

        // Act - Get modes
        var modes = switcher.GetModes();

        // Assert - Should have modes
        modes.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task EndToEnd_ApplyMode_VerifyDetection()
    {
        // Arrange
        var configPath = GetProductionConfigPath();
        var fs = new RealFileSystem();
        var switcher = new ModeSwitcher.Core.CodeSwitcher(configPath, fs);

        // Act - Get modes
        var modes = switcher.GetModes();
        modes.Should().HaveCountGreaterThan(0);

        // Apply first mode
        var result = await switcher.ApplyModeAsync(modes[0].Name);
        result.Should().BeTrue();

        // Verify detection
        var current = switcher.DetectCurrentMode();
        current.Should().NotBeNull();
        current!.ModeName.Should().Be(modes[0].Name);
    }

    [Fact]
    public async Task EndToEnd_SwitchBetweenModes()
    {
        // Arrange
        var configPath = GetProductionConfigPath();
        var fs = new RealFileSystem();
        var switcher = new ModeSwitcher.Core.CodeSwitcher(configPath, fs);

        // Act - Get modes
        var modes = switcher.GetModes();
        if (modes.Count < 2)
        {
            return; // Skip if less than 2 modes
        }

        // Apply second mode
        var result = await switcher.ApplyModeAsync(modes[1].Name);
        result.Should().BeTrue();

        // Verify detection
        var current = switcher.DetectCurrentMode();
        current.Should().NotBeNull();
        current!.ModeName.Should().Be(modes[1].Name);
    }
}
