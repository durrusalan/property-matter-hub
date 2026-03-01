namespace PropertyMatterHub.Infrastructure.FileSystem;

public record FolderStructureConfig
{
    public string RootPath          { get; init; } = string.Empty;
    /// <summary>Regex with named groups: ClientName, CaseNumber.</summary>
    public string CaseFolderPattern { get; init; } = @"^(?<ClientName>.+?)\s*-\s*(?<CaseNumber>.+)$";
    /// <summary>1 = direct children of root, 2 = one grouping level (e.g. year folders).</summary>
    public int    CaseFolderDepth   { get; init; } = 1;
    /// <summary>Optional: subfolder name → role mapping (e.g. "Emails" → "emails").</summary>
    public Dictionary<string, string> SubFolderRoles { get; init; } = new();
}
