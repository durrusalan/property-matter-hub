using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PropertyMatterHub.Core.Interfaces;
using PropertyMatterHub.Core.Models;
using PropertyMatterHub.Core.Services;

namespace PropertyMatterHub.App.ViewModels;

public partial class MatterDetailViewModel : ObservableObject
{
    private readonly IMatterRepository _matters;
    private readonly IEmailService _email;
    private readonly ICalendarService _calendar;
    private readonly MatterService _matterService;

    [ObservableProperty] private Matter? _selectedMatter;
    [ObservableProperty] private IReadOnlyList<Document> _documents = [];
    [ObservableProperty] private IReadOnlyList<EmailRecord> _emails = [];
    [ObservableProperty] private IReadOnlyList<KeyDate> _keyDates = [];
    [ObservableProperty] private IReadOnlyList<CalendarEvent> _calendarEvents = [];
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _activeTab = "Overview";

    public MatterDetailViewModel(
        IMatterRepository matters,
        IEmailService email,
        ICalendarService calendar,
        MatterService matterService)
    {
        _matters       = matters;
        _email         = email;
        _calendar      = calendar;
        _matterService = matterService;
    }

    [RelayCommand]
    public async Task LoadMatterAsync(int matterId)
    {
        IsLoading = true;
        try
        {
            SelectedMatter  = await _matters.GetByIdAsync(matterId);
            if (SelectedMatter is null) return;

            Emails         = await _email.GetEmailsForMatterAsync(matterId);
            CalendarEvents = await _calendar.GetEventsForMatterAsync(matterId);
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (SelectedMatter is null) return;
        await _matterService.UpdateMatterAsync(SelectedMatter);
    }

    [RelayCommand]
    private void SetTab(string tab) => ActiveTab = tab;
}
