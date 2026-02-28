using PropertyMatterHub.Core.Models;

namespace PropertyMatterHub.Core.Interfaces;

public interface IMatterRepository
{
    Task<Matter?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Matter?> GetByRefAsync(string matterRef, CancellationToken ct = default);
    Task<IReadOnlyList<Matter>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Matter>> GetActiveAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Matter>> SearchAsync(string query, CancellationToken ct = default);
    Task<Matter> AddAsync(Matter matter, CancellationToken ct = default);
    Task UpdateAsync(Matter matter, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
