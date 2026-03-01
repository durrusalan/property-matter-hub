using PropertyMatterHub.Core.Interfaces;
using PropertyMatterHub.Core.Models;

namespace PropertyMatterHub.App.Services;

/// <summary>No-op calendar service used before Google auth is configured.</summary>
public class NullCalendarService : ICalendarService
{
    public Task<IReadOnlyList<CalendarEvent>> GetEventsAsync(DateTime from, DateTime to, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<CalendarEvent>>([]);

    public Task<IReadOnlyList<CalendarEvent>> GetEventsForMatterAsync(int matterId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<CalendarEvent>>([]);

    public Task<CalendarEvent> CreateEventAsync(CalendarEvent calendarEvent, CancellationToken ct = default)
        => Task.FromResult(calendarEvent);

    public Task UpdateEventAsync(CalendarEvent calendarEvent, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task DeleteEventAsync(int id, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task SyncWithGoogleAsync(CancellationToken ct = default)
        => Task.CompletedTask;
}
