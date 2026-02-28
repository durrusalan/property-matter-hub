namespace PropertyMatterHub.Core.Models;

public class Document
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string FileHash { get; set; } = string.Empty;   // SHA256 of content
    public string? SearchableContent { get; set; }          // Extracted text for FTS
    public string? ContentType { get; set; }                // docx, pdf, etc.
    public DateTime LastModified { get; set; }
    public DateTime IndexedAt { get; set; } = DateTime.UtcNow;

    public int MatterId { get; set; }
    public Matter Matter { get; set; } = null!;
}
