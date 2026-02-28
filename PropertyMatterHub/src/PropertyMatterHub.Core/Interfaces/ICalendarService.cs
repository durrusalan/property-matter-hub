using PropertyMatterHub.Core.Models;

namespace PropertyMatterHub.Core.Interfaces;

public interface ICalendarService
{
    Task<IReadOnlyList<CalendarEvent>> GetEventsAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<IReadOnlyList<CalendarEvent>> GetEventsForMatterAsync(int matterId, CancellationToken ct = default);
    Task<CalendarEvent> CreateEventAsync(CalendarEvent calendarEvent, CancellationToken ct = default);
    Task UpdateEventAsync(CalendarEvent calendarEvent, CancellationToken ct = default);
    Task DeleteEventAsync(int id, CancellationToken ct = default);
    Task SyncWithGoogleAsync(CancellationToken ct = default);
}
