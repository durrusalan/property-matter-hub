using Google.Apis.Auth.OAuth2;

namespace PropertyMatterHub.Infrastructure.Google;

/// <summary>
/// Abstracts Google OAuth 2.0 so SettingsViewModel can be unit-tested
/// without real network access or real credential files.
/// </summary>
public interface IGoogleAuthService
{
    bool IsCredentialsFilePresent { get; }
    bool IsAuthorised { get; }

    Task<UserCredential> GetCredentialAsync(CancellationToken ct = default);
    Task RevokeAsync(CancellationToken ct = default);
}
