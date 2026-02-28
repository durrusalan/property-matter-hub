namespace PropertyMatterHub.Core.Models;

public class SyncLog
{
    public int Id { get; set; }
    public string ResourceType { get; set; } = string.Empty;    // "Excel", "GoogleCalendar", "Gmail"
    public string ResourceKey { get; set; } = string.Empty;     // file path, sync token, etc.
    public DateTime LastSyncedAt { get; set; }
    public string? Metadata { get; set; }   // JSON blob for extra state (e.g. row hashes)
}
