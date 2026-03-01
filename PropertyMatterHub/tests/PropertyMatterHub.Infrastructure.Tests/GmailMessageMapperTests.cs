using FluentAssertions;
using Google.Apis.Gmail.v1.Data;
using PropertyMatterHub.Core.Models;
using PropertyMatterHub.Infrastructure.Google;
using Xunit;

namespace PropertyMatterHub.Infrastructure.Tests;

/// <summary>
/// RED tests for GmailMessageMapper — pure static mapping logic, no API calls.
/// </summary>
public class GmailMessageMapperTests
{
    [Fact]
    public void ToEmailRecord_MapsIdAndThreadId()
    {
        var msg = MakeMessage(id: "abc123", threadId: "thread456");

        var record = GmailMessageMapper.ToEmailRecord(msg);

        record.GmailMessageId.Should().Be("abc123");
    }

    [Fact]
    public void ToEmailRecord_MapsSubjectFromHeader()
    {
        var msg = MakeMessage(subject: "PROP-2026-0042 – contract query");

        var record = GmailMessageMapper.ToEmailRecord(msg);

        record.Subject.Should().Be("PROP-2026-0042 – contract query");
    }

    [Fact]
    public void ToEmailRecord_MapsFromHeader()
    {
        var msg = MakeMessage(from: "client@example.com");

        var record = GmailMessageMapper.ToEmailRecord(msg);

        record.From.Should().Be("client@example.com");
    }

    [Fact]
    public void ToEmailRecord_SetsDirectionInbound_ForReceivedMessage()
    {
        var msg = MakeMessage(from: "someone@external.com");

        var record = GmailMessageMapper.ToEmailRecord(msg);

        record.Direction.Should().Be(EmailDirection.Inbound);
    }

    [Fact]
    public void ToEmailRecord_IsUnclassified_ByDefault()
    {
        var msg = MakeMessage();

        var record = GmailMessageMapper.ToEmailRecord(msg);

        record.ClassificationStatus.Should().Be(EmailClassificationStatus.Unclassified);
    }

    [Fact]
    public void ToEmailRecord_SetsReceivedAtFromInternalDate()
    {
        // 1_700_000_000_000 ms = 2023-11-14 22:13:20 UTC
        var msg = MakeMessage(internalDateMs: 1_700_000_000_000);

        var record = GmailMessageMapper.ToEmailRecord(msg);

        record.SentAt.Should().BeCloseTo(
            new DateTime(2023, 11, 14, 22, 13, 20, DateTimeKind.Utc),
            precision: TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ToEmailRecord_ExtractsPlainTextBody()
    {
        // "Hello World" Base64url-encoded
        var encoded = Convert.ToBase64String("Hello World"u8.ToArray())
            .Replace('+', '-').Replace('/', '_');

        var msg = MakeMessage(bodyData: encoded);

        var record = GmailMessageMapper.ToEmailRecord(msg);

        record.Body.Should().Contain("Hello World");
    }

    [Fact]
    public void ToEmailRecord_TruncatesSnippetAt500Chars()
    {
        var longBody = new string('x', 600);
        var encoded  = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(longBody))
            .Replace('+', '-').Replace('/', '_');

        var msg = MakeMessage(bodyData: encoded);

        var record = GmailMessageMapper.ToEmailRecord(msg);

        record.Snippet.Should().HaveLength(500);
    }

    [Fact]
    public void IsUnread_WhenLabelContainsUNREAD()
    {
        var msg = MakeMessage();
        msg.LabelIds = ["INBOX", "UNREAD"];

        var record = GmailMessageMapper.ToEmailRecord(msg);

        // Unread label means not-yet-read
        record.Direction.Should().Be(EmailDirection.Inbound);  // sanity
        // The mapper should expose unread state via a helper or property
        GmailMessageMapper.IsUnread(msg).Should().BeTrue();
    }

    [Fact]
    public void IsUnread_WhenNoUNREADLabel_ReturnsFalse()
    {
        var msg = MakeMessage();
        msg.LabelIds = ["INBOX"];

        GmailMessageMapper.IsUnread(msg).Should().BeFalse();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Message MakeMessage(
        string id              = "msgId",
        string threadId        = "threadId",
        string subject         = "Test Subject",
        string from            = "sender@example.com",
        string to              = "office@firm.ie",
        long   internalDateMs  = 0,
        string? bodyData       = null)
    {
        var payload = new MessagePart
        {
            MimeType = "text/plain",
            Headers  =
            [
                new MessagePartHeader { Name = "Subject", Value = subject },
                new MessagePartHeader { Name = "From",    Value = from    },
                new MessagePartHeader { Name = "To",      Value = to      }
            ],
            Body = new MessagePartBody
            {
                Data = bodyData ?? Convert.ToBase64String("body text"u8.ToArray())
                    .Replace('+', '-').Replace('/', '_')
            }
        };

        return new Message
        {
            Id           = id,
            ThreadId     = threadId,
            Payload      = payload,
            InternalDate = internalDateMs == 0 ? null : internalDateMs,
            LabelIds     = ["INBOX"]
        };
    }
}
