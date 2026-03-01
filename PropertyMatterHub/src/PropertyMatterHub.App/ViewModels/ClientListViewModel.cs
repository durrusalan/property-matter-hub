using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PropertyMatterHub.Core.Models;
using PropertyMatterHub.Core.Services;

namespace PropertyMatterHub.App.ViewModels;

public partial class ClientListViewModel : ObservableObject
{
    private readonly ClientService _service;

    [ObservableProperty] private IReadOnlyList<Client> _clients = [];
    [ObservableProperty] private Client? _selectedClient;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private bool _isLoading;

    public ClientListViewModel(ClientService service) => _service = service;

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        try { Clients = await _service.GetAllAsync(); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void NewClient() { SelectedClient = new Client(); IsEditing = true; }

    [RelayCommand]
    private void EditClient(Client c) { SelectedClient = c; IsEditing = true; }

    [RelayCommand]
    private void Cancel() { SelectedClient = null; IsEditing = false; }

    [RelayCommand]
    private async Task SaveClientAsync()
    {
        if (SelectedClient is null) return;
        if (SelectedClient.Id == 0)
            await _service.CreateClientAsync(SelectedClient);
        else
            await _service.UpdateClientAsync(SelectedClient);

        IsEditing = false;
        await LoadAsync();
    }
}
