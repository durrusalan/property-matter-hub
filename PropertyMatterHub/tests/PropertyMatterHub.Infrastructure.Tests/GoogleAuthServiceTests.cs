using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using PropertyMatterHub.Infrastructure.Google;
using Xunit;

namespace PropertyMatterHub.Infrastructure.Tests;

/// <summary>
/// Baseline tests for GoogleAuthService (non-credential-path logic).
/// Credential-path tests live in GoogleAuthServiceCredentialTests.cs.
/// </summary>
public class GoogleAuthServiceTests : IDisposable
{
    private readonly string _tempDir;

    public GoogleAuthServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    private GoogleAuthService BuildSut(params (string key, string value)[] entries)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(entries.Select(e =>
                new KeyValuePair<string, string?>(e.key, e.value)))
            .Build();
        return new GoogleAuthService(config, NullLogger<GoogleAuthService>.Instance);
    }

    [Fact]
    public void IsAuthorised_ReturnsFalse_BeforeAnyAuth()
    {
        BuildSut().IsAuthorised.Should().BeFalse();
    }

    [Fact]
    public async Task GetCredentialAsync_Throws_WhenNoCredentialsConfigured()
    {
        var sut = BuildSut();   // nothing in config, nothing set at runtime

        await sut.Invoking(s => s.GetCredentialAsync())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Client ID*");
    }

    [Fact]
    public void Scopes_ContainsGmailAndCalendar()
    {
        GoogleAuthService.Scopes.Should().Contain(s => s.Contains("gmail"));
        GoogleAuthService.Scopes.Should().Contain(s => s.Contains("calendar"));
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);
}
