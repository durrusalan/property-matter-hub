using PropertyMatterHub.Core.Models;

namespace PropertyMatterHub.Core.Interfaces;

public interface IDocumentIndexer
{
    Task IndexFolderAsync(string rootPath, CancellationToken ct = default);
    Task<IReadOnlyList<Document>> GetDocumentsForMatterAsync(int matterId, CancellationToken ct = default);
    Task<string?> ExtractTextAsync(string filePath, CancellationToken ct = default);
}
