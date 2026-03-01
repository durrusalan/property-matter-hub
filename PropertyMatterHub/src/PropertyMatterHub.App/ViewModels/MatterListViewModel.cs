using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PropertyMatterHub.Core.Interfaces;
using PropertyMatterHub.Core.Models;

namespace PropertyMatterHub.App.ViewModels;

public partial class MatterListViewModel : ObservableObject
{
    private readonly IMatterRepository _repo;

    [ObservableProperty] private IReadOnlyList<Matter> _matters = [];
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _showActiveOnly = true;

    public event Action<int>? MatterSelected;

    public MatterListViewModel(IMatterRepository repo) => _repo = repo;

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            Matters = ShowActiveOnly
                ? await _repo.GetActiveAsync()
                : await _repo.GetAllAsync();
        }
        finally { IsLoading = false; }
    }

    partial void OnShowActiveOnlyChanged(bool value) => LoadCommand.Execute(null);

    [RelayCommand]
    private void SelectMatter(Matter matter)
    {
        if (matter is not null)
            MatterSelected?.Invoke(matter.Id);
    }
}
