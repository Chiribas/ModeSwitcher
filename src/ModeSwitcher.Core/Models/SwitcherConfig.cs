namespace ModeSwitcher.Core.Models;

public class SwitcherConfig
{
    public string TargetPath { get; set; } = null!;
    public List<ModeDefinition> Modes { get; set; } = null!;
}
