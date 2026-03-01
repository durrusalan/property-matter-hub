using PropertyMatterHub.Core.Interfaces;
using PropertyMatterHub.Core.Models;

namespace PropertyMatterHub.App.Services;

/// <summary>No-op email service used before Google auth is configured.</summary>
public class NullEmailService : IEmailService
{
    public Task<IReadOnlyList<EmailRecord>> FetchNewEmailsAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<EmailRecord>>([]);

    public Task<IReadOnlyList<EmailRecord>> GetEmailsForMatterAsync(int matterId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<EmailRecord>>([]);

    public Task<IReadOnlyList<EmailRecord>> GetUnclassifiedEmailsAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<EmailRecord>>([]);

    public Task SendEmailAsync(string to, string subject, string body, string? matterId = null,
        IEnumerable<string>? attachmentPaths = null, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task ClassifyEmailAsync(int emailRecordId, int matterId, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<string?> GetFullBodyAsync(string gmailMessageId, CancellationToken ct = default)
        => Task.FromResult<string?>(null);

    public Task<IReadOnlyList<EmailRecord>> GetNeedsReviewEmailsAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<EmailRecord>>([]);
}
