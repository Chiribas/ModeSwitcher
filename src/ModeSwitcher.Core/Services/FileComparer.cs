using System.Security.Cryptography;
using System.Text;
using ModeSwitcher.Core.FileSystem;
using ModeSwitcher.Core.Models;

namespace ModeSwitcher.Core.Services;

public class FileComparer
{
    private readonly IFileSystem _fileSystem;

    public FileComparer(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public CurrentModeResult? DetectCurrentMode(
        string targetPath,
        List<ModeDefinition> modes,
        string modesBasePath)
    {
        if (!_fileSystem.DirectoryExists(targetPath))
        {
            return null;
        }

        foreach (var mode in modes)
        {
            var modePath = Path.Combine(modesBasePath, mode.Folder);
            if (!_fileSystem.DirectoryExists(modePath))
            {
                continue;
            }

            // Get files from mode to know which ones to compare in target
            var modeFiles = _fileSystem.GetAllFiles(modePath, "*", System.IO.SearchOption.AllDirectories);
            Array.Sort(modeFiles);

            // Compute hash based only on files that exist in mode
            var targetHash = ComputeDirectoryHash(targetPath, modeFiles, modePath);
            var modeHash = ComputeDirectoryHash(modePath, modeFiles, modePath);

            if (targetHash == modeHash)
            {
                return new CurrentModeResult { ModeName = mode.Name };
            }
        }

        return null;
    }

    private string ComputeDirectoryHash(string basePath, string[] filesToCompare, string modePath)
    {
        using var md5 = MD5.Create();

        foreach (var file in filesToCompare)
        {
            // Get relative path from mode path
            var relativePath = file.Substring(modePath.Length).TrimStart(Path.DirectorySeparatorChar);
            // Build full path in the directory we're checking
            var fullPath = Path.Combine(basePath, relativePath);

            if (!_fileSystem.FileExists(fullPath))
            {
                return "MISSING_FILE"; // File missing - hashes won't match
            }

            var size = _fileSystem.GetFileSize(fullPath);
            var modified = _fileSystem.GetLastWriteTime(fullPath).Ticks;

            var hashInput = $"{relativePath}|{size}|{modified}";
            var inputBytes = Encoding.UTF8.GetBytes(hashInput);
            md5.TransformBlock(inputBytes, 0, inputBytes.Length, null, 0);
        }

        md5.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return Convert.ToHexString(md5.Hash!);
    }
}
