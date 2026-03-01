using Google.Apis.Auth.OAuth2;

namespace PropertyMatterHub.Infrastructure.Google;

/// <summary>
/// Abstracts Google OAuth 2.0 so SettingsViewModel can be unit-tested
/// without real network access or real credential files.
/// </summary>
public interface IGoogleAuthService
{
    /// <summary>True when a usable stored token exists.</summary>
    bool IsAuthorised { get; }

    /// <summary>
    /// True when either Client ID + Secret have been supplied (via SetClientSecrets or
    /// appsettings), so that GetCredentialAsync can start the OAuth browser flow.
    /// </summary>
    bool HasCredentials { get; }

    /// <summary>
    /// Supply Client ID and Client Secret at runtime (entered by the user in the
    /// credentials dialog).  Passing empty strings clears any previously stored values.
    /// </summary>
    void SetClientSecrets(string clientId, string clientSecret);

    /// <summary>
    /// Starts the Google OAuth browser flow and stores the resulting token.
    /// Throws <see cref="InvalidOperationException"/> if no credentials have been configured.
    /// </summary>
    Task<UserCredential> GetCredentialAsync(CancellationToken ct = default);

    Task RevokeAsync(CancellationToken ct = default);
}
