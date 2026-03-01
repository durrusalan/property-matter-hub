using CommunityToolkit.Mvvm.ComponentModel;
using PropertyMatterHub.Core.Models;
using PropertyMatterHub.Core.Services;

namespace PropertyMatterHub.App.ViewModels;

public partial class SearchViewModel : ObservableObject
{
    private readonly SearchService _service;

    [ObservableProperty] private IReadOnlyList<Matter> _matterResults = [];
    [ObservableProperty] private IReadOnlyList<Client> _clientResults = [];
    [ObservableProperty] private bool _hasResults;

    public event Action<int>? MatterSelected;

    public SearchViewModel(SearchService service) => _service = service;

    public async Task RunSearchAsync(string query)
    {
        var results = await _service.SearchAllAsync(query);
        MatterResults = results.Matters;
        ClientResults = results.Clients;
        HasResults    = MatterResults.Count + ClientResults.Count > 0;
    }

    public void SelectMatter(int matterId) => MatterSelected?.Invoke(matterId);
}
