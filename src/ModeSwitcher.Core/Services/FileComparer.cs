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

        var targetHash = ComputeDirectoryHash(targetPath);

        foreach (var mode in modes)
        {
            var modePath = Path.Combine(modesBasePath, mode.Folder);
            if (!_fileSystem.DirectoryExists(modePath))
            {
                continue;
            }

            var modeHash = ComputeDirectoryHash(modePath);
            if (targetHash == modeHash)
            {
                return new CurrentModeResult { ModeName = mode.Name };
            }
        }

        return null;
    }

    private string ComputeDirectoryHash(string path)
    {
        var files = _fileSystem.GetAllFiles(path, "*", System.IO.SearchOption.AllDirectories);
        Array.Sort(files);

        using var md5 = MD5.Create();
        foreach (var file in files)
        {
            var relativePath = file.Substring(path.Length).TrimStart(Path.DirectorySeparatorChar);
            var size = _fileSystem.GetFileSize(file);
            var modified = _fileSystem.GetLastWriteTime(file).Ticks;

            var hashInput = $"{relativePath}|{size}|{modified}";
            var inputBytes = Encoding.UTF8.GetBytes(hashInput);
            md5.TransformBlock(inputBytes, 0, inputBytes.Length, null, 0);
        }

        md5.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return Convert.ToHexString(md5.Hash!);
    }
}
