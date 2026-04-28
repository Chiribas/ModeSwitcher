namespace ModeSwitcher.Core.FileSystem;

public class RealFileSystem : IFileSystem
{
    public bool DirectoryExists(string path) => Directory.Exists(path);

    public bool FileExists(string path) => File.Exists(path);

    public void CreateDirectory(string path) => Directory.CreateDirectory(path);

    public string[] GetAllFiles(string path, string searchPattern, System.IO.SearchOption option)
        => Directory.GetFiles(path, searchPattern, option);

    public System.IO.Stream OpenRead(string path) => File.OpenRead(path);

    public void CopyFile(string source, string dest, bool overwrite)
        => File.Copy(source, dest, overwrite);

    public DateTime GetLastWriteTime(string path) => File.GetLastWriteTime(path);

    public void SetLastWriteTime(string path, DateTime time)
        => File.SetLastWriteTime(path, time);

    public long GetFileSize(string path) => new FileInfo(path).Length;

    public void DeleteDirectory(string path, bool recursive)
        => Directory.Delete(path, recursive);
}
