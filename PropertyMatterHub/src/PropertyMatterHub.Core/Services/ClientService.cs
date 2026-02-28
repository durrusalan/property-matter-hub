using PropertyMatterHub.Core.Interfaces;
using PropertyMatterHub.Core.Models;

namespace PropertyMatterHub.Core.Services;

public class ClientService
{
    private readonly IClientRepository _clients;
    private readonly IExcelSyncService _excelSync;

    public ClientService(IClientRepository clients, IExcelSyncService excelSync)
    {
        _clients = clients;
        _excelSync = excelSync;
    }

    public Task<IReadOnlyList<Client>> GetAllAsync(CancellationToken ct = default)
        => _clients.GetAllAsync(ct);

    public Task<Client?> GetByIdAsync(int id, CancellationToken ct = default)
        => _clients.GetByIdAsync(id, ct);

    public Task<IReadOnlyList<Client>> SearchAsync(string query, CancellationToken ct = default)
        => _clients.SearchAsync(query, ct);

    public async Task<Client> CreateClientAsync(Client client, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(client.Name))
            throw new ArgumentException("Client name is required.", nameof(client));

        if (!string.IsNullOrWhiteSpace(client.Email))
        {
            var existing = await _clients.FindByEmailAsync(client.Email, ct);
            if (existing is not null)
                throw new InvalidOperationException($"A client with email '{client.Email}' already exists.");
        }

        client.CreatedAt = DateTime.UtcNow;
        client.UpdatedAt = DateTime.UtcNow;

        var saved = await _clients.AddAsync(client, ct);
        await _excelSync.WriteClientAsync(saved, ct);
        return saved;
    }

    public async Task UpdateClientAsync(Client client, CancellationToken ct = default)
    {
        client.UpdatedAt = DateTime.UtcNow;
        await _clients.UpdateAsync(client, ct);
        await _excelSync.WriteClientAsync(client, ct);
    }

    public Task DeleteClientAsync(int id, CancellationToken ct = default)
        => _clients.DeleteAsync(id, ct);
}
