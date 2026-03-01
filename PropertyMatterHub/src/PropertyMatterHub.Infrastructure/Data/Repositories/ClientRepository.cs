using Microsoft.EntityFrameworkCore;
using PropertyMatterHub.Core.Interfaces;
using PropertyMatterHub.Core.Models;

namespace PropertyMatterHub.Infrastructure.Data.Repositories;

public class ClientRepository : IClientRepository
{
    private readonly AppDbContext _db;

    public ClientRepository(AppDbContext db) => _db = db;

    public async Task<Client?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _db.Clients.Include(c => c.Matters).FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<Client>> GetAllAsync(CancellationToken ct = default)
        => await _db.Clients.OrderBy(c => c.Name).ToListAsync(ct);

    public async Task<IReadOnlyList<Client>> SearchAsync(string query, CancellationToken ct = default)
    {
        var q = query.ToLower();
        return await _db.Clients
            .Where(c => c.Name.ToLower().Contains(q)
                     || (c.Email    != null && c.Email.ToLower().Contains(q))
                     || (c.Address  != null && c.Address.ToLower().Contains(q)))
            .OrderBy(c => c.Name)
            .ToListAsync(ct);
    }

    public async Task<Client?> FindByEmailAsync(string email, CancellationToken ct = default)
        => await _db.Clients
                    .FirstOrDefaultAsync(c => c.Email != null &&
                                              c.Email.ToLower() == email.ToLower(), ct);

    public async Task<Client> AddAsync(Client client, CancellationToken ct = default)
    {
        _db.Clients.Add(client);
        await _db.SaveChangesAsync(ct);
        return client;
    }

    public async Task UpdateAsync(Client client, CancellationToken ct = default)
    {
        _db.Clients.Update(client);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var client = await _db.Clients.FindAsync(new object[] { id }, ct);
        if (client is null) return;
        _db.Clients.Remove(client);
        await _db.SaveChangesAsync(ct);
    }
}
