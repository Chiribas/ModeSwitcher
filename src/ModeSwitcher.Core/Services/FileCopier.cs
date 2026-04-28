using ModeSwitcher.Core.FileSystem;

namespace ModeSwitcher.Core.Services;

public class FileCopier
{
    private readonly IFileSystem _fileSystem;

    public FileCopier(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public async Task CopyAsync(
        string sourcePath,
        string targetPath,
        IProgress<string>? progress = null)
    {
        await Task.Run(() =>
        {
            if (!_fileSystem.DirectoryExists(sourcePath))
            {
                return;
            }

            if (!_fileSystem.DirectoryExists(targetPath))
            {
                _fileSystem.CreateDirectory(targetPath);
            }

            var files = _fileSystem.GetAllFiles(sourcePath, "*", System.IO.SearchOption.AllDirectories);

            foreach (var file in files)
            {
                var relativePath = file.Substring(sourcePath.Length).TrimStart(Path.DirectorySeparatorChar);
                var destFile = Path.Combine(targetPath, relativePath);

                var destDir = Path.GetDirectoryName(destFile);
                if (!string.IsNullOrEmpty(destDir) && !_fileSystem.DirectoryExists(destDir))
                {
                    _fileSystem.CreateDirectory(destDir);
                }

                progress?.Report($"Copying {relativePath}...");
                _fileSystem.CopyFile(file, destFile, overwrite: true);
                // Preserve original file modification time for hash comparison
                var originalTime = _fileSystem.GetLastWriteTime(file);
                _fileSystem.SetLastWriteTime(destFile, originalTime);
            }

            progress?.Report("Copy completed.");
        });
    }
}
