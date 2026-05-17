using System.Text.Json;
using ModeSwitcher.Core.FileSystem;
using ModeSwitcher.Core.Models;

namespace ModeSwitcher.Core.Services;

public class ConfigWriter
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    private readonly IFileSystem _fileSystem;

    public ConfigWriter(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public void Save(string configPath, SwitcherConfig config)
    {
        var tempPath = configPath + ".tmp";
        using (var stream = _fileSystem.OpenWrite(tempPath))
        {
            JsonSerializer.Serialize(stream, config, Options);
        }
        _fileSystem.MoveFile(tempPath, configPath, overwrite: true);
    }
}
