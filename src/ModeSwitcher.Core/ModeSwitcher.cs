using ModeSwitcher.Core.FileSystem;
using ModeSwitcher.Core.Models;
using ModeSwitcher.Core.Services;

namespace ModeSwitcher.Core;

public class CodeSwitcher : ICodeSwitcher
{
    private readonly string _configPath;
    private readonly string _modesBasePath;
    private readonly ConfigLoader _configLoader;
    private readonly FileComparer _fileComparer;
    private readonly FileCopier _fileCopier;

    private SwitcherConfig? _cachedConfig;

    public string ConfigPath => _configPath;

    public CodeSwitcher() : this(GetDefaultConfigPath())
    {
    }

    public CodeSwitcher(string configPath)
        : this(configPath, new RealFileSystem())
    {
    }

    internal CodeSwitcher(
        string configPath,
        IFileSystem fileSystem,
        ConfigLoader? configLoader = null,
        FileComparer? fileComparer = null,
        FileCopier? fileCopier = null)
    {
        _configPath = configPath;
        _modesBasePath = Path.Combine(Path.GetDirectoryName(configPath)!, "modes");
        _configLoader = configLoader ?? new ConfigLoader(fileSystem);
        _fileComparer = fileComparer ?? new FileComparer(fileSystem);
        _fileCopier = fileCopier ?? new FileCopier(fileSystem);
    }

    public IReadOnlyList<ModeInfo> GetModes()
    {
        var config = LoadConfig();
        if (config is null)
        {
            return Array.Empty<ModeInfo>();
        }

        var currentMode = DetectCurrentMode();
        return config.Modes.Select(m => new ModeInfo
        {
            Name = m.Name,
            IsActive = currentMode?.ModeName == m.Name
        }).ToList();
    }

    public CurrentModeResult? DetectCurrentMode()
    {
        var config = LoadConfig();
        if (config is null)
        {
            return null;
        }

        return _fileComparer.DetectCurrentMode(config.TargetPath, config.Modes, _modesBasePath);
    }

    public async Task<bool> ApplyModeAsync(string modeName)
    {
        var config = LoadConfig();
        if (config is null)
        {
            return false;
        }

        var mode = config.Modes.FirstOrDefault(m => m.Name == modeName);
        if (mode is null)
        {
            return false;
        }

        var modePath = Path.Combine(_modesBasePath, mode.Folder);
        await _fileCopier.CopyAsync(modePath, config.TargetPath);

        return true;
    }

    private SwitcherConfig? LoadConfig()
    {
        if (_cachedConfig is not null)
        {
            return _cachedConfig;
        }

        _cachedConfig = _configLoader.Load(_configPath);
        return _cachedConfig;
    }

    private static string GetDefaultConfigPath()
    {
        var baseDirectory = AppContext.BaseDirectory;
        return Path.Combine(baseDirectory, "modeswitcher.json");
    }
}
