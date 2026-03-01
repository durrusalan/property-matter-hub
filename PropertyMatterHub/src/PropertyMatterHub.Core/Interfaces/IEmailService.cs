using PropertyMatterHub.Core.Models;

namespace PropertyMatterHub.Core.Interfaces;

public interface IEmailService
{
    Task<IReadOnlyList<EmailRecord>> FetchNewEmailsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<EmailRecord>> GetEmailsForMatterAsync(int matterId, CancellationToken ct = default);
    Task<IReadOnlyList<EmailRecord>> GetUnclassifiedEmailsAsync(CancellationToken ct = default);
    Task SendEmailAsync(string to, string subject, string body, string? matterId = null,
        IEnumerable<string>? attachmentPaths = null, CancellationToken ct = default);
    Task ClassifyEmailAsync(int emailRecordId, int matterId, CancellationToken ct = default);
    Task<string?> GetFullBodyAsync(string gmailMessageId, CancellationToken ct = default);
    Task<IReadOnlyList<EmailRecord>> GetNeedsReviewEmailsAsync(CancellationToken ct = default);
}
