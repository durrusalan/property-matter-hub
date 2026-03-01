using FluentAssertions;
using Google.Apis.Calendar.v3.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PropertyMatterHub.Core.Models;
using PropertyMatterHub.Infrastructure.Data;
using PropertyMatterHub.Infrastructure.Google;
using Xunit;

namespace PropertyMatterHub.Infrastructure.Tests;

/// <summary>
/// RED tests for GoogleCalendarService.
/// Uses in-memory SQLite + a mocked IGoogleCalendarClient.
/// </summary>
public class GoogleCalendarServiceTests : IAsyncLifetime
{
    private readonly IGoogleCalendarClient _client = Substitute.For<IGoogleCalendarClient>();
    private AppDbContext _db = null!;

    public async Task InitializeAsync()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        _db = new AppDbContext(opts);
        await _db.Database.OpenConnectionAsync();   // keep connection open (in-memory db lives on the connection)
        await _db.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync() => await _db.DisposeAsync();

    private GoogleCalendarService BuildSut() =>
        new(_client, _db, NullLogger<GoogleCalendarService>.Instance);

    // ── GetEventsAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetEventsAsync_ReturnsEvents_InDateRange()
    {
        _db.CalendarEvents.AddRange(
            new CalendarEvent { Title = "Within",  StartUtc = new DateTime(2025, 6, 10), EndUtc = new DateTime(2025, 6, 10, 1, 0, 0) },
            new CalendarEvent { Title = "Outside", StartUtc = new DateTime(2025, 8,  1), EndUtc = new DateTime(2025, 8,  1, 1, 0, 0) });
        await _db.SaveChangesAsync();

        var result = await BuildSut().GetEventsAsync(
            new DateTime(2025, 6, 1), new DateTime(2025, 7, 1));

        result.Should().ContainSingle(e => e.Title == "Within");
        result.Should().NotContain(e => e.Title == "Outside");
    }

    [Fact]
    public async Task GetEventsAsync_ReturnsEmpty_WhenNoneInRange()
    {
        var result = await BuildSut().GetEventsAsync(DateTime.Today, DateTime.Today.AddDays(7));
        result.Should().BeEmpty();
    }

    // ── GetEventsForMatterAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetEventsForMatterAsync_ReturnsOnlyLinkedEvents()
    {
        var client = new Client { Name = "ACME" };
        _db.Clients.Add(client);
        await _db.SaveChangesAsync();

        var matter = new Matter { Title = "Case A", ClientId = client.Id };
        _db.Matters.Add(matter);
        await _db.SaveChangesAsync();

        _db.CalendarEvents.AddRange(
            new CalendarEvent { Title = "Linked",    MatterId = matter.Id, StartUtc = DateTime.UtcNow, EndUtc = DateTime.UtcNow.AddHours(1) },
            new CalendarEvent { Title = "Unlinked",  StartUtc = DateTime.UtcNow, EndUtc = DateTime.UtcNow.AddHours(1) });
        await _db.SaveChangesAsync();

        var result = await BuildSut().GetEventsForMatterAsync(matter.Id);

        result.Should().ContainSingle(e => e.Title == "Linked");
    }

    // ── CreateEventAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task CreateEventAsync_PersistsEventInDatabase()
    {
        _client.CreateAsync(Arg.Any<Event>(), Arg.Any<CancellationToken>())
               .Returns(new Event { Id = "g-new-1" });

        var ev = new CalendarEvent
        {
            Title    = "Court Hearing",
            StartUtc = new DateTime(2025, 9, 1, 10, 0, 0),
            EndUtc   = new DateTime(2025, 9, 1, 11, 0, 0)
        };

        var saved = await BuildSut().CreateEventAsync(ev);

        saved.Id.Should().BeGreaterThan(0);
        (await _db.CalendarEvents.FindAsync(saved.Id)).Should().NotBeNull();
    }

    [Fact]
    public async Task CreateEventAsync_PushesToGoogleAndStoresGoogleEventId()
    {
        _client.CreateAsync(Arg.Any<Event>(), Arg.Any<CancellationToken>())
               .Returns(new Event { Id = "g-abc-123" });

        var saved = await BuildSut().CreateEventAsync(new CalendarEvent
        {
            Title    = "Filing Deadline",
            StartUtc = new DateTime(2025, 9, 5, 9, 0, 0),
            EndUtc   = new DateTime(2025, 9, 5, 10, 0, 0)
        });

        saved.GoogleEventId.Should().Be("g-abc-123");
        saved.SyncStatus.Should().Be(CalendarEventSyncStatus.Synced);
        await _client.Received(1).CreateAsync(Arg.Any<Event>(), Arg.Any<CancellationToken>());
    }

    // ── UpdateEventAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateEventAsync_UpdatesTitleAndDescription()
    {
        _db.CalendarEvents.Add(new CalendarEvent
        {
            Title    = "Original",
            StartUtc = DateTime.UtcNow,
            EndUtc   = DateTime.UtcNow.AddHours(1),
            GoogleEventId = "g-upd-1"
        });
        await _db.SaveChangesAsync();

        var existing = await _db.CalendarEvents.FirstAsync();
        existing.Title       = "Updated Title";
        existing.Description = "New description";

        await BuildSut().UpdateEventAsync(existing);

        var reloaded = await _db.CalendarEvents.FindAsync(existing.Id);
        reloaded!.Title.Should().Be("Updated Title");
        reloaded.Description.Should().Be("New description");
    }

    [Fact]
    public async Task UpdateEventAsync_PushesToGoogle_WhenGoogleEventIdPresent()
    {
        _db.CalendarEvents.Add(new CalendarEvent
        {
            Title    = "Meeting",
            StartUtc = DateTime.UtcNow,
            EndUtc   = DateTime.UtcNow.AddHours(1),
            GoogleEventId = "g-push-me"
        });
        await _db.SaveChangesAsync();

        var ev = await _db.CalendarEvents.FirstAsync();
        ev.Title = "Rescheduled Meeting";

        await BuildSut().UpdateEventAsync(ev);

        await _client.Received(1).UpdateAsync(
            "g-push-me", Arg.Any<Event>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateEventAsync_DoesNotCallGoogle_WhenNoGoogleEventId()
    {
        _db.CalendarEvents.Add(new CalendarEvent
        {
            Title    = "Local only",
            StartUtc = DateTime.UtcNow,
            EndUtc   = DateTime.UtcNow.AddHours(1)
            // GoogleEventId intentionally null
        });
        await _db.SaveChangesAsync();

        var ev = await _db.CalendarEvents.FirstAsync();
        ev.Title = "Still local";

        await BuildSut().UpdateEventAsync(ev);

        await _client.DidNotReceive().UpdateAsync(
            Arg.Any<string>(), Arg.Any<Event>(), Arg.Any<CancellationToken>());
    }

    // ── DeleteEventAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteEventAsync_RemovesFromDatabase()
    {
        _db.CalendarEvents.Add(new CalendarEvent
        {
            Title    = "To delete",
            StartUtc = DateTime.UtcNow,
            EndUtc   = DateTime.UtcNow.AddHours(1)
        });
        await _db.SaveChangesAsync();
        var id = (await _db.CalendarEvents.FirstAsync()).Id;

        await BuildSut().DeleteEventAsync(id);

        (await _db.CalendarEvents.FindAsync(id)).Should().BeNull();
    }

    [Fact]
    public async Task DeleteEventAsync_CallsGoogleDelete_WhenGoogleEventIdPresent()
    {
        _db.CalendarEvents.Add(new CalendarEvent
        {
            Title    = "To delete from Google",
            StartUtc = DateTime.UtcNow,
            EndUtc   = DateTime.UtcNow.AddHours(1),
            GoogleEventId = "g-del-1"
        });
        await _db.SaveChangesAsync();
        var id = (await _db.CalendarEvents.FirstAsync()).Id;

        await BuildSut().DeleteEventAsync(id);

        await _client.Received(1).DeleteAsync("g-del-1", Arg.Any<CancellationToken>());
    }

    // ── SyncWithGoogleAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task SyncWithGoogleAsync_CreatesNewLocalRecords_ForUnknownGoogleEvents()
    {
        _client.ListAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
               .Returns([new Event { Id = "g-brand-new", Summary = "Remote event",
                   Start = new EventDateTime { DateTimeDateTimeOffset = DateTimeOffset.UtcNow },
                   End   = new EventDateTime { DateTimeDateTimeOffset = DateTimeOffset.UtcNow.AddHours(1) } }]);

        await BuildSut().SyncWithGoogleAsync();

        _db.CalendarEvents.Should().ContainSingle(e => e.GoogleEventId == "g-brand-new");
    }

    [Fact]
    public async Task SyncWithGoogleAsync_UpdatesExistingRecord_WhenGoogleEventIdMatches()
    {
        _db.CalendarEvents.Add(new CalendarEvent
        {
            GoogleEventId = "g-existing",
            Title         = "Old title",
            StartUtc      = DateTime.UtcNow,
            EndUtc        = DateTime.UtcNow.AddHours(1),
            SyncStatus    = CalendarEventSyncStatus.Synced
        });
        await _db.SaveChangesAsync();

        _client.ListAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
               .Returns([new Event { Id = "g-existing", Summary = "Updated title",
                   Start = new EventDateTime { DateTimeDateTimeOffset = DateTimeOffset.UtcNow },
                   End   = new EventDateTime { DateTimeDateTimeOffset = DateTimeOffset.UtcNow.AddHours(1) } }]);

        await BuildSut().SyncWithGoogleAsync();

        var ev = await _db.CalendarEvents.FirstAsync(e => e.GoogleEventId == "g-existing");
        ev.Title.Should().Be("Updated title");
    }

    [Fact]
    public async Task SyncWithGoogleAsync_PushesPendingLocalEvents_ToGoogle()
    {
        _client.ListAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
               .Returns([]);
        _client.CreateAsync(Arg.Any<Event>(), Arg.Any<CancellationToken>())
               .Returns(new Event { Id = "g-pushed-1" });

        _db.CalendarEvents.Add(new CalendarEvent
        {
            Title      = "Pending",
            StartUtc   = DateTime.UtcNow,
            EndUtc     = DateTime.UtcNow.AddHours(1),
            SyncStatus = CalendarEventSyncStatus.PendingPush
        });
        await _db.SaveChangesAsync();

        await BuildSut().SyncWithGoogleAsync();

        await _client.Received(1).CreateAsync(Arg.Any<Event>(), Arg.Any<CancellationToken>());
        var ev = await _db.CalendarEvents.FirstAsync();
        ev.GoogleEventId.Should().Be("g-pushed-1");
        ev.SyncStatus.Should().Be(CalendarEventSyncStatus.Synced);
    }
}
