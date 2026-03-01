using FluentAssertions;
using NSubstitute;
using PropertyMatterHub.App.ViewModels;
using PropertyMatterHub.Core.Interfaces;
using PropertyMatterHub.Core.Models;
using PropertyMatterHub.Core.Services;
using Xunit;

namespace PropertyMatterHub.App.Tests;

/// <summary>
/// RED tests for compose/send behaviour in EmailViewModel.
/// </summary>
public class EmailComposeViewModelTests
{
    private readonly IEmailService _emailService = Substitute.For<IEmailService>();
    private readonly IMatterRepository _matters   = Substitute.For<IMatterRepository>();
    private readonly IClientRepository _clients   = Substitute.For<IClientRepository>();
    private readonly EmailClassificationService _classifier = new();

    private EmailViewModel BuildSut()
    {
        _matters.GetActiveAsync().Returns([]);
        _clients.GetAllAsync().Returns([]);
        _emailService.FetchNewEmailsAsync().Returns([]);
        _emailService.GetUnclassifiedEmailsAsync().Returns([]);
        _emailService.GetNeedsReviewEmailsAsync().Returns([]);
        return new(_emailService, _matters, _clients, _classifier);
    }

    // ── Compose toggle ────────────────────────────────────────────────────────

    [Fact]
    public void Compose_SetsIsComposing_True()
    {
        var sut = BuildSut();
        sut.ComposeCommand.Execute(null);
        sut.IsComposing.Should().BeTrue();
    }

    [Fact]
    public void CancelCompose_SetsIsComposing_False()
    {
        var sut = BuildSut();
        sut.ComposeCommand.Execute(null);
        sut.CancelComposeCommand.Execute(null);
        sut.IsComposing.Should().BeFalse();
    }

    [Fact]
    public void CancelCompose_ClearsAllComposeFields()
    {
        var sut = BuildSut();
        sut.ComposeTo      = "someone@example.com";
        sut.ComposeSubject = "Test";
        sut.ComposeBody    = "Body";
        sut.ComposeMatterId = 3;

        sut.CancelComposeCommand.Execute(null);

        sut.ComposeTo.Should().BeEmpty();
        sut.ComposeSubject.Should().BeEmpty();
        sut.ComposeBody.Should().BeEmpty();
        sut.ComposeMatterId.Should().BeNull();
    }

    // ── Send guard ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Send_DoesNotCallService_WhenToIsEmpty()
    {
        var sut = BuildSut();
        sut.ComposeTo      = string.Empty;
        sut.ComposeSubject = "Hello";
        sut.ComposeBody    = "Body";

        await sut.SendCommand.ExecuteAsync(null);

        await _emailService.DidNotReceive().SendEmailAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<IEnumerable<string>?>());
    }

    [Fact]
    public async Task Send_DoesNotCallService_WhenToIsWhitespace()
    {
        var sut = BuildSut();
        sut.ComposeTo = "   ";

        await sut.SendCommand.ExecuteAsync(null);

        await _emailService.DidNotReceive().SendEmailAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<IEnumerable<string>?>());
    }

    // ── Send success ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Send_CallsService_WithCorrectArguments()
    {
        var sut = BuildSut();
        sut.ComposeTo       = "client@example.com";
        sut.ComposeSubject  = "Contract update";
        sut.ComposeBody     = "Please find attached.";
        sut.ComposeMatterId = 42;

        await sut.SendCommand.ExecuteAsync(null);

        await _emailService.Received(1).SendEmailAsync(
            "client@example.com",
            "Contract update",
            "Please find attached.",
            "42",
            Arg.Any<IEnumerable<string>?>());
    }

    [Fact]
    public async Task Send_ResetsComposeFields_AfterSuccess()
    {
        var sut = BuildSut();
        sut.ComposeTo      = "a@b.com";
        sut.ComposeSubject = "Sub";
        sut.ComposeBody    = "Bdy";

        await sut.SendCommand.ExecuteAsync(null);

        sut.ComposeTo.Should().BeEmpty();
        sut.ComposeSubject.Should().BeEmpty();
        sut.ComposeBody.Should().BeEmpty();
    }

    [Fact]
    public async Task Send_ClosesComposePanel_AfterSuccess()
    {
        var sut = BuildSut();
        sut.ComposeCommand.Execute(null);
        sut.ComposeTo = "a@b.com";

        await sut.SendCommand.ExecuteAsync(null);

        sut.IsComposing.Should().BeFalse();
    }

    [Fact]
    public async Task Send_PassesNullMatterId_WhenComposeMatterIdNotSet()
    {
        var sut = BuildSut();
        sut.ComposeTo       = "x@y.com";
        sut.ComposeMatterId = null;

        await sut.SendCommand.ExecuteAsync(null);

        await _emailService.Received(1).SendEmailAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            null,
            Arg.Any<IEnumerable<string>?>());
    }
}
