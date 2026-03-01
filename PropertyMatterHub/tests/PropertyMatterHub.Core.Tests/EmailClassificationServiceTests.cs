using FluentAssertions;
using PropertyMatterHub.Core.Models;
using PropertyMatterHub.Core.Services;

namespace PropertyMatterHub.Core.Tests;

public class EmailClassificationServiceTests
{
    private static readonly List<Matter> Matters =
    [
        new() { Id = 1, MatterRef = "PROP-2026-0042", Title = "Purchase – 12 Oak Lane", ClientId = 1, Status = MatterStatus.Active, UpdatedAt = DateTime.UtcNow },
        new() { Id = 2, MatterRef = "PROP-2026-0018", Title = "Sale – 4 Harbour View",  ClientId = 2, Status = MatterStatus.Active, UpdatedAt = DateTime.UtcNow },
        new() { Id = 3, MatterRef = "PROP-2025-0087", Title = "Mortgage – Refinance",   ClientId = 1, Status = MatterStatus.Closed, UpdatedAt = DateTime.UtcNow }
    ];

    private static readonly List<Client> Clients =
    [
        new() { Id = 1, Name = "Siobhán Murphy", Email = "siobhan.murphy@email.ie" },
        new() { Id = 2, Name = "Patrick Doyle",  Email = "patrick.doyle@email.ie"  }
    ];

    private readonly EmailClassificationService _sut = new();

    // ── MatterRefRule ─────────────────────────────────────────────────────────

    [Fact]
    public void Classify_WhenSubjectContainsMatterRef_ReturnsHighConfidence()
    {
        var email = new EmailRecord { Subject = "RE: PROP-2026-0042 – contract query", From = "unknown@example.com" };

        var result = _sut.Classify(email, Matters, Clients);

        result.MatterId.Should().Be(1);
        result.Confidence.Should().BeGreaterThanOrEqualTo(0.9f);
    }

    [Fact]
    public void Classify_WhenBodyContainsMatterRef_ReturnsHighConfidence()
    {
        var email = new EmailRecord
        {
            Subject  = "General query",
            Body     = "Please find attached the search results for PROP-2026-0018.",
            From     = "unknown@example.com"
        };

        var result = _sut.Classify(email, Matters, Clients);

        result.MatterId.Should().Be(2);
        result.Confidence.Should().BeGreaterThanOrEqualTo(0.9f);
    }

    [Fact]
    public void Classify_WhenMatterRefNotInDatabase_ReturnsNoMatch()
    {
        var email = new EmailRecord { Subject = "PROP-2099-9999 something", From = "a@b.com" };

        var result = _sut.Classify(email, Matters, Clients);

        result.MatterId.Should().BeNull();
    }

    // ── ClientEmailRule ───────────────────────────────────────────────────────

    [Fact]
    public void Classify_WhenSenderMatchesClientEmail_ReturnsMediumConfidence()
    {
        var email = new EmailRecord
        {
            Subject = "Question about my purchase",
            From    = "siobhan.murphy@email.ie"
        };

        var result = _sut.Classify(email, Matters, Clients);

        result.MatterId.Should().Be(1);   // active matter for Siobhán
        result.Confidence.Should().BeInRange(0.5f, 0.89f);
    }

    [Fact]
    public void Classify_WhenSenderHasNoActiveMatter_ReturnsLowConfidenceNoMatter()
    {
        // Siobhán's only active matter is 1; mark it closed for this test
        var matters = Matters.Select(m => m.ClientId == 1
            ? new Matter { Id = m.Id, MatterRef = m.MatterRef, ClientId = m.ClientId, Status = MatterStatus.Closed, UpdatedAt = m.UpdatedAt }
            : m).ToList();

        var email = new EmailRecord { Subject = "Hi", From = "siobhan.murphy@email.ie" };
        var result = _sut.Classify(email, matters, Clients);

        result.MatterId.Should().BeNull();
    }

    [Fact]
    public void Classify_WhenNoRuleMatches_ReturnsZeroConfidence()
    {
        var email = new EmailRecord { Subject = "Newsletter", From = "noreply@spam.com" };

        var result = _sut.Classify(email, Matters, Clients);

        result.Confidence.Should().Be(0f);
        result.MatterId.Should().BeNull();
    }

    // ── MatterRefRule wins over ClientEmailRule ────────────────────────────────

    [Fact]
    public void Classify_MatterRefBeatsClientEmailWhenBothMatch()
    {
        // Siobhán sends email mentioning Patrick's matter
        var email = new EmailRecord
        {
            Subject = "PROP-2026-0018 forwarded query",
            From    = "siobhan.murphy@email.ie"
        };

        var result = _sut.Classify(email, Matters, Clients);

        result.MatterId.Should().Be(2, "matter ref always wins");
        result.Confidence.Should().BeGreaterThanOrEqualTo(0.9f);
    }

    // ── DetermineStatus ───────────────────────────────────────────────────────

    [Theory]
    [InlineData(0.95f, EmailClassificationStatus.AutoClassified)]
    [InlineData(0.80f, EmailClassificationStatus.AutoClassified)]
    [InlineData(0.75f, EmailClassificationStatus.NeedsReview)]
    [InlineData(0.50f, EmailClassificationStatus.NeedsReview)]
    [InlineData(0.30f, EmailClassificationStatus.Unclassified)]
    [InlineData(0.00f, EmailClassificationStatus.Unclassified)]
    public void DetermineStatus_ReturnsCorrectStatus(float confidence, EmailClassificationStatus expected)
    {
        _sut.DetermineStatus(confidence).Should().Be(expected);
    }
}
