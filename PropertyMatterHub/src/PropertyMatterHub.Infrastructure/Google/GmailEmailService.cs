using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PropertyMatterHub.Core.Interfaces;
using PropertyMatterHub.Core.Models;
using PropertyMatterHub.Infrastructure.Data;

namespace PropertyMatterHub.Infrastructure.Google;

/// <summary>
/// Live implementation of IEmailService backed by Gmail API + SQLite.
/// Gmail API calls are delegated to IGmailApiAdapter for testability.
/// </summary>
public class GmailEmailService : IEmailService
{
    private readonly IGmailApiAdapter _adapter;
    private readonly AppDbContext _db;
    private readonly ILogger<GmailEmailService> _logger;

    public GmailEmailService(
        IGmailApiAdapter adapter,
        AppDbContext db,
        ILogger<GmailEmailService> logger)
    {
        _adapter = adapter;
        _db      = db;
        _logger  = logger;
    }

    public async Task<IReadOnlyList<EmailRecord>> FetchNewEmailsAsync(CancellationToken ct = default)
    {
        var messages = await _adapter.FetchUnreadAsync(maxResults: 50, ct);

        var existingIds = (await _db.EmailRecords
            .Select(e => e.GmailMessageId)
            .ToListAsync(ct))
            .ToHashSet();

        var newRecords = messages
            .Where(m => !existingIds.Contains(m.Id))
            .Select(GmailMessageMapper.ToEmailRecord)
            .ToList();

        if (newRecords.Count > 0)
        {
            _db.EmailRecords.AddRange(newRecords);
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Stored {Count} new email(s).", newRecords.Count);
        }

        return newRecords;
    }

    public async Task<IReadOnlyList<EmailRecord>> GetEmailsForMatterAsync(int matterId, CancellationToken ct = default) =>
        await _db.EmailRecords
            .Where(e => e.MatterId == matterId)
            .OrderByDescending(e => e.SentAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<EmailRecord>> GetUnclassifiedEmailsAsync(CancellationToken ct = default) =>
        await _db.EmailRecords
            .Where(e => e.ClassificationStatus == EmailClassificationStatus.Unclassified)
            .OrderByDescending(e => e.FetchedAt)
            .ToListAsync(ct);

    public async Task ClassifyEmailAsync(int emailRecordId, int matterId, CancellationToken ct = default)
    {
        var record = await _db.EmailRecords.FindAsync([emailRecordId], ct)
            ?? throw new KeyNotFoundException($"EmailRecord {emailRecordId} not found.");

        record.MatterId             = matterId;
        record.ClassificationStatus = EmailClassificationStatus.ManuallyClassified;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<string?> GetFullBodyAsync(string gmailMessageId, CancellationToken ct = default)
    {
        var record = await _db.EmailRecords
            .FirstOrDefaultAsync(e => e.GmailMessageId == gmailMessageId, ct);

        return record?.Body;
    }

    public async Task SendEmailAsync(
        string to, string subject, string body,
        string? matterId = null,
        IEnumerable<string>? attachmentPaths = null,
        CancellationToken ct = default)
    {
        var raw = GmailMessageMapper.BuildBase64UrlRaw(to, subject, body);
        await _adapter.SendRawAsync(raw, threadId: null, ct);

        _db.EmailRecords.Add(new EmailRecord
        {
            // Prefix clearly marks this as a local placeholder; never a real Gmail ID.
            GmailMessageId       = GmailMessageMapper.OutboundIdPrefix + Guid.NewGuid(),
            Subject              = subject,
            To                   = to,
            Body                 = body,
            Snippet              = GmailMessageMapper.Truncate(body, GmailMessageMapper.SnippetMaxLength),
            Direction            = EmailDirection.Outbound,
            SentAt               = DateTime.UtcNow,
            FetchedAt            = DateTime.UtcNow,
            ClassificationStatus = EmailClassificationStatus.ManuallyClassified,
            MatterId             = int.TryParse(matterId, out var mid) ? mid : null
        });
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Sent email to {To} (subject: {Subject})", to, subject);
    }
}
