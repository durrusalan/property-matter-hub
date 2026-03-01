using System.Text.RegularExpressions;

namespace PropertyMatterHub.Infrastructure.FileSystem;

public record FolderScanEntry(
    string FolderPath,
    string FolderName,
    bool IsMatched,
    string? ClientName,
    string? CaseNumber
);

public record ParsedFolderName(string ClientName, string CaseNumber);

public class ZDriveScanner
{
    private readonly FolderStructureConfig _config;
    private readonly IFileSystem _fs;
    private readonly Regex _pattern;

    public ZDriveScanner(FolderStructureConfig config, IFileSystem? fs = null)
    {
        _config  = config;
        _fs      = fs ?? new RealFileSystem();
        _pattern = new Regex(config.CaseFolderPattern,
                             RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
    }

    /// <summary>
    /// Parses a folder name against the configured regex.
    /// Returns null if the name does not match.
    /// </summary>
    public ParsedFolderName? ParseFolderName(string folderName)
    {
        var m = _pattern.Match(folderName.Trim());
        if (!m.Success) return null;

        var clientName = m.Groups["ClientName"].Value.Trim();
        var caseNumber = m.Groups["CaseNumber"].Value.Trim();
        return new ParsedFolderName(clientName, caseNumber);
    }

    /// <summary>
    /// Scans the configured root path and returns one entry per discovered folder.
    /// Unmatched folders are included as uncategorized (IsMatched = false).
    /// </summary>
    public IReadOnlyList<FolderScanEntry> ScanFolders()
    {
        var results = new List<FolderScanEntry>();

        var directories = _fs.GetDirectories(_config.RootPath, _config.CaseFolderDepth);
        foreach (var dir in directories)
        {
            var name   = Path.GetFileName(dir);
            var parsed = ParseFolderName(name);
            results.Add(new FolderScanEntry(
                FolderPath:  dir,
                FolderName:  name,
                IsMatched:   parsed is not null,
                ClientName:  parsed?.ClientName,
                CaseNumber:  parsed?.CaseNumber
            ));
        }

        return results;
    }
}
