using FluentAssertions;
using NSubstitute;
using PropertyMatterHub.App.ViewModels;
using PropertyMatterHub.Core.Interfaces;
using PropertyMatterHub.Core.Models;
using Xunit;

namespace PropertyMatterHub.App.Tests;

/// <summary>
/// RED tests for CalendarViewModel — load, sync, event editing lifecycle.
/// </summary>
public class CalendarViewModelTests
{
    private readonly ICalendarService _calendar = Substitute.For<ICalendarService>();

    private CalendarViewModel BuildSut() => new(_calendar);

    // ── Load ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Load_PopulatesEventsFromService()
    {
        var events = new List<CalendarEvent>
        {
            new() { Title = "Hearing A", StartUtc = DateTime.UtcNow, EndUtc = DateTime.UtcNow.AddHours(1) },
            new() { Title = "Hearing B", StartUtc = DateTime.UtcNow.AddDays(3), EndUtc = DateTime.UtcNow.AddDays(3).AddHours(1) }
        };
        _calendar.GetEventsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
                 .Returns(events);

        var sut = BuildSut();
        await sut.LoadCommand.ExecuteAsync(null);

        sut.Events.Should().HaveCount(2);
    }

    [Fact]
    public async Task Load_SetsIsLoading_TrueThenFalse_ViaPropertyChanged()
    {
        // Track every IsLoading change via PropertyChanged for a full audit trail.
        var sut     = BuildSut();
        var changes = new List<bool>();
        sut.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(sut.IsLoading))
                changes.Add(sut.IsLoading);
        };

        _calendar.GetEventsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
                 .Returns([]);

        await sut.LoadAsync();

        changes.Should().ContainInOrder(true, false);
    }

    // ── Sync ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Sync_SetsSyncStatus_ToSyncing_ThenSynced()
    {
        _calendar.GetEventsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
                 .Returns([]);

        var sut = BuildSut();
        await sut.SyncCommand.ExecuteAsync(null);

        sut.SyncStatus.Should().StartWith("Synced");
    }

    [Fact]
    public async Task Sync_CallsServiceSync_ThenReloadsEvents()
    {
        _calendar.GetEventsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
                 .Returns([]);

        var sut = BuildSut();
        await sut.SyncCommand.ExecuteAsync(null);

        await _calendar.Received(1).SyncWithGoogleAsync(Arg.Any<CancellationToken>());
        await _calendar.Received().GetEventsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>());
    }

    // ── New / save event ──────────────────────────────────────────────────────

    [Fact]
    public void NewEvent_OpensEditorWithEmptyEvent()
    {
        var sut = BuildSut();
        sut.NewEventCommand.Execute(null);

        sut.IsEditing.Should().BeTrue();
        sut.SelectedEvent.Should().NotBeNull();
        sut.SelectedEvent!.Id.Should().Be(0);
    }

    [Fact]
    public async Task SaveEvent_CallsCreate_ForNewEvent()
    {
        _calendar.CreateEventAsync(Arg.Any<CalendarEvent>(), Arg.Any<CancellationToken>())
                 .Returns(x => x.ArgAt<CalendarEvent>(0));
        _calendar.GetEventsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
                 .Returns([]);

        var sut = BuildSut();
        sut.NewEventCommand.Execute(null);
        sut.SelectedEvent!.Title = "Test event";

        await sut.SaveEventCommand.ExecuteAsync(null);

        await _calendar.Received(1).CreateEventAsync(
            Arg.Is<CalendarEvent>(e => e.Title == "Test event"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveEvent_CallsUpdate_ForExistingEvent()
    {
        var existing = new CalendarEvent
        {
            Id       = 7,
            Title    = "Existing",
            StartUtc = DateTime.UtcNow,
            EndUtc   = DateTime.UtcNow.AddHours(1)
        };
        _calendar.GetEventsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
                 .Returns([]);

        var sut = BuildSut();
        sut.SelectedEvent = existing;
        existing.Title    = "Updated";

        await sut.SaveEventCommand.ExecuteAsync(null);

        await _calendar.Received(1).UpdateEventAsync(
            Arg.Is<CalendarEvent>(e => e.Id == 7),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveEvent_ClosesEditor_AfterSave()
    {
        _calendar.CreateEventAsync(Arg.Any<CalendarEvent>(), Arg.Any<CancellationToken>())
                 .Returns(x => x.ArgAt<CalendarEvent>(0));
        _calendar.GetEventsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
                 .Returns([]);

        var sut = BuildSut();
        sut.NewEventCommand.Execute(null);
        await sut.SaveEventCommand.ExecuteAsync(null);

        sut.IsEditing.Should().BeFalse();
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteEvent_CallsService_WithSelectedEventId()
    {
        _calendar.GetEventsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
                 .Returns([]);

        var sut = BuildSut();
        sut.SelectedEvent = new CalendarEvent { Id = 42, Title = "To remove",
            StartUtc = DateTime.UtcNow, EndUtc = DateTime.UtcNow.AddHours(1) };

        await sut.DeleteEventCommand.ExecuteAsync(null);

        await _calendar.Received(1).DeleteEventAsync(42, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteEvent_DoesNothing_WhenNoEventSelected()
    {
        var sut = BuildSut();
        await sut.DeleteEventCommand.ExecuteAsync(null);

        await _calendar.DidNotReceive().DeleteEventAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    // ── View-mode navigation ──────────────────────────────────────────────────

    [Fact]
    public void SetViewMode_ChangesViewMode()
    {
        var sut = BuildSut();
        _calendar.GetEventsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
                 .Returns([]);

        sut.SetViewModeCommand.Execute(CalendarViewMode.Week);

        sut.ViewMode.Should().Be(CalendarViewMode.Week);
    }

    [Fact]
    public void Previous_MovesViewStartBack_ByMonthInMonthMode()
    {
        _calendar.GetEventsAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>())
                 .Returns([]);

        var sut      = BuildSut();
        var original = sut.ViewStart;
        sut.SetViewModeCommand.Execute(CalendarViewMode.Month);
        sut.PreviousCommand.Execute(null);

        sut.ViewStart.Should().Be(original.AddMonths(-1));
    }
}
