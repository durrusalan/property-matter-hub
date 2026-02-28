using PropertyMatterHub.Core.Interfaces;
using PropertyMatterHub.Core.Models;

namespace PropertyMatterHub.Core.Services;

public record SearchResults(
    IReadOnlyList<Matter> Matters,
    IReadOnlyList<Client> Clients,
    IReadOnlyList<Document> Documents,
    IReadOnlyList<Template> Templates,
    IReadOnlyList<EmailRecord> Emails
);

public class SearchService
{
    private readonly IMatterRepository _matters;
    private readonly IClientRepository _clients;

    public SearchService(IMatterRepository matters, IClientRepository clients)
    {
        _matters = matters;
        _clients = clients;
    }

    public async Task<SearchResults> SearchAllAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new SearchResults([], [], [], [], []);

        var mattersTask = _matters.SearchAsync(query, ct);
        var clientsTask = _clients.SearchAsync(query, ct);

        await Task.WhenAll(mattersTask, clientsTask);

        return new SearchResults(
            mattersTask.Result,
            clientsTask.Result,
            [],     // Documents and emails searched via EF/FTS in infrastructure
            [],
            []
        );
    }
}
