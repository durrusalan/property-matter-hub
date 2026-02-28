namespace PropertyMatterHub.Core.Models;

public class CalendarEvent
{
    public int Id { get; set; }
    public string? GoogleEventId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public bool IsAllDay { get; set; }
    public string? Location { get; set; }
    public string? AttendeesJson { get; set; }  // JSON array
    public string? Recurrence { get; set; }
    public string? SyncToken { get; set; }
    public DateTime? LastSyncedUtc { get; set; }
    public CalendarEventSyncStatus SyncStatus { get; set; } = CalendarEventSyncStatus.Local;

    public int? MatterId { get; set; }
    public Matter? Matter { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum CalendarEventSyncStatus
{
    Local,          // Not yet pushed to Google
    Synced,         // In sync with Google
    PendingPush,    // Local change waiting to be pushed
    Conflict        // Edited on both sides
}
