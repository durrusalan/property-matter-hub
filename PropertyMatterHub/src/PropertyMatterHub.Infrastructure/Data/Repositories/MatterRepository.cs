using Microsoft.EntityFrameworkCore;
using PropertyMatterHub.Core.Interfaces;
using PropertyMatterHub.Core.Models;

namespace PropertyMatterHub.Infrastructure.Data.Repositories;

public class MatterRepository : IMatterRepository
{
    private readonly AppDbContext _db;

    public MatterRepository(AppDbContext db) => _db = db;

    public async Task<Matter?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _db.Matters.Include(m => m.Client).FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task<Matter?> GetByRefAsync(string matterRef, CancellationToken ct = default)
        => await _db.Matters.Include(m => m.Client)
                             .FirstOrDefaultAsync(m => m.MatterRef == matterRef, ct);

    public async Task<IReadOnlyList<Matter>> GetAllAsync(CancellationToken ct = default)
        => await _db.Matters.Include(m => m.Client)
                             .OrderByDescending(m => m.UpdatedAt)
                             .ToListAsync(ct);

    public async Task<IReadOnlyList<Matter>> GetActiveAsync(CancellationToken ct = default)
        => await _db.Matters.Include(m => m.Client)
                             .Where(m => m.Status == MatterStatus.Active)
                             .OrderByDescending(m => m.UpdatedAt)
                             .ToListAsync(ct);

    public async Task<IReadOnlyList<Matter>> SearchAsync(string query, CancellationToken ct = default)
    {
        var q = query.ToLower();
        return await _db.Matters
            .Include(m => m.Client)
            .Where(m => m.MatterRef.ToLower().Contains(q)
                     || m.Title.ToLower().Contains(q)
                     || m.PracticeArea.ToLower().Contains(q)
                     || (m.Client != null && m.Client.Name.ToLower().Contains(q)))
            .OrderByDescending(m => m.UpdatedAt)
            .ToListAsync(ct);
    }

    public async Task<Matter> AddAsync(Matter matter, CancellationToken ct = default)
    {
        _db.Matters.Add(matter);
        await _db.SaveChangesAsync(ct);
        return matter;
    }

    public async Task UpdateAsync(Matter matter, CancellationToken ct = default)
    {
        _db.Matters.Update(matter);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var matter = await _db.Matters.FindAsync(new object[] { id }, ct);
        if (matter is null) return;
        _db.Matters.Remove(matter);
        await _db.SaveChangesAsync(ct);
    }
}
