using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PropertyMatterHub.Core.Interfaces;
using PropertyMatterHub.Core.Models;
using PropertyMatterHub.Infrastructure.Data;

namespace PropertyMatterHub.Infrastructure.Google;

/// <summary>
/// Live implementation of ICalendarService backed by SQLite + Google Calendar API.
/// All raw API calls are delegated to IGoogleCalendarClient for testability.
/// </summary>
public sealed class GoogleCalendarService : ICalendarService
{
    // How far ahead we fetch during a full sync.
    private static readonly TimeSpan SyncWindow = TimeSpan.FromDays(90);

    private readonly IGoogleCalendarClient _client;
    private readonly AppDbContext _db;
    private readonly ILogger<GoogleCalendarService> _logger;

    public GoogleCalendarService(
        IGoogleCalendarClient client,
        AppDbContext db,
        ILogger<GoogleCalendarService> logger)
    {
        _client = client;
        _db     = db;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CalendarEvent>> GetEventsAsync(
        DateTime from, DateTime to, CancellationToken ct = default) =>
        await _db.CalendarEvents
            .Where(e => e.StartUtc >= from && e.StartUtc < to)
            .OrderBy(e => e.StartUtc)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<CalendarEvent>> GetEventsForMatterAsync(
        int matterId, CancellationToken ct = default) =>
        await _db.CalendarEvents
            .Where(e => e.MatterId == matterId)
            .OrderBy(e => e.StartUtc)
            .ToListAsync(ct);

    public async Task<CalendarEvent> CreateEventAsync(
        CalendarEvent calendarEvent, CancellationToken ct = default)
    {
        var gEvent = await _client.CreateAsync(
            GoogleCalendarMapper.ToGoogleEvent(calendarEvent), ct);

        calendarEvent.GoogleEventId  = gEvent.Id;
        calendarEvent.SyncStatus     = CalendarEventSyncStatus.Synced;
        calendarEvent.LastSyncedUtc  = DateTime.UtcNow;

        _db.CalendarEvents.Add(calendarEvent);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Created event '{Title}' (Google id: {GId})",
            calendarEvent.Title, calendarEvent.GoogleEventId);
        return calendarEvent;
    }

    public async Task UpdateEventAsync(CalendarEvent calendarEvent, CancellationToken ct = default)
    {
        var record = await _db.CalendarEvents.FindAsync([calendarEvent.Id], ct)
            ?? throw new KeyNotFoundException($"CalendarEvent {calendarEvent.Id} not found.");

        record.Title       = calendarEvent.Title;
        record.Description = calendarEvent.Description;
        record.StartUtc    = calendarEvent.StartUtc;
        record.EndUtc      = calendarEvent.EndUtc;
        record.IsAllDay    = calendarEvent.IsAllDay;
        record.Location    = calendarEvent.Location;
        record.MatterId    = calendarEvent.MatterId;
        record.UpdatedAt   = DateTime.UtcNow;

        if (record.GoogleEventId is not null)
        {
            await _client.UpdateAsync(record.GoogleEventId,
                GoogleCalendarMapper.ToGoogleEvent(record), ct);
            record.SyncStatus    = CalendarEventSyncStatus.Synced;
            record.LastSyncedUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteEventAsync(int id, CancellationToken ct = default)
    {
        var record = await _db.CalendarEvents.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"CalendarEvent {id} not found.");

        if (record.GoogleEventId is not null)
            await _client.DeleteAsync(record.GoogleEventId, ct);

        _db.CalendarEvents.Remove(record);
        await _db.SaveChangesAsync(ct);
    }

    public async Task SyncWithGoogleAsync(CancellationToken ct = default)
    {
        var now  = DateTime.UtcNow;
        var from = now.Date;
        var to   = now + SyncWindow;

        // ── Pull: fetch Google events, upsert locally ─────────────────────
        var googleEvents = await _client.ListAsync(from, to, ct);

        var knownIds = (await _db.CalendarEvents
            .Select(e => e.GoogleEventId)
            .Where(id => id != null)
            .ToListAsync(ct))
            .ToHashSet();

        foreach (var gEvent in googleEvents)
        {
            var mapped = GoogleCalendarMapper.ToCalendarEvent(gEvent);

            if (knownIds.Contains(gEvent.Id))
            {
                var existing = await _db.CalendarEvents
                    .FirstAsync(e => e.GoogleEventId == gEvent.Id, ct);
                existing.Title        = mapped.Title;
                existing.Description  = mapped.Description;
                existing.StartUtc     = mapped.StartUtc;
                existing.EndUtc       = mapped.EndUtc;
                existing.IsAllDay     = mapped.IsAllDay;
                existing.Location     = mapped.Location;
                existing.SyncStatus   = CalendarEventSyncStatus.Synced;
                existing.LastSyncedUtc = DateTime.UtcNow;
            }
            else
            {
                _db.CalendarEvents.Add(mapped);
            }
        }

        // ── Push: send PendingPush local events to Google ─────────────────
        var pending = await _db.CalendarEvents
            .Where(e => e.SyncStatus == CalendarEventSyncStatus.PendingPush)
            .ToListAsync(ct);

        foreach (var local in pending)
        {
            var created = await _client.CreateAsync(
                GoogleCalendarMapper.ToGoogleEvent(local), ct);
            local.GoogleEventId  = created.Id;
            local.SyncStatus     = CalendarEventSyncStatus.Synced;
            local.LastSyncedUtc  = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Calendar sync complete: {Pull} pulled, {Push} pushed.",
            googleEvents.Count, pending.Count);
    }
}
