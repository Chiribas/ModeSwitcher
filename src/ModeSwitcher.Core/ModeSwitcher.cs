using ModeSwitcher.Core.FileSystem;
using ModeSwitcher.Core.Models;
using ModeSwitcher.Core.Services;

namespace ModeSwitcher.Core;

public class CodeSwitcher : ICodeSwitcher
{
    private readonly string _configPath;
    private readonly string _modesBasePath;
    private readonly IFileSystem _fileSystem;
    private readonly ConfigLoader _configLoader;
    private readonly FileComparer _fileComparer;
    private readonly FileCopier _fileCopier;
    private readonly ModeSaver _modeSaver;
    private readonly ConfigWriter _configWriter;

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
        FileCopier? fileCopier = null,
        ModeSaver? modeSaver = null,
        ConfigWriter? configWriter = null)
    {
        _configPath = configPath;
        _modesBasePath = Path.Combine(Path.GetDirectoryName(configPath)!, "modes");
        _fileSystem = fileSystem;
        _configLoader = configLoader ?? new ConfigLoader(fileSystem);
        _fileComparer = fileComparer ?? new FileComparer(fileSystem);
        _fileCopier = fileCopier ?? new FileCopier(fileSystem);
        _modeSaver = modeSaver ?? new ModeSaver(fileSystem);
        _configWriter = configWriter ?? new ConfigWriter(fileSystem);
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

    public async Task SaveCurrentAsModeAsync(
        string modeName,
        string folderName,
        IEnumerable<string> relativePaths,
        bool overwrite)
    {
        var config = LoadConfig();
        if (config is null)
        {
            throw new InvalidOperationException("Config could not be loaded.");
        }

        var newModePath = Path.Combine(_modesBasePath, folderName);

        if (overwrite && _fileSystem.DirectoryExists(newModePath))
        {
            _fileSystem.DeleteDirectory(newModePath, recursive: true);
        }

        await _modeSaver.SaveAsync(config.TargetPath, newModePath, relativePaths);

        var existing = config.Modes.FirstOrDefault(m => m.Name == modeName);
        if (existing is not null)
        {
            existing.Folder = folderName;
        }
        else
        {
            config.Modes.Add(new ModeDefinition { Name = modeName, Folder = folderName });
        }

        _configWriter.Save(_configPath, config);
        Reload();
    }

    public Task DeleteModeAsync(string modeName)
    {
        var config = LoadConfig();
        if (config is null)
        {
            throw new InvalidOperationException("Config could not be loaded.");
        }

        var def = config.Modes.FirstOrDefault(m => m.Name == modeName);
        if (def is null)
        {
            return Task.CompletedTask;
        }

        var modePath = Path.Combine(_modesBasePath, def.Folder);
        if (_fileSystem.DirectoryExists(modePath))
        {
            _fileSystem.DeleteDirectory(modePath, recursive: true);
        }

        config.Modes.Remove(def);
        _configWriter.Save(_configPath, config);
        Reload();

        return Task.CompletedTask;
    }

    public void Reload()
    {
        _cachedConfig = null;
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
