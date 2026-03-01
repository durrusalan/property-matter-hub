using Google.Apis.Gmail.v1.Data;

namespace PropertyMatterHub.Infrastructure.Google;

/// <summary>
/// Abstracts raw Gmail API calls so GmailEmailService can be unit-tested
/// without real network access.
/// </summary>
public interface IGmailApiAdapter
{
    Task<IReadOnlyList<Message>> FetchUnreadAsync(int maxResults, CancellationToken ct);
    Task<Message> GetMessageAsync(string messageId, CancellationToken ct);
    Task SendRawAsync(string base64UrlRaw, string? threadId, CancellationToken ct);
    Task MarkReadAsync(string messageId, CancellationToken ct);
}
