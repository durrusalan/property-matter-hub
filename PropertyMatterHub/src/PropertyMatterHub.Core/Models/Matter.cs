namespace PropertyMatterHub.Core.Models;

public class Matter
{
    public int Id { get; set; }
    public string MatterRef { get; set; } = string.Empty;   // e.g. PROP-2026-0042
    public string Title { get; set; } = string.Empty;
    public string PracticeArea { get; set; } = string.Empty;
    public MatterStatus Status { get; set; } = MatterStatus.Active;

    public int ClientId { get; set; }
    public Client Client { get; set; } = null!;

    public string? FolderPath { get; set; }     // Z: drive path
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public ICollection<KeyDate> KeyDates { get; set; } = new List<KeyDate>();
    public ICollection<EmailRecord> Emails { get; set; } = new List<EmailRecord>();
    public ICollection<CalendarEvent> CalendarEvents { get; set; } = new List<CalendarEvent>();
}

public enum MatterStatus
{
    Active,
    Closed,
    Archived
}
