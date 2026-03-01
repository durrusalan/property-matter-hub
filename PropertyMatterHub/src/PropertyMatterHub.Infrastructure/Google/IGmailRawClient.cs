using Google.Apis.Gmail.v1.Data;

namespace PropertyMatterHub.Infrastructure.Google;

/// <summary>
/// Thin abstraction over the Google.Apis.Gmail.v1.GmailService, so
/// LiveGmailApiAdapter can be unit-tested without real network access.
/// </summary>
public interface IGmailRawClient
{
    /// <summary>Lists message stubs matching <paramref name="query"/>.</summary>
    Task<ListMessagesResponse> ListAsync(string query, int maxResults, CancellationToken ct);

    /// <summary>Fetches a full message by its Gmail ID.</summary>
    Task<Message> GetAsync(string messageId, CancellationToken ct);

    /// <summary>Sends a raw RFC 2822 email (base-64url encoded).</summary>
    Task SendAsync(string base64UrlRaw, string? threadId, CancellationToken ct);

    /// <summary>Modifies the label set on a single message.</summary>
    Task ModifyLabelsAsync(string messageId, IEnumerable<string> removeLabels, CancellationToken ct);
}
