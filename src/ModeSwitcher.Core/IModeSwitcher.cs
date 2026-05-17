using ModeSwitcher.Core.Models;

namespace ModeSwitcher.Core;

public interface ICodeSwitcher
{
    IReadOnlyList<ModeInfo> GetModes();
    CurrentModeResult? DetectCurrentMode();
    Task<bool> ApplyModeAsync(string modeName);
    void Reload();
    string ConfigPath { get; }
}
