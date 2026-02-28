using PropertyMatterHub.Core.Models;

namespace PropertyMatterHub.Core.Interfaces;

public interface IClientRepository
{
    Task<Client?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Client>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Client>> SearchAsync(string query, CancellationToken ct = default);
    Task<Client?> FindByEmailAsync(string email, CancellationToken ct = default);
    Task<Client> AddAsync(Client client, CancellationToken ct = default);
    Task UpdateAsync(Client client, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
