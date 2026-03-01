using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace PropertyMatterHub.Infrastructure.Google;

/// <summary>
/// Handles Google OAuth 2.0 without requiring a credentials.json file on disk.
/// Client ID + Secret can be supplied either via appsettings (Google:ClientId /
/// Google:ClientSecret) or at runtime through <see cref="SetClientSecrets"/>.
/// The browser-based consent flow is handled by GoogleWebAuthorizationBroker.
/// </summary>
public class GoogleAuthService : IGoogleAuthService
{
    public static readonly string[] Scopes =
    [
        "https://www.googleapis.com/auth/gmail.modify",
        "https://www.googleapis.com/auth/calendar"
    ];

    private readonly string _tokenFolder;
    private readonly ILogger<GoogleAuthService> _logger;

    // Credentials sourced from config (appsettings / user config).
    private readonly string? _configClientId;
    private readonly string? _configClientSecret;

    // Credentials supplied at runtime by the user via the credentials dialog.
    private string? _runtimeClientId;
    private string? _runtimeClientSecret;

    private UserCredential? _credential;

    public GoogleAuthService(IConfiguration config, ILogger<GoogleAuthService> logger)
    {
        _tokenFolder       = config["Google:TokenStorePath"]
                             ?? Path.Combine(
                                 Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                 "PropertyMatterHub", "google-token");
        _configClientId     = config["Google:ClientId"];
        _configClientSecret = config["Google:ClientSecret"];
        _logger             = logger;
    }

    // ── IGoogleAuthService ────────────────────────────────────────────────────

    public bool IsAuthorised => _credential is not null && !_credential.Token.IsStale;

    public bool HasCredentials
    {
        get
        {
            // Runtime secrets take priority; fall back to config.
            var id     = NonEmpty(_runtimeClientId)     ?? NonEmpty(_configClientId);
            var secret = NonEmpty(_runtimeClientSecret) ?? NonEmpty(_configClientSecret);
            return id is not null && secret is not null;
        }
    }

    public void SetClientSecrets(string clientId, string clientSecret)
    {
        _runtimeClientId     = clientId;
        _runtimeClientSecret = clientSecret;
    }

    public async Task<UserCredential> GetCredentialAsync(CancellationToken ct = default)
    {
        if (_credential is not null && IsAuthorised)
            return _credential;

        var id     = NonEmpty(_runtimeClientId)     ?? NonEmpty(_configClientId);
        var secret = NonEmpty(_runtimeClientSecret) ?? NonEmpty(_configClientSecret);

        if (id is null || secret is null)
            throw new InvalidOperationException(
                "Google Client ID and Client Secret are required. " +
                "Click \"Connect Google Account\" and enter your credentials from Google Cloud Console.");

        var secrets = new ClientSecrets { ClientId = id, ClientSecret = secret };

        _credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            secrets,
            Scopes,
            "shared-office-user",
            ct,
            new FileDataStore(_tokenFolder, fullPath: true));

        _logger.LogInformation("Google OAuth authorised. Token stored at {Path}", _tokenFolder);
        return _credential;
    }

    public async Task RevokeAsync(CancellationToken ct = default)
    {
        if (_credential is not null)
        {
            await _credential.RevokeTokenAsync(ct);
            _credential = null;
        }

        if (Directory.Exists(_tokenFolder))
            Directory.Delete(_tokenFolder, recursive: true);

        _logger.LogInformation("Google OAuth token revoked.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string? NonEmpty(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s;
}
