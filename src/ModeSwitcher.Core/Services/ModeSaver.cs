using ModeSwitcher.Core.FileSystem;

namespace ModeSwitcher.Core.Services;

public record FileCandidate(string RelativePath, bool InCurrentMode);
public record SaveCandidates(List<FileCandidate> Files);

public class ModeSaver
{
    private readonly IFileSystem _fileSystem;

    public ModeSaver(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public SaveCandidates GetCandidates(string targetPath, string? currentModePath)
    {
        var currentModeRels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (currentModePath is not null && _fileSystem.DirectoryExists(currentModePath))
        {
            foreach (var file in _fileSystem.GetAllFiles(currentModePath, "*", SearchOption.AllDirectories))
            {
                var rel = file.Substring(currentModePath.Length).TrimStart(Path.DirectorySeparatorChar);
                currentModeRels.Add(rel);
            }
        }

        var newFiles = new List<string>();
        if (_fileSystem.DirectoryExists(targetPath))
        {
            foreach (var file in _fileSystem.GetAllFiles(targetPath, "*", SearchOption.TopDirectoryOnly))
            {
                var rel = file.Substring(targetPath.Length).TrimStart(Path.DirectorySeparatorChar);
                if (!currentModeRels.Contains(rel))
                {
                    newFiles.Add(rel);
                }
            }
        }

        var combined = currentModeRels
            .OrderBy(r => r, StringComparer.OrdinalIgnoreCase)
            .Select(r => new FileCandidate(r, true))
            .Concat(newFiles
                .OrderBy(r => r, StringComparer.OrdinalIgnoreCase)
                .Select(r => new FileCandidate(r, false)))
            .ToList();

        return new SaveCandidates(combined);
    }
}
