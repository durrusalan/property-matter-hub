using FluentAssertions;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using PropertyMatterHub.App.Services;
using PropertyMatterHub.App.ViewModels;
using PropertyMatterHub.Infrastructure.FileSystem;
using PropertyMatterHub.Infrastructure.Google;
using Xunit;

namespace PropertyMatterHub.App.Tests;

/// <summary>
/// RED tests for Google auth commands in SettingsViewModel.
/// IGoogleAuthService is mocked to avoid real OAuth flows.
/// </summary>
public class SettingsGoogleTests
{
    private readonly IConfiguration    _cfg     = new ConfigurationBuilder().Build();
    private readonly ZDriveScanner     _scanner;
    private readonly ZDriveIndexingService _indexer = null!; // not exercised here
    private readonly IGoogleAuthService _googleAuth = Substitute.For<IGoogleAuthService>();

    public SettingsGoogleTests()
    {
        var cfg = new FolderStructureConfig { RootPath = @"C:\nonexistent", CaseFolderPattern = ".*" };
        _scanner = new ZDriveScanner(cfg);
        // ZDriveIndexingService requires a DB — use null (commands won't be tested here)
        _indexer = null!;
    }

    private SettingsViewModel BuildSut() =>
        new(_cfg, _scanner, _indexer, _googleAuth);

    // ── Initial state ─────────────────────────────────────────────────────────

    [Fact]
    public void Initialises_GoogleAuthStatus_FromService()
    {
        _googleAuth.IsAuthorised.Returns(true);
        var sut = BuildSut();
        sut.IsGoogleAuthorized.Should().BeTrue();
    }

    [Fact]
    public void Initialises_WithNotConnected_WhenNotAuthorised()
    {
        _googleAuth.IsAuthorised.Returns(false);
        var sut = BuildSut();
        sut.IsGoogleAuthorized.Should().BeFalse();
        sut.GoogleAuthStatus.Should().Contain("Not");
    }

    // ── ConnectGoogleCommand ──────────────────────────────────────────────────

    [Fact]
    public async Task ConnectGoogle_CallsGetCredentialAsync()
    {
        _googleAuth.IsAuthorised.Returns(false);
        // NSubstitute returns null by default for reference-type Task<T> results.
        // We only need the call to succeed without throwing.

        var sut = BuildSut();
        await sut.ConnectGoogleCommand.ExecuteAsync(null);

        await _googleAuth.Received(1).GetCredentialAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConnectGoogle_SetsIsGoogleAuthorized_True_OnSuccess()
    {
        _googleAuth.IsAuthorised.Returns(false, true);   // false before, true after connect

        var sut = BuildSut();
        await sut.ConnectGoogleCommand.ExecuteAsync(null);

        sut.IsGoogleAuthorized.Should().BeTrue();
    }

    [Fact]
    public async Task ConnectGoogle_SetsErrorStatus_OnException()
    {
        _googleAuth.IsAuthorised.Returns(false);
        _googleAuth.GetCredentialAsync(Arg.Any<CancellationToken>())
                   .Returns(Task.FromException<UserCredential>(new IOException("OAuth cancelled")));

        var sut = BuildSut();
        await sut.ConnectGoogleCommand.ExecuteAsync(null);

        sut.GoogleAuthStatus.Should().Contain("failed");
    }

    // ── DisconnectGoogleCommand ───────────────────────────────────────────────

    [Fact]
    public async Task DisconnectGoogle_CallsRevokeAsync()
    {
        _googleAuth.IsAuthorised.Returns(true);

        var sut = BuildSut();
        await sut.DisconnectGoogleCommand.ExecuteAsync(null);

        await _googleAuth.Received(1).RevokeAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DisconnectGoogle_SetsIsGoogleAuthorized_False()
    {
        _googleAuth.IsAuthorised.Returns(true, false);

        var sut = BuildSut();
        await sut.DisconnectGoogleCommand.ExecuteAsync(null);

        sut.IsGoogleAuthorized.Should().BeFalse();
    }

    [Fact]
    public async Task DisconnectGoogle_UpdatesGoogleAuthStatus()
    {
        // First call (init) returns true; second call (after revoke) returns false.
        _googleAuth.IsAuthorised.Returns(true, false);

        var sut = BuildSut();
        await sut.DisconnectGoogleCommand.ExecuteAsync(null);

        sut.GoogleAuthStatus.Should().Contain("Not");
    }
}
