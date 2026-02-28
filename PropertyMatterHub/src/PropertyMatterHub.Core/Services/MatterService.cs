using PropertyMatterHub.Core.Interfaces;
using PropertyMatterHub.Core.Models;

namespace PropertyMatterHub.Core.Services;

public class MatterService
{
    private readonly IMatterRepository _matters;
    private readonly IClientRepository _clients;
    private readonly IExcelSyncService _excelSync;

    public MatterService(IMatterRepository matters, IClientRepository clients, IExcelSyncService excelSync)
    {
        _matters = matters;
        _clients = clients;
        _excelSync = excelSync;
    }

    public Task<IReadOnlyList<Matter>> GetAllAsync(CancellationToken ct = default)
        => _matters.GetAllAsync(ct);

    public Task<IReadOnlyList<Matter>> GetActiveAsync(CancellationToken ct = default)
        => _matters.GetActiveAsync(ct);

    public Task<Matter?> GetByRefAsync(string matterRef, CancellationToken ct = default)
        => _matters.GetByRefAsync(matterRef, ct);

    public Task<IReadOnlyList<Matter>> SearchAsync(string query, CancellationToken ct = default)
        => _matters.SearchAsync(query, ct);

    public async Task<Matter> CreateMatterAsync(Matter matter, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(matter.MatterRef))
            throw new ArgumentException("Matter reference is required.", nameof(matter));

        if (string.IsNullOrWhiteSpace(matter.Title))
            throw new ArgumentException("Matter title is required.", nameof(matter));

        var existing = await _matters.GetByRefAsync(matter.MatterRef, ct);
        if (existing is not null)
            throw new InvalidOperationException($"A matter with reference '{matter.MatterRef}' already exists.");

        matter.CreatedAt = DateTime.UtcNow;
        matter.UpdatedAt = DateTime.UtcNow;

        var saved = await _matters.AddAsync(matter, ct);
        await _excelSync.WriteMatterAsync(saved, ct);
        return saved;
    }

    public async Task UpdateMatterAsync(Matter matter, CancellationToken ct = default)
    {
        matter.UpdatedAt = DateTime.UtcNow;
        await _matters.UpdateAsync(matter, ct);
        await _excelSync.WriteMatterAsync(matter, ct);
    }

    public Task DeleteMatterAsync(int id, CancellationToken ct = default)
        => _matters.DeleteAsync(id, ct);
}
