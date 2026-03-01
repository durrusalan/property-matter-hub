using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using PropertyMatterHub.Infrastructure.Google;
using Xunit;

namespace PropertyMatterHub.Infrastructure.Tests;

/// <summary>
/// RED tests for the runtime-credential path in GoogleAuthService.
/// No real OAuth calls are made; we only test HasCredentials / SetClientSecrets logic.
/// </summary>
public class GoogleAuthServiceCredentialTests
{
    private static GoogleAuthService Build(params (string key, string value)[] entries)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(entries.Select(e =>
                new KeyValuePair<string, string?>(e.key, e.value)))
            .Build();
        return new GoogleAuthService(config, NullLogger<GoogleAuthService>.Instance);
    }

    // ── HasCredentials ────────────────────────────────────────────────────────

    [Fact]
    public void HasCredentials_False_WhenNoConfigAndNoRuntimeSecrets()
    {
        var sut = Build();
        sut.HasCredentials.Should().BeFalse();
    }

    [Fact]
    public void HasCredentials_True_WhenConfigHasClientIdAndSecret()
    {
        var sut = Build(
            ("Google:ClientId",     "my-client-id"),
            ("Google:ClientSecret", "my-client-secret"));

        sut.HasCredentials.Should().BeTrue();
    }

    [Fact]
    public void HasCredentials_False_WhenOnlyClientIdInConfig()
    {
        // Both ClientId AND ClientSecret must be present
        var sut = Build(("Google:ClientId", "my-client-id"));
        sut.HasCredentials.Should().BeFalse();
    }

    [Fact]
    public void HasCredentials_True_AfterSetClientSecrets()
    {
        var sut = Build();   // no config
        sut.HasCredentials.Should().BeFalse();

        sut.SetClientSecrets("id", "secret");

        sut.HasCredentials.Should().BeTrue();
    }

    // ── SetClientSecrets ──────────────────────────────────────────────────────

    [Fact]
    public void SetClientSecrets_OverridesConfig()
    {
        var sut = Build(
            ("Google:ClientId",     "config-id"),
            ("Google:ClientSecret", "config-secret"));

        // Calling SetClientSecrets a second time (e.g. user changes creds) must work.
        sut.SetClientSecrets("new-id", "new-secret");
        sut.HasCredentials.Should().BeTrue();
    }

    [Fact]
    public void SetClientSecrets_EmptyStrings_ClearsCredentials()
    {
        var sut = Build();
        sut.SetClientSecrets("id", "secret");
        sut.HasCredentials.Should().BeTrue();

        sut.SetClientSecrets(string.Empty, string.Empty);
        sut.HasCredentials.Should().BeFalse();
    }

    // ── GetCredentialAsync guard ──────────────────────────────────────────────

    [Fact]
    public async Task GetCredentialAsync_Throws_WhenNoCredentialsConfigured()
    {
        var sut = Build();   // nothing configured

        var act = async () => await sut.GetCredentialAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Client ID*");
    }
}
