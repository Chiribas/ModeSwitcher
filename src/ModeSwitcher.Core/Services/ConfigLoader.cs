using System.Text.Json;
using ModeSwitcher.Core.FileSystem;
using ModeSwitcher.Core.Models;

namespace ModeSwitcher.Core.Services;

public class ConfigLoader
{
    private readonly IFileSystem _fileSystem;

    public ConfigLoader(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public SwitcherConfig? Load(string configPath)
    {
        try
        {
            using var stream = _fileSystem.OpenRead(configPath);
            var config = JsonSerializer.Deserialize<SwitcherConfig>(stream);
            return config;
        }
        catch
        {
            return null;
        }
    }
}
