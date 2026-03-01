using Google.Apis.Calendar.v3.Data;

namespace PropertyMatterHub.Infrastructure.Google;

/// <summary>
/// Abstracts raw Google Calendar API calls so GoogleCalendarService
/// can be unit-tested without real network access.
/// </summary>
public interface IGoogleCalendarClient
{
    /// <summary>Lists events whose start time falls within [from, to].</summary>
    Task<IReadOnlyList<Event>> ListAsync(DateTime from, DateTime to, CancellationToken ct);

    /// <summary>Creates a new event and returns the saved resource (with its Google Id).</summary>
    Task<Event> CreateAsync(Event ev, CancellationToken ct);

    /// <summary>Updates an existing event identified by <paramref name="eventId"/>.</summary>
    Task<Event> UpdateAsync(string eventId, Event ev, CancellationToken ct);

    /// <summary>Deletes the event with the given Google Calendar event Id.</summary>
    Task DeleteAsync(string eventId, CancellationToken ct);
}
