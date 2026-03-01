namespace PropertyMatterHub.Infrastructure.FileSystem;

public class RealFileSystem : IFileSystem
{
    public IEnumerable<string> GetDirectories(string path, int depth)
    {
        if (!Directory.Exists(path)) return Enumerable.Empty<string>();
        return depth == 1
            ? Directory.GetDirectories(path)
            : Directory.GetDirectories(path)
                       .SelectMany(d => GetDirectories(d, depth - 1));
    }

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public IEnumerable<string> GetFiles(string path, string searchPattern = "*", bool recurse = false)
        => Directory.GetFiles(path, searchPattern,
               recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

    public DateTime GetLastWriteTimeUtc(string path) => File.GetLastWriteTimeUtc(path);
    public long GetFileSize(string path) => new FileInfo(path).Length;
}
