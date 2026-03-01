using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;

namespace PropertyMatterHub.Infrastructure.Google;

/// <summary>
/// Production IGoogleCalendarClient backed by the real Google.Apis.Calendar.v3.CalendarService.
/// Credentials are obtained lazily via GoogleAuthService.
/// </summary>
public sealed class LiveGoogleCalendarClient : IGoogleCalendarClient
{
    private const string PrimaryCalendar = "primary";

    private readonly GoogleAuthService _auth;
    private CalendarService? _service;

    public LiveGoogleCalendarClient(GoogleAuthService auth) => _auth = auth;

    public async Task<IReadOnlyList<Event>> ListAsync(
        DateTime from, DateTime to, CancellationToken ct)
    {
        var svc = await GetServiceAsync(ct);
        var req  = svc.Events.List(PrimaryCalendar);
        req.TimeMinDateTimeOffset = from;
        req.TimeMaxDateTimeOffset = to;
        req.SingleEvents          = true;
        req.OrderBy               = EventsResource.ListRequest.OrderByEnum.StartTime;
        req.MaxResults            = 250;

        var response = await req.ExecuteAsync(ct);
        return (IReadOnlyList<Event>)response.Items ?? [];
    }

    public async Task<Event> CreateAsync(Event ev, CancellationToken ct)
    {
        var svc = await GetServiceAsync(ct);
        return await svc.Events.Insert(ev, PrimaryCalendar).ExecuteAsync(ct);
    }

    public async Task<Event> UpdateAsync(string eventId, Event ev, CancellationToken ct)
    {
        var svc = await GetServiceAsync(ct);
        return await svc.Events.Update(ev, PrimaryCalendar, eventId).ExecuteAsync(ct);
    }

    public async Task DeleteAsync(string eventId, CancellationToken ct)
    {
        var svc = await GetServiceAsync(ct);
        await svc.Events.Delete(PrimaryCalendar, eventId).ExecuteAsync(ct);
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private async Task<CalendarService> GetServiceAsync(CancellationToken ct)
    {
        if (_service is not null)
            return _service;

        var credential = await _auth.GetCredentialAsync(ct);
        _service = new CalendarService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName       = "PropertyMatterHub"
        });
        return _service;
    }
}
