using FluentAssertions;
using Google.Apis.Calendar.v3.Data;
using PropertyMatterHub.Core.Models;
using PropertyMatterHub.Infrastructure.Google;
using Xunit;

namespace PropertyMatterHub.Infrastructure.Tests;

/// <summary>
/// RED tests for GoogleCalendarMapper — pure static mapping, no API calls.
/// </summary>
public class GoogleCalendarMapperTests
{
    [Fact]
    public void ToCalendarEvent_MapsGoogleEventId()
    {
        var gEvent = MakeEvent(id: "google-evt-001");

        var evt = GoogleCalendarMapper.ToCalendarEvent(gEvent);

        evt.GoogleEventId.Should().Be("google-evt-001");
    }

    [Fact]
    public void ToCalendarEvent_MapsSummaryToTitle()
    {
        var gEvent = MakeEvent(summary: "Completion meeting – Murphy");

        var evt = GoogleCalendarMapper.ToCalendarEvent(gEvent);

        evt.Title.Should().Be("Completion meeting – Murphy");
    }

    [Fact]
    public void ToCalendarEvent_MapsDescription()
    {
        var gEvent = MakeEvent(description: "Bring signed contracts");

        var evt = GoogleCalendarMapper.ToCalendarEvent(gEvent);

        evt.Description.Should().Be("Bring signed contracts");
    }

    [Fact]
    public void ToCalendarEvent_MapsStartAndEndUtc()
    {
        var start = new DateTime(2026, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var end   = new DateTime(2026, 6, 15, 11, 0, 0, DateTimeKind.Utc);
        var gEvent = MakeEvent(start: start, end: end);

        var evt = GoogleCalendarMapper.ToCalendarEvent(gEvent);

        evt.StartUtc.Should().Be(start);
        evt.EndUtc.Should().Be(end);
    }

    [Fact]
    public void ToCalendarEvent_SetsIsAllDay_ForDateOnlyEvent()
    {
        var gEvent = new Event
        {
            Id      = "allday",
            Summary = "All Day Event",
            Start   = new EventDateTime { Date = "2026-06-15" },
            End     = new EventDateTime { Date = "2026-06-16" }
        };

        var evt = GoogleCalendarMapper.ToCalendarEvent(gEvent);

        evt.IsAllDay.Should().BeTrue();
    }

    [Fact]
    public void ToCalendarEvent_SetsIsAllDay_FalseForTimedEvent()
    {
        var gEvent = MakeEvent(
            start: new DateTime(2026, 6, 15, 10, 0, 0, DateTimeKind.Utc),
            end:   new DateTime(2026, 6, 15, 11, 0, 0, DateTimeKind.Utc));

        var evt = GoogleCalendarMapper.ToCalendarEvent(gEvent);

        evt.IsAllDay.Should().BeFalse();
    }

    [Fact]
    public void ToCalendarEvent_SetsSyncStatusToSynced()
    {
        var evt = GoogleCalendarMapper.ToCalendarEvent(MakeEvent());

        evt.SyncStatus.Should().Be(CalendarEventSyncStatus.Synced);
    }

    [Fact]
    public void ToGoogleEvent_MapsLocalCalendarEventToGoogleFormat()
    {
        var local = new CalendarEvent
        {
            Title       = "Closing meeting",
            Description = "Final sign-off",
            StartUtc    = new DateTime(2026, 7, 1, 9, 0, 0, DateTimeKind.Utc),
            EndUtc      = new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc),
            IsAllDay    = false
        };

        var gEvent = GoogleCalendarMapper.ToGoogleEvent(local);

        gEvent.Summary.Should().Be("Closing meeting");
        gEvent.Description.Should().Be("Final sign-off");
        gEvent.Start.DateTimeDateTimeOffset.Should().NotBeNull();
    }

    [Fact]
    public void ToGoogleEvent_MapsAllDayEvent_UsingDateNotDateTime()
    {
        var local = new CalendarEvent
        {
            Title    = "Bank Holiday",
            StartUtc = new DateTime(2026, 8, 3, 0, 0, 0, DateTimeKind.Utc),
            EndUtc   = new DateTime(2026, 8, 4, 0, 0, 0, DateTimeKind.Utc),
            IsAllDay = true
        };

        var gEvent = GoogleCalendarMapper.ToGoogleEvent(local);

        gEvent.Start.Date.Should().Be("2026-08-03");
        gEvent.End.Date.Should().Be("2026-08-04");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Event MakeEvent(
        string  id          = "evt-001",
        string  summary     = "Test Event",
        string? description = null,
        DateTime? start     = null,
        DateTime? end       = null)
    {
        var s = start ?? new DateTime(2026, 6, 1, 9, 0, 0, DateTimeKind.Utc);
        var e = end   ?? s.AddHours(1);

        return new Event
        {
            Id          = id,
            Summary     = summary,
            Description = description,
            Start       = new EventDateTime { DateTimeDateTimeOffset = s },
            End         = new EventDateTime { DateTimeDateTimeOffset = e }
        };
    }
}
