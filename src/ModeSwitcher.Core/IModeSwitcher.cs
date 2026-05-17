using ModeSwitcher.Core.Models;

namespace ModeSwitcher.Core;

public interface ICodeSwitcher
{
    IReadOnlyList<ModeInfo> GetModes();
    CurrentModeResult? DetectCurrentMode();
    Task<bool> ApplyModeAsync(string modeName);
    Task SaveCurrentAsModeAsync(string modeName, string folderName, IEnumerable<string> relativePaths, bool overwrite);
    Task DeleteModeAsync(string modeName);
    void Reload();
    string ConfigPath { get; }
}
