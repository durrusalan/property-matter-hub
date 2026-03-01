using Google.Apis.Gmail.v1.Data;
using PropertyMatterHub.Core.Models;
using System.Text;

namespace PropertyMatterHub.Infrastructure.Google;

/// <summary>Pure static mapping — Gmail API Message → EmailRecord.</summary>
public static class GmailMessageMapper
{
    private const int SnippetMaxLength = 500;

    public static EmailRecord ToEmailRecord(Message msg)
    {
        var body = ExtractPlainText(msg.Payload);

        return new EmailRecord
        {
            GmailMessageId       = msg.Id,
            Subject              = Header(msg, "Subject"),
            From                 = Header(msg, "From"),
            To                   = Header(msg, "To"),
            Body                 = body,
            Snippet              = Truncate(body, SnippetMaxLength),
            SentAt               = msg.InternalDate.HasValue
                                       ? DateTimeOffset.FromUnixTimeMilliseconds(msg.InternalDate.Value).UtcDateTime
                                       : DateTime.UtcNow,
            Direction            = EmailDirection.Inbound,
            ClassificationStatus = EmailClassificationStatus.Unclassified,
            FetchedAt            = DateTime.UtcNow
        };
    }

    public static bool IsUnread(Message msg) =>
        msg.LabelIds?.Contains("UNREAD") ?? false;

    // ── Private helpers ───────────────────────────────────────────────────────

    private static string Header(Message msg, string name) =>
        msg.Payload?.Headers?
            .FirstOrDefault(h => h.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            ?.Value ?? string.Empty;

    private static string ExtractPlainText(MessagePart? part)
    {
        if (part is null) return string.Empty;

        if (part.MimeType == "text/plain" && part.Body?.Data is not null)
            return DecodeBase64Url(part.Body.Data);

        if (part.Parts is not null)
            foreach (var child in part.Parts)
            {
                var text = ExtractPlainText(child);
                if (!string.IsNullOrEmpty(text)) return text;
            }

        return string.Empty;
    }

    /// <summary>Decodes a base64url string (Gmail's encoding) to UTF-8 text.</summary>
    private static string DecodeBase64Url(string base64Url)
    {
        var base64 = base64Url.Replace('-', '+').Replace('_', '/');
        var padding = (4 - base64.Length % 4) % 4;
        base64 += new string('=', padding);
        return Encoding.UTF8.GetString(Convert.FromBase64String(base64));
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length > maxLength ? value[..maxLength] : value;
}
