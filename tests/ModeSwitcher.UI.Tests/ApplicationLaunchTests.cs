using Xunit;
using Xunit.Abstractions;
using System.Diagnostics;

namespace ModeSwitcher.UI.Tests;

public class ApplicationLaunchTests
{
    private readonly ITestOutputHelper _output;

    public ApplicationLaunchTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Application_Starts_InTestMode_ExitsSuccessfully()
    {
        // Arrange
        var solutionRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var exePath = Path.Combine(solutionRoot, "src", "ModeSwitcher.UI", "bin", "Debug", "net10.0-windows", "ModeSwitcher.UI.exe");

        if (!File.Exists(exePath))
        {
            _output.WriteLine($"Debug exe not found at {exePath}");
            Assert.True(false, "Debug build not found. Run 'dotnet build' first.");
        }

        // Act
        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = "--test",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(exePath)
        };

        using var process = Process.Start(startInfo);
        Assert.NotNull(process);

        var exited = process.WaitForExit(10000);
        Assert.True(exited, "Application did not exit within timeout");

        _output.WriteLine($"Exit code: {process.ExitCode}");
        _output.WriteLine($"Stdout: {process.StandardOutput.ReadToEnd()}");
        _output.WriteLine($"Stderr: {process.StandardError.ReadToEnd()}");

        // Assert
        Assert.Equal(0, process.ExitCode);
    }

    [Fact]
    public void Application_Starts_InTestMode_LogsSuccess()
    {
        // Arrange
        var solutionRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var exePath = Path.Combine(solutionRoot, "src", "ModeSwitcher.UI", "bin", "Debug", "net10.0-windows", "ModeSwitcher.UI.exe");

        if (!File.Exists(exePath))
        {
            _output.WriteLine($"Debug exe not found at {exePath}");
            Assert.True(false, "Debug build not found. Run 'dotnet build' first.");
            return;
        }

        // Create temp config for isolated test
        var tempDir = Path.Combine(Path.GetTempPath(), $"mode_switcher_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Copy exe and all dependencies to temp dir
            var exeDir = Path.GetDirectoryName(exePath)!;
            foreach (var file in Directory.GetFiles(exeDir))
            {
                var fileName = Path.GetFileName(file);
                if (fileName.StartsWith("ModeSwitcher.UI") || fileName.EndsWith(".dll"))
                {
                    File.Copy(file, Path.Combine(tempDir, fileName), true);
                }
            }

            var tempExePath = Path.Combine(tempDir, "ModeSwitcher.UI.exe");

            // Act
            var startInfo = new ProcessStartInfo
            {
                FileName = tempExePath,
                Arguments = "--test",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = tempDir
            };
            startInfo.Environment["MODE_SWITCHER_LOG_DIR"] = tempDir;

            using var process = Process.Start(startInfo);
            Assert.NotNull(process);

            var exited = process.WaitForExit(10000);
            Assert.True(exited, "Application did not exit within timeout");

            _output.WriteLine($"Exit code: {process.ExitCode}");
            _output.WriteLine($"Stdout: {process.StandardOutput.ReadToEnd()}");
            _output.WriteLine($"Stderr: {process.StandardError.ReadToEnd()}");

            var logPath = Path.Combine(tempDir, "log.txt");
            _output.WriteLine($"Log path: {logPath}");

            // Assert
            Assert.Equal(0, process.ExitCode);
            Assert.True(File.Exists(logPath), "Log file was not created");

            var logContent = File.ReadAllText(logPath);
            _output.WriteLine($"Log content:\n{logContent}");

            Assert.Contains("Test mode: MainForm created successfully", logContent);
            Assert.Contains("Application exiting normally (test mode)", logContent);
            Assert.DoesNotContain("FATAL ERROR", logContent);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }
}
