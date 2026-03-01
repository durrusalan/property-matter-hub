using FluentAssertions;
using Google.Apis.Gmail.v1.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PropertyMatterHub.Core.Models;
using PropertyMatterHub.Infrastructure.Data;
using PropertyMatterHub.Infrastructure.Google;
using Xunit;

namespace PropertyMatterHub.Infrastructure.Tests;

/// <summary>
/// RED tests for GmailEmailService.
/// All Gmail API calls go through IGmailApiAdapter (mocked).
/// All persistence goes through AppDbContext (in-memory SQLite).
/// </summary>
public class GmailEmailServiceTests : IAsyncLifetime
{
    private AppDbContext _db = null!;
    private IGmailApiAdapter _adapter = null!;
    private GmailEmailService _sut = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        _db = new AppDbContext(options);
        await _db.Database.OpenConnectionAsync();
        await _db.Database.EnsureCreatedAsync();

        _adapter = Substitute.For<IGmailApiAdapter>();
        _sut = new GmailEmailService(_adapter, _db, NullLogger<GmailEmailService>.Instance);
    }

    public async Task DisposeAsync() => await _db.DisposeAsync();

    // ── FetchNewEmailsAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task FetchNewEmailsAsync_StoresNewEmailsInDatabase()
    {
        _adapter.FetchUnreadAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([MakeMessage("msg-001", "Contract query")]);

        await _sut.FetchNewEmailsAsync();

        var stored = await _db.EmailRecords.ToListAsync();
        stored.Should().HaveCount(1);
        stored[0].GmailMessageId.Should().Be("msg-001");
        stored[0].Subject.Should().Be("Contract query");
    }

    [Fact]
    public async Task FetchNewEmailsAsync_SkipsEmailsAlreadyInDatabase()
    {
        _db.EmailRecords.Add(new EmailRecord
        {
            GmailMessageId = "msg-001", Subject = "existing",
            SentAt = DateTime.UtcNow, FetchedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        _adapter.FetchUnreadAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([MakeMessage("msg-001", "Contract query")]);

        await _sut.FetchNewEmailsAsync();

        var stored = await _db.EmailRecords.ToListAsync();
        stored.Should().HaveCount(1, "duplicate should not be inserted");
    }

    [Fact]
    public async Task FetchNewEmailsAsync_ReturnsOnlyNewlyFetchedRecords()
    {
        _adapter.FetchUnreadAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([
                MakeMessage("msg-A", "First"),
                MakeMessage("msg-B", "Second")
            ]);

        var result = await _sut.FetchNewEmailsAsync();

        result.Should().HaveCount(2);
    }

    // ── GetEmailsForMatterAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetEmailsForMatterAsync_ReturnsEmailsLinkedToMatter()
    {
        await SeedMatters(1, 2);
        _db.EmailRecords.AddRange(
            EmailForMatter(matterId: 1, gmailId: "e1"),
            EmailForMatter(matterId: 1, gmailId: "e2"),
            EmailForMatter(matterId: 2, gmailId: "e3"));
        await _db.SaveChangesAsync();

        var result = await _sut.GetEmailsForMatterAsync(matterId: 1);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(e => e.MatterId.Should().Be(1));
    }

    [Fact]
    public async Task GetEmailsForMatterAsync_ReturnsEmpty_WhenNoEmailsForMatter()
    {
        var result = await _sut.GetEmailsForMatterAsync(matterId: 99);

        result.Should().BeEmpty();
    }

    // ── GetUnclassifiedEmailsAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetUnclassifiedEmailsAsync_ReturnsOnlyUnclassifiedEmails()
    {
        _db.EmailRecords.AddRange(
            EmailWithStatus("u1", EmailClassificationStatus.Unclassified),
            EmailWithStatus("u2", EmailClassificationStatus.AutoClassified),
            EmailWithStatus("u3", EmailClassificationStatus.Unclassified));
        await _db.SaveChangesAsync();

        var result = await _sut.GetUnclassifiedEmailsAsync();

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(e =>
            e.ClassificationStatus.Should().Be(EmailClassificationStatus.Unclassified));
    }

    // ── ClassifyEmailAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task ClassifyEmailAsync_UpdatesMatterIdAndStatus()
    {
        await SeedMatters(7);
        var record = EmailWithStatus("cls-1", EmailClassificationStatus.Unclassified);
        _db.EmailRecords.Add(record);
        await _db.SaveChangesAsync();

        await _sut.ClassifyEmailAsync(record.Id, matterId: 7);

        var updated = await _db.EmailRecords.FindAsync(record.Id);
        updated!.MatterId.Should().Be(7);
        updated.ClassificationStatus.Should().Be(EmailClassificationStatus.ManuallyClassified);
    }

    [Fact]
    public async Task ClassifyEmailAsync_Throws_WhenEmailNotFound()
    {
        await _sut.Invoking(s => s.ClassifyEmailAsync(emailRecordId: 9999, matterId: 1))
            .Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*9999*");
    }

    // ── GetFullBodyAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetFullBodyAsync_ReturnsBody_WhenEmailIsInDatabase()
    {
        _db.EmailRecords.Add(new EmailRecord
        {
            GmailMessageId = "body-msg",
            Body = "Full email body text",
            Subject = "test", SentAt = DateTime.UtcNow, FetchedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var body = await _sut.GetFullBodyAsync("body-msg");

        body.Should().Be("Full email body text");
    }

    [Fact]
    public async Task GetFullBodyAsync_ReturnsNull_WhenEmailNotInDatabase()
    {
        var body = await _sut.GetFullBodyAsync("unknown-id");

        body.Should().BeNull();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Message MakeMessage(string id, string subject) => new()
    {
        Id       = id,
        ThreadId = "thread-" + id,
        Payload  = new MessagePart
        {
            MimeType = "text/plain",
            Headers  = [
                new MessagePartHeader { Name = "Subject", Value = subject },
                new MessagePartHeader { Name = "From",    Value = "client@example.com" },
                new MessagePartHeader { Name = "To",      Value = "office@firm.ie" }
            ],
            Body = new MessagePartBody
            {
                Data = Convert.ToBase64String("body text"u8.ToArray())
                    .Replace('+', '-').Replace('/', '_')
            }
        },
        LabelIds     = ["INBOX", "UNREAD"],
        InternalDate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
    };

    private static EmailRecord EmailForMatter(int matterId, string gmailId) => new()
    {
        GmailMessageId = gmailId, MatterId = matterId,
        Subject = "test", SentAt = DateTime.UtcNow, FetchedAt = DateTime.UtcNow,
        ClassificationStatus = EmailClassificationStatus.ManuallyClassified
    };

    private static EmailRecord EmailWithStatus(string gmailId, EmailClassificationStatus status) => new()
    {
        GmailMessageId = gmailId, ClassificationStatus = status,
        Subject = "test", SentAt = DateTime.UtcNow, FetchedAt = DateTime.UtcNow
    };

    private async Task SeedMatters(params int[] ids)
    {
        foreach (var id in ids)
        {
            if (await _db.Matters.FindAsync(id) is not null) continue;
            var client = new Client { Id = id, Name = $"Client {id}", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            _db.Clients.Add(client);
            await _db.SaveChangesAsync();
            _db.Matters.Add(new Matter
            {
                Id = id, ClientId = client.Id, MatterRef = $"PROP-TEST-{id:0000}",
                Title = $"Matter {id}", Status = MatterStatus.Active,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }
    }
}
