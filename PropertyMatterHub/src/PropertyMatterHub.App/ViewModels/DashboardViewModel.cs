using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PropertyMatterHub.Core.Interfaces;
using PropertyMatterHub.Core.Models;

namespace PropertyMatterHub.App.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IMatterRepository _matters;
    private readonly ICalendarService _calendar;

    [ObservableProperty] private IReadOnlyList<Matter> _activeMatters = [];
    [ObservableProperty] private IReadOnlyList<CalendarEvent> _todaysEvents = [];
    [ObservableProperty] private IReadOnlyList<CalendarEvent> _upcomingEvents = [];
    [ObservableProperty] private IReadOnlyList<KeyDate> _upcomingDeadlines = [];
    [ObservableProperty] private bool _isLoading;

    public DashboardViewModel(IMatterRepository matters, ICalendarService calendar)
    {
        _matters  = matters;
        _calendar = calendar;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            ActiveMatters = await _matters.GetActiveAsync();

            var now   = DateTime.Now;
            var week  = now.AddDays(7);
            var events = await _calendar.GetEventsAsync(now, week);

            TodaysEvents   = events.Where(e => e.StartUtc.Date == now.Date).ToList();
            UpcomingEvents = events.Where(e => e.StartUtc.Date > now.Date).ToList();
        }
        finally { IsLoading = false; }
    }
}
