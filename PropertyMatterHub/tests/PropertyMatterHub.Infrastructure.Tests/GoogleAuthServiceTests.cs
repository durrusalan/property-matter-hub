using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using PropertyMatterHub.Infrastructure.Google;
using Xunit;

namespace PropertyMatterHub.Infrastructure.Tests;

/// <summary>
/// RED tests for GoogleAuthService.
/// These verify the guard logic that doesn't require a real OAuth flow.
/// </summary>
public class GoogleAuthServiceTests : IDisposable
{
    private readonly string _tempDir;

    public GoogleAuthServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    private GoogleAuthService BuildSut(string credentialsPath, string tokenPath)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Google:CredentialsPath"]  = credentialsPath,
                ["Google:TokenStorePath"]   = tokenPath
            })
            .Build();
        return new GoogleAuthService(config, NullLogger<GoogleAuthService>.Instance);
    }

    [Fact]
    public void IsCredentialsFilePresent_ReturnsFalse_WhenFileDoesNotExist()
    {
        var sut = BuildSut(
            credentialsPath: Path.Combine(_tempDir, "credentials.json"),
            tokenPath: _tempDir);

        sut.IsCredentialsFilePresent.Should().BeFalse();
    }

    [Fact]
    public void IsCredentialsFilePresent_ReturnsTrue_WhenFileExists()
    {
        var credPath = Path.Combine(_tempDir, "credentials.json");
        File.WriteAllText(credPath, "{}");

        var sut = BuildSut(credentialsPath: credPath, tokenPath: _tempDir);

        sut.IsCredentialsFilePresent.Should().BeTrue();
    }

    [Fact]
    public void IsAuthorised_ReturnsFalse_BeforeAnyAuth()
    {
        var sut = BuildSut(
            credentialsPath: Path.Combine(_tempDir, "credentials.json"),
            tokenPath: _tempDir);

        sut.IsAuthorised.Should().BeFalse();
    }

    [Fact]
    public async Task GetCredentialAsync_Throws_WhenCredentialsFileMissing()
    {
        var sut = BuildSut(
            credentialsPath: Path.Combine(_tempDir, "missing.json"),
            tokenPath: _tempDir);

        await sut.Invoking(s => s.GetCredentialAsync())
            .Should().ThrowAsync<FileNotFoundException>()
            .WithMessage("*credentials file not found*");
    }

    [Fact]
    public void Scopes_ContainsGmailAndCalendar()
    {
        GoogleAuthService.Scopes.Should().Contain(s => s.Contains("gmail"));
        GoogleAuthService.Scopes.Should().Contain(s => s.Contains("calendar"));
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);
}
