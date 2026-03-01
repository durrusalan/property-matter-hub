using Google.Apis.Calendar.v3.Data;
using PropertyMatterHub.Core.Models;

namespace PropertyMatterHub.Infrastructure.Google;

/// <summary>Pure static mapping — Google Calendar Event ↔ CalendarEvent.</summary>
public static class GoogleCalendarMapper
{
    public static CalendarEvent ToCalendarEvent(Event gEvent)
    {
        var isAllDay = gEvent.Start?.Date is not null;

        return new CalendarEvent
        {
            GoogleEventId  = gEvent.Id,
            Title          = gEvent.Summary ?? string.Empty,
            Description    = gEvent.Description,
            StartUtc       = ResolveDateTime(gEvent.Start!, isAllDay, DateTime.UtcNow),
            EndUtc         = ResolveDateTime(gEvent.End!,   isAllDay, DateTime.UtcNow.AddHours(1)),
            IsAllDay       = isAllDay,
            Location       = gEvent.Location,
            SyncStatus     = CalendarEventSyncStatus.Synced,
            LastSyncedUtc  = DateTime.UtcNow,
            CreatedAt      = DateTime.UtcNow,
            UpdatedAt      = DateTime.UtcNow
        };
    }

    public static Event ToGoogleEvent(CalendarEvent local) => new()
    {
        Id          = local.GoogleEventId,
        Summary     = local.Title,
        Description = local.Description,
        Location    = local.Location,
        Start       = ToEventDateTime(local.StartUtc, local.IsAllDay),
        End         = ToEventDateTime(local.EndUtc,   local.IsAllDay)
    };

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Resolves a Google EventDateTime to a UTC DateTime.
    /// All-day events carry only a date string; timed events carry a DateTimeOffset.
    /// </summary>
    private static DateTime ResolveDateTime(EventDateTime edt, bool isAllDay, DateTime fallback) =>
        isAllDay
            ? DateOnly.Parse(edt.Date).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
            : edt.DateTimeDateTimeOffset?.UtcDateTime ?? fallback;

    /// <summary>
    /// Converts a UTC DateTime to a Google EventDateTime, using Date-only
    /// format for all-day events and a full DateTimeOffset for timed ones.
    /// </summary>
    private static EventDateTime ToEventDateTime(DateTime dt, bool isAllDay) =>
        isAllDay
            ? new EventDateTime { Date = dt.ToString("yyyy-MM-dd") }
            : new EventDateTime { DateTimeDateTimeOffset = dt };
}
