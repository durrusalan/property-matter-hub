namespace PropertyMatterHub.Core.Models;

public class Template
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string PracticeArea { get; set; } = "All";
    public string? TagsJson { get; set; }   // JSON array of tag strings
    public DateTime LastModified { get; set; }
    public DateTime IndexedAt { get; set; } = DateTime.UtcNow;
}
