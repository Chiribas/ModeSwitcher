namespace ModeSwitcher.Core.FileSystem;

public interface IFileSystem
{
    bool DirectoryExists(string path);
    bool FileExists(string path);
    void CreateDirectory(string path);
    string[] GetAllFiles(string path, string searchPattern, System.IO.SearchOption option);
    System.IO.Stream OpenRead(string path);
    void CopyFile(string source, string dest, bool overwrite);
    DateTime GetLastWriteTime(string path);
    void SetLastWriteTime(string path, DateTime time);
    long GetFileSize(string path);
    void DeleteDirectory(string path, bool recursive);
}
