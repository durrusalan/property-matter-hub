using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PropertyMatterHub.App.ViewModels;

public enum NavigationPage
{
    Dashboard,
    Matters,
    Clients,
    Emails,
    Calendar,
    Settings
}

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] private NavigationPage _currentPage = NavigationPage.Dashboard;
    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private bool _isSearchOpen;
    [ObservableProperty] private string _statusMessage = string.Empty;

    public DashboardViewModel Dashboard { get; }
    public MatterListViewModel MatterList { get; }
    public MatterDetailViewModel MatterDetail { get; }
    public ClientListViewModel ClientList { get; }
    public EmailViewModel Email { get; }
    public CalendarViewModel Calendar { get; }
    public SettingsViewModel Settings { get; }
    public SearchViewModel Search { get; }

    public MainViewModel(
        DashboardViewModel dashboard,
        MatterListViewModel matterList,
        MatterDetailViewModel matterDetail,
        ClientListViewModel clientList,
        EmailViewModel email,
        CalendarViewModel calendar,
        SettingsViewModel settings,
        SearchViewModel search)
    {
        Dashboard    = dashboard;
        MatterList   = matterList;
        MatterDetail = matterDetail;
        ClientList   = clientList;
        Email        = email;
        Calendar     = calendar;
        Settings     = settings;
        Search       = search;
    }

    [RelayCommand]
    private void Navigate(NavigationPage page)
    {
        CurrentPage = page;
        MatterDetail.SelectedMatter = null;
        IsSearchOpen = false;
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery)) { IsSearchOpen = false; return; }
        await Search.RunSearchAsync(SearchQuery);
        IsSearchOpen = true;
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchQuery  = string.Empty;
        IsSearchOpen = false;
    }

    public void OpenMatter(int matterId)
    {
        MatterDetail.LoadMatterCommand.Execute(matterId);
        CurrentPage  = NavigationPage.Matters;
        IsSearchOpen = false;
    }
}
