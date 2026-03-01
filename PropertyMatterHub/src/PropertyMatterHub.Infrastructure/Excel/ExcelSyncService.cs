using ClosedXML.Excel;
using Microsoft.Extensions.Logging;
using PropertyMatterHub.Core.Interfaces;
using PropertyMatterHub.Core.Models;
using PropertyMatterHub.Infrastructure.Data;
using System.Security.Cryptography;
using System.Text.Json;

namespace PropertyMatterHub.Infrastructure.Excel;

public class ExcelSyncService : IExcelSyncService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ExcelSyncService> _logger;

    // Named mutex prevents two app instances writing simultaneously
    private const string MutexName = "Global\\PropertyMatterHubExcelSync";

    public ExcelSyncService(AppDbContext db, ILogger<ExcelSyncService> logger)
    {
        _db     = db;
        _logger = logger;
    }

    // ── Import ────────────────────────────────────────────────────────────────

    public async Task ImportFromExcelAsync(string filePath, CancellationToken ct = default)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Excel file not found.", filePath);

        using var mutex = new Mutex(false, MutexName);
        if (!mutex.WaitOne(TimeSpan.FromSeconds(10)))
            throw new TimeoutException("Could not acquire Excel sync mutex within 10 seconds.");

        try
        {
            using var wb = new XLWorkbook(filePath);
            var ws = wb.Worksheets.First();

            // Read header row to discover column mapping
            var headers = ws.Row(1).CellsUsed()
                            .ToDictionary(c => c.Value.ToString().Trim().ToLower(), c => c.Address.ColumnNumber);

            foreach (var row in ws.RowsUsed().Skip(1))
            {
                ct.ThrowIfCancellationRequested();

                var name    = GetCell(row, headers, "name", "client name", "client");
                var email   = GetCell(row, headers, "email");
                var phone   = GetCell(row, headers, "phone", "telephone");
                var address = GetCell(row, headers, "address");

                if (string.IsNullOrWhiteSpace(name)) continue;

                var existing = await _db.Clients.FindAsync(new object[] { }, ct)
                               ?? _db.Clients.Local.FirstOrDefault(c =>
                                   c.Email != null && c.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

                if (existing is null)
                {
                    _db.Clients.Add(new Client
                    {
                        Name    = name,
                        Email   = email,
                        Phone   = phone,
                        Address = address
                    });
                }
            }

            await _db.SaveChangesAsync(ct);
            await RecordSyncAsync(filePath, ComputeFileHash(filePath), ct);
        }
        finally
        {
            mutex.ReleaseMutex();
        }
    }

    // ── Write Client ──────────────────────────────────────────────────────────

    public async Task WriteClientAsync(Client client, CancellationToken ct = default)
    {
        // If no Excel path configured, no-op
        var syncLog = _db.SyncLogs.FirstOrDefault(s => s.ResourceType == "Excel");
        if (syncLog is null) return;

        var filePath = syncLog.ResourceKey;
        if (!File.Exists(filePath)) return;

        await WithMutexAsync(async () =>
        {
            using var wb = new XLWorkbook(filePath);
            var ws = wb.Worksheets.First();

            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
            var newRow  = ws.Row(lastRow + 1);

            // Simple append: Name | Email | Phone | Address
            newRow.Cell(1).Value = client.Name;
            newRow.Cell(2).Value = client.Email ?? string.Empty;
            newRow.Cell(3).Value = client.Phone ?? string.Empty;
            newRow.Cell(4).Value = client.Address ?? string.Empty;

            wb.Save();
            await RecordSyncAsync(filePath, ComputeFileHash(filePath), ct);
        });
    }

    public Task WriteMatterAsync(Matter matter, CancellationToken ct = default)
    {
        // Matters are not directly in the Excel file (clients-only model)
        // This is a no-op unless a future column mapping is added
        return Task.CompletedTask;
    }

    // ── Change Detection ──────────────────────────────────────────────────────

    public Task<bool> HasExternalChangesAsync(string filePath, CancellationToken ct = default)
    {
        var syncLog = _db.SyncLogs.FirstOrDefault(s =>
            s.ResourceType == "Excel" && s.ResourceKey == filePath);

        if (syncLog is null) return Task.FromResult(true);    // never synced → treat as changed

        var currentHash = ComputeFileHash(filePath);
        var storedHash  = syncLog.Metadata;

        return Task.FromResult(currentHash != storedHash);
    }

    public async Task MergeExternalChangesAsync(string filePath, CancellationToken ct = default)
    {
        _logger.LogInformation("Merging external Excel changes from {Path}", filePath);
        await ImportFromExcelAsync(filePath, ct);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string? GetCell(IXLRow row, Dictionary<string, int> headers, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (headers.TryGetValue(key, out var col))
            {
                var val = row.Cell(col).GetValue<string>().Trim();
                if (!string.IsNullOrWhiteSpace(val)) return val;
            }
        }
        return null;
    }

    private static string ComputeFileHash(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var hash = SHA256.HashData(stream);
        return Convert.ToHexString(hash);
    }

    private async Task RecordSyncAsync(string filePath, string hash, CancellationToken ct)
    {
        var log = _db.SyncLogs.FirstOrDefault(s => s.ResourceType == "Excel" && s.ResourceKey == filePath);
        if (log is null)
        {
            _db.SyncLogs.Add(new SyncLog
            {
                ResourceType  = "Excel",
                ResourceKey   = filePath,
                LastSyncedAt  = DateTime.UtcNow,
                Metadata      = hash
            });
        }
        else
        {
            log.LastSyncedAt = DateTime.UtcNow;
            log.Metadata     = hash;
        }
        await _db.SaveChangesAsync(ct);
    }

    private static async Task WithMutexAsync(Func<Task> action)
    {
        using var mutex = new Mutex(false, MutexName);
        if (!mutex.WaitOne(TimeSpan.FromSeconds(10)))
            throw new TimeoutException("Could not acquire Excel sync mutex.");
        try   { await action(); }
        finally { mutex.ReleaseMutex(); }
    }
}
