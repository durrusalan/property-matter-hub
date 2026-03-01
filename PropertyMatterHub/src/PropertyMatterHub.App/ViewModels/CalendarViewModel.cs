using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PropertyMatterHub.Core.Interfaces;
using PropertyMatterHub.Core.Models;

namespace PropertyMatterHub.App.ViewModels;

public enum CalendarViewMode { Month, Week, Agenda }

public partial class CalendarViewModel : ObservableObject
{
    private readonly ICalendarService _calendar;

    [ObservableProperty] private IReadOnlyList<CalendarEvent> _events = [];
    [ObservableProperty] private CalendarViewMode _viewMode = CalendarViewMode.Agenda;
    [ObservableProperty] private DateTime _viewStart = DateTime.Today;
    [ObservableProperty] private CalendarEvent? _selectedEvent;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private DateTime _lastSyncedAt;
    [ObservableProperty] private string _syncStatus = "Not synced";

    public CalendarViewModel(ICalendarService calendar) => _calendar = calendar;

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var (from, to) = GetViewRange();
            Events = await _calendar.GetEventsAsync(from, to);
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task SyncAsync()
    {
        SyncStatus = "Syncing...";
        await _calendar.SyncWithGoogleAsync();
        LastSyncedAt = DateTime.Now;
        SyncStatus   = $"Synced {LastSyncedAt:HH:mm}";
        await LoadAsync();
    }

    [RelayCommand]
    private void NewEvent()
    {
        SelectedEvent = new CalendarEvent { StartUtc = DateTime.UtcNow, EndUtc = DateTime.UtcNow.AddHours(1) };
        IsEditing = true;
    }

    [RelayCommand]
    private async Task SaveEventAsync()
    {
        if (SelectedEvent is null) return;
        if (SelectedEvent.Id == 0)
            await _calendar.CreateEventAsync(SelectedEvent);
        else
            await _calendar.UpdateEventAsync(SelectedEvent);
        IsEditing = false;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteEventAsync(CalendarEvent? ev)
    {
        var target = ev ?? SelectedEvent;
        if (target is null) return;
        await _calendar.DeleteEventAsync(target.Id);
        if (SelectedEvent?.Id == target.Id) SelectedEvent = null;
        await LoadAsync();
    }

    /// <summary>Opens the edit form for an existing event clicked in the agenda.</summary>
    [RelayCommand]
    private void EditEvent(CalendarEvent? ev)
    {
        if (ev is not null) SelectedEvent = ev;
        IsEditing = true;
    }

    [RelayCommand]
    private void CancelEdit() => IsEditing = false;

    [RelayCommand]
    private void SetViewMode(CalendarViewMode mode) { ViewMode = mode; _ = LoadAsync(); }

    [RelayCommand]
    private void Previous() { ViewStart = ViewMode == CalendarViewMode.Month ? ViewStart.AddMonths(-1) : ViewStart.AddDays(-7); _ = LoadAsync(); }

    [RelayCommand]
    private void Next()     { ViewStart = ViewMode == CalendarViewMode.Month ? ViewStart.AddMonths(1)  : ViewStart.AddDays(7);  _ = LoadAsync(); }

    private (DateTime from, DateTime to) GetViewRange() => ViewMode switch
    {
        CalendarViewMode.Month  => (new DateTime(ViewStart.Year, ViewStart.Month, 1), new DateTime(ViewStart.Year, ViewStart.Month, 1).AddMonths(1)),
        CalendarViewMode.Week   => (ViewStart, ViewStart.AddDays(7)),
        CalendarViewMode.Agenda => (DateTime.Today, DateTime.Today.AddDays(30)),
        _ => (DateTime.Today, DateTime.Today.AddDays(30))
    };
}
