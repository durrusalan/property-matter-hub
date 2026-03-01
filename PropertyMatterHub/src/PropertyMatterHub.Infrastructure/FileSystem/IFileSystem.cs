namespace PropertyMatterHub.Infrastructure.FileSystem;

/// <summary>
/// Abstraction over System.IO so ZDriveScanner can be tested without a real disk.
/// </summary>
public interface IFileSystem
{
    IEnumerable<string> GetDirectories(string path, int depth);
    bool DirectoryExists(string path);
    IEnumerable<string> GetFiles(string path, string searchPattern = "*", bool recurse = false);
    DateTime GetLastWriteTimeUtc(string path);
    long GetFileSize(string path);
}
