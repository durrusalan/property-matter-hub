using PropertyMatterHub.Infrastructure.FileSystem;

namespace PropertyMatterHub.Infrastructure.Tests;

/// <summary>
/// In-memory IFileSystem for unit-testing ZDriveScanner without touching disk.
/// Pass in the full paths of folders that should "exist".
/// </summary>
public class FakeFileSystem : IFileSystem
{
    private readonly IReadOnlyList<string> _dirs;
    private readonly int _depth;

    public FakeFileSystem(IEnumerable<string> dirs, int depth = 1)
    {
        _dirs  = dirs.ToList();
        _depth = depth;
    }

    public IEnumerable<string> GetDirectories(string path, int depth)
    {
        // Return all registered dirs that are at exactly 'depth' levels below path
        return _dirs.Where(d =>
        {
            if (!d.StartsWith(path, StringComparison.OrdinalIgnoreCase)) return false;
            var relative = d[path.Length..].Trim(Path.DirectorySeparatorChar, '/');
            var parts    = relative.Split(Path.DirectorySeparatorChar);
            return parts.Length == depth;
        });
    }

    public bool DirectoryExists(string path)
        => _dirs.Any(d => d.Equals(path, StringComparison.OrdinalIgnoreCase));

    public IEnumerable<string> GetFiles(string path, string searchPattern = "*", bool recurse = false)
        => Enumerable.Empty<string>();

    public DateTime GetLastWriteTimeUtc(string path) => DateTime.UtcNow;
    public long GetFileSize(string path) => 0;
}
