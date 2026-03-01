using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PropertyMatterHub.Core.Models;
using PropertyMatterHub.Infrastructure.Data;

namespace PropertyMatterHub.Infrastructure.FileSystem;

/// <summary>
/// Scans the Z: drive folder structure and upserts Clients + Matters
/// into the SQLite database, preserving any data already there.
/// </summary>
public class ZDriveIndexingService
{
    private readonly ZDriveScanner _scanner;
    private readonly AppDbContext _db;
    private readonly ILogger<ZDriveIndexingService> _logger;

    public ZDriveIndexingService(
        ZDriveScanner scanner,
        AppDbContext db,
        ILogger<ZDriveIndexingService> logger)
    {
        _scanner = scanner;
        _db      = db;
        _logger  = logger;
    }

    /// <summary>
    /// Scan the configured Z: drive root and upsert any new Clients/Matters.
    /// Existing records are never overwritten — only missing ones are created.
    /// Returns a summary of what was found and created.
    /// </summary>
    public async Task<IndexingSummary> RunAsync(CancellationToken ct = default)
    {
        var summary = new IndexingSummary();

        var entries = _scanner.ScanFolders();
        summary.FoldersFound  = entries.Count;
        summary.FoldersMatched = entries.Count(e => e.IsMatched);

        _logger.LogInformation(
            "Z: drive scan found {Total} folders, {Matched} matched the pattern.",
            summary.FoldersFound, summary.FoldersMatched);

        foreach (var entry in entries.Where(e => e.IsMatched))
        {
            ct.ThrowIfCancellationRequested();

            // ── Upsert Client ─────────────────────────────────────────────
            var client = await _db.Clients
                .FirstOrDefaultAsync(c => c.Name == entry.ClientName!, ct);

            if (client is null)
            {
                client = new Client
                {
                    Name      = entry.ClientName!,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _db.Clients.Add(client);
                await _db.SaveChangesAsync(ct);  // need Id before creating Matter
                summary.ClientsCreated++;
                _logger.LogInformation("Created client: {Name}", client.Name);
            }

            // ── Upsert Matter ─────────────────────────────────────────────
            var matter = await _db.Matters
                .FirstOrDefaultAsync(m => m.MatterRef == entry.CaseNumber!, ct);

            if (matter is null)
            {
                matter = new Matter
                {
                    MatterRef    = entry.CaseNumber!,
                    Title        = BuildTitle(entry.ClientName!, entry.CaseNumber!),
                    PracticeArea = "Conveyancing",   // default — editable later
                    Status       = MatterStatus.Active,
                    ClientId     = client.Id,
                    FolderPath   = entry.FolderPath,
                    CreatedAt    = DateTime.UtcNow,
                    UpdatedAt    = DateTime.UtcNow
                };
                _db.Matters.Add(matter);
                summary.MattersCreated++;
                _logger.LogInformation("Created matter: {Ref} for {Client}", matter.MatterRef, client.Name);
            }
            else if (matter.FolderPath != entry.FolderPath)
            {
                // Keep folder path in sync if the folder was moved
                matter.FolderPath = entry.FolderPath;
                matter.UpdatedAt  = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Indexing complete. Created {C} client(s) and {M} matter(s).",
            summary.ClientsCreated, summary.MattersCreated);

        return summary;
    }

    private static string BuildTitle(string clientName, string caseNumber)
    {
        // "Murphy Siobhan" → "Siobhan Murphy" for the matter title
        var parts = clientName.Split(' ', 2);
        var displayName = parts.Length == 2 ? $"{parts[1]} {parts[0]}" : clientName;
        return $"{displayName} – {caseNumber}";
    }
}

public record IndexingSummary
{
    public int FoldersFound    { get; set; }
    public int FoldersMatched  { get; set; }
    public int ClientsCreated  { get; set; }
    public int MattersCreated  { get; set; }
}
