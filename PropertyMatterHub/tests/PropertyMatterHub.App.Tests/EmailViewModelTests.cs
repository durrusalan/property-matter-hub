using FluentAssertions;
using NSubstitute;
using PropertyMatterHub.App.ViewModels;
using PropertyMatterHub.Core.Interfaces;
using PropertyMatterHub.Core.Models;
using PropertyMatterHub.Core.Services;
using Xunit;

namespace PropertyMatterHub.App.Tests;

/// <summary>
/// RED tests for EmailViewModel classification wiring.
/// IEmailService and repository calls are mocked via NSubstitute.
/// </summary>
public class EmailViewModelTests
{
    private readonly IEmailService _emailService = Substitute.For<IEmailService>();
    private readonly IMatterRepository _matters   = Substitute.For<IMatterRepository>();
    private readonly IClientRepository _clients   = Substitute.For<IClientRepository>();
    private readonly EmailClassificationService _classifier = new();

    private EmailViewModel BuildSut() =>
        new(_emailService, _matters, _clients, _classifier);

    // ── FetchAndClassify: auto-classifies high-confidence emails ──────────────

    [Fact]
    public async Task FetchAndClassify_AutoClassifies_WhenConfidenceIsHigh()
    {
        // Arrange: matter ref in subject → MatterRefRule gives 0.95
        var matter = new Matter
        {
            Id = 1, MatterRef = "PROP-2026-0042", Title = "Test", ClientId = 1,
            Status = MatterStatus.Active, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        var email = new EmailRecord
        {
            Id = 10, GmailMessageId = "g1",
            Subject = "RE: PROP-2026-0042 – contract query",
            From = "unknown@example.com",
            ClassificationStatus = EmailClassificationStatus.Unclassified,
            SentAt = DateTime.UtcNow, FetchedAt = DateTime.UtcNow
        };

        _emailService.FetchNewEmailsAsync().Returns([email]);
        _matters.GetActiveAsync().Returns([matter]);
        _clients.GetAllAsync().Returns([]);
        _emailService.GetUnclassifiedEmailsAsync().Returns([]);

        // Act
        var sut = BuildSut();
        await sut.FetchAndClassifyCommand.ExecuteAsync(null);

        // Assert: high confidence → auto-classify called
        await _emailService.Received(1).ClassifyEmailAsync(10, 1);
    }

    [Fact]
    public async Task FetchAndClassify_DoesNotAutoClassify_WhenConfidenceIsLow()
    {
        var email = new EmailRecord
        {
            Id = 11, GmailMessageId = "g2",
            Subject = "Hello, do you have a minute?",
            From = "unknown@nowhere.com",
            ClassificationStatus = EmailClassificationStatus.Unclassified,
            SentAt = DateTime.UtcNow, FetchedAt = DateTime.UtcNow
        };

        _emailService.FetchNewEmailsAsync().Returns([email]);
        _matters.GetActiveAsync().Returns([]);
        _clients.GetAllAsync().Returns([]);
        _emailService.GetUnclassifiedEmailsAsync().Returns([email]);

        var sut = BuildSut();
        await sut.FetchAndClassifyCommand.ExecuteAsync(null);

        await _emailService.DidNotReceive().ClassifyEmailAsync(Arg.Any<int>(), Arg.Any<int>());
    }

    // ── SuggestedClassification on selection ─────────────────────────────────

    [Fact]
    public async Task SelectEmail_PopulatesSuggestedMatterAndReason()
    {
        var matter = new Matter
        {
            Id = 5, MatterRef = "PROP-2026-0099", Title = "Walsh Declan",
            ClientId = 1, Status = MatterStatus.Active,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        var email = new EmailRecord
        {
            Id = 20, GmailMessageId = "g3",
            Subject = "PROP-2026-0099 update",
            From = "client@example.com",
            ClassificationStatus = EmailClassificationStatus.Unclassified,
            SentAt = DateTime.UtcNow, FetchedAt = DateTime.UtcNow
        };

        _matters.GetActiveAsync().Returns([matter]);
        _clients.GetAllAsync().Returns([]);

        var sut = BuildSut();
        await sut.LoadSuggestionForEmailAsync(email);

        sut.SuggestedMatterId.Should().Be(5);
        sut.SuggestionReason.Should().NotBeNullOrEmpty();
        sut.SuggestionConfidence.Should().BeGreaterThan(0f);
    }

    [Fact]
    public async Task SelectEmail_ClearsSuggestion_WhenNoMatchFound()
    {
        var email = new EmailRecord
        {
            Id = 21, GmailMessageId = "g4",
            Subject = "Random unrelated email",
            From = "nobody@spam.com",
            ClassificationStatus = EmailClassificationStatus.Unclassified,
            SentAt = DateTime.UtcNow, FetchedAt = DateTime.UtcNow
        };

        _matters.GetActiveAsync().Returns([]);
        _clients.GetAllAsync().Returns([]);

        var sut = BuildSut();
        await sut.LoadSuggestionForEmailAsync(email);

        sut.SuggestedMatterId.Should().BeNull();
    }

    // ── NeedsReview queue ─────────────────────────────────────────────────────

    [Fact]
    public async Task FetchAndClassify_LoadsUnclassifiedAndReviewQueues()
    {
        var unclassified = new EmailRecord
        {
            Id = 30, GmailMessageId = "g5", Subject = "Unknown",
            ClassificationStatus = EmailClassificationStatus.Unclassified,
            SentAt = DateTime.UtcNow, FetchedAt = DateTime.UtcNow
        };
        var needsReview = new EmailRecord
        {
            Id = 31, GmailMessageId = "g6", Subject = "Maybe",
            ClassificationStatus = EmailClassificationStatus.NeedsReview,
            SentAt = DateTime.UtcNow, FetchedAt = DateTime.UtcNow
        };

        _emailService.FetchNewEmailsAsync().Returns([]);
        _matters.GetActiveAsync().Returns([]);
        _clients.GetAllAsync().Returns([]);
        _emailService.GetUnclassifiedEmailsAsync().Returns([unclassified]);
        _emailService.GetNeedsReviewEmailsAsync().Returns([needsReview]);

        var sut = BuildSut();
        await sut.FetchAndClassifyCommand.ExecuteAsync(null);

        sut.UnclassifiedEmails.Should().HaveCount(1);
        sut.NeedsReviewEmails.Should().HaveCount(1);
    }

    // ── IsLoading guard ───────────────────────────────────────────────────────

    [Fact]
    public async Task FetchAndClassify_SetsIsLoadingDuringFetch()
    {
        var loadingValues = new List<bool>();
        _emailService.FetchNewEmailsAsync().Returns([]);
        _matters.GetActiveAsync().Returns([]);
        _clients.GetAllAsync().Returns([]);
        _emailService.GetUnclassifiedEmailsAsync().Returns([]);
        _emailService.GetNeedsReviewEmailsAsync().Returns([]);

        var sut = BuildSut();
        sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(sut.IsLoading))
                loadingValues.Add(sut.IsLoading);
        };

        await sut.FetchAndClassifyCommand.ExecuteAsync(null);

        loadingValues.Should().Contain(true, "IsLoading should be true during fetch");
        loadingValues.Last().Should().BeFalse("IsLoading should be false after fetch");
    }
}
