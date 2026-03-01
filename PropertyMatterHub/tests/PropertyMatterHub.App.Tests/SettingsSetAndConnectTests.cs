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
/// RED tests for SettingsViewModel.SetAndConnectAsync —
/// the path the UI takes after the user provides credentials in the dialog.
/// </summary>
public class SettingsSetAndConnectTests
{
    private readonly IConfiguration     _cfg        = new ConfigurationBuilder().Build();
    private readonly IGoogleAuthService _googleAuth = Substitute.For<IGoogleAuthService>();
    private readonly ZDriveScanner      _scanner;

    public SettingsSetAndConnectTests()
    {
        var cfg = new FolderStructureConfig
        {
            RootPath = @"C:\nonexistent",
            CaseFolderPattern = ".*"
        };
        _scanner = new ZDriveScanner(cfg);
    }

    private SettingsViewModel BuildSut() =>
        new(_cfg, _scanner, null!, _googleAuth);

    // ── SetAndConnectAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task SetAndConnect_CallsSetClientSecrets_WithSuppliedValues()
    {
        var sut = BuildSut();

        await sut.SetAndConnectAsync("my-id", "my-secret");

        _googleAuth.Received(1).SetClientSecrets("my-id", "my-secret");
    }

    [Fact]
    public async Task SetAndConnect_CallsGetCredentialAsync_AfterSettingSecrets()
    {
        var sut = BuildSut();

        await sut.SetAndConnectAsync("my-id", "my-secret");

        await _googleAuth.Received(1).GetCredentialAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetAndConnect_RefreshesGoogleStatus_AfterSuccess()
    {
        _googleAuth.IsAuthorised.Returns(false, true);

        var sut = BuildSut();
        await sut.SetAndConnectAsync("my-id", "my-secret");

        sut.IsGoogleAuthorized.Should().BeTrue();
    }

    [Fact]
    public async Task SetAndConnect_SetsErrorStatus_OnException()
    {
        _googleAuth.GetCredentialAsync(Arg.Any<CancellationToken>())
                   .Returns(Task.FromException<UserCredential>(
                       new InvalidOperationException("OAuth failed")));

        var sut = BuildSut();
        await sut.SetAndConnectAsync("my-id", "my-secret");

        sut.GoogleAuthStatus.Should().Contain("failed");
    }

    [Fact]
    public async Task SetAndConnect_DoesNotThrow_WhenClientIdEmpty()
    {
        // Validation happens in the dialog; ViewModel is lenient and lets the service decide.
        var act = async () => await BuildSut().SetAndConnectAsync("", "");
        await act.Should().NotThrowAsync();
    }
}
