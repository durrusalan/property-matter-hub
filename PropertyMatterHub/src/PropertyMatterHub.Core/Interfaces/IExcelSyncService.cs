using PropertyMatterHub.Core.Models;

namespace PropertyMatterHub.Core.Interfaces;

public interface IExcelSyncService
{
    Task ImportFromExcelAsync(string filePath, CancellationToken ct = default);
    Task WriteClientAsync(Client client, CancellationToken ct = default);
    Task WriteMatterAsync(Matter matter, CancellationToken ct = default);
    Task<bool> HasExternalChangesAsync(string filePath, CancellationToken ct = default);
    Task MergeExternalChangesAsync(string filePath, CancellationToken ct = default);
}
