namespace PropertyMatterHub.Core.Models;

public class KeyDate
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public KeyDateSeverity Severity { get; set; } = KeyDateSeverity.Normal;
    public string? Notes { get; set; }

    public int MatterId { get; set; }
    public Matter Matter { get; set; } = null!;

    public int? LinkedCalendarEventId { get; set; }
    public CalendarEvent? LinkedCalendarEvent { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum KeyDateSeverity
{
    Normal,
    Warning,
    Critical
}
