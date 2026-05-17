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

    public Task SaveAsync(string targetPath, string newModePath, IEnumerable<string> relativePaths)
    {
        return Task.Run(() =>
        {
            if (!_fileSystem.DirectoryExists(newModePath))
            {
                _fileSystem.CreateDirectory(newModePath);
            }

            foreach (var rel in relativePaths)
            {
                var source = Path.Combine(targetPath, rel);
                var dest = Path.Combine(newModePath, rel);

                var destDir = Path.GetDirectoryName(dest);
                if (!string.IsNullOrEmpty(destDir)
                    && !string.Equals(destDir, newModePath, StringComparison.OrdinalIgnoreCase)
                    && !_fileSystem.DirectoryExists(destDir))
                {
                    _fileSystem.CreateDirectory(destDir);
                }

                _fileSystem.CopyFile(source, dest, overwrite: true);
                var ts = _fileSystem.GetLastWriteTime(source);
                _fileSystem.SetLastWriteTime(dest, ts);
            }
        });
    }
}
