using Google.Apis.Auth.OAuth2;
using Google.Apis.Util;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace PropertyMatterHub.Infrastructure.Google;

public class GoogleAuthService
{
    public static readonly string[] Scopes =
    [
        "https://www.googleapis.com/auth/gmail.modify",
        "https://www.googleapis.com/auth/calendar"
    ];

    private readonly string _credentialsPath;
    private readonly string _tokenFolder;
    private readonly ILogger<GoogleAuthService> _logger;
    private UserCredential? _credential;

    public GoogleAuthService(IConfiguration config, ILogger<GoogleAuthService> logger)
    {
        _credentialsPath = config["Google:CredentialsPath"]
            ?? @"Z:\PropertyMatterHub\credentials.json";
        _tokenFolder = config["Google:TokenStorePath"]
            ?? @"Z:\PropertyMatterHub\google-token";
        _logger = logger;
    }

    public bool IsCredentialsFilePresent => File.Exists(_credentialsPath);

    // Single source of truth for "do we have a usable token right now?"
    public bool IsAuthorised => _credential is not null && IsTokenValid(_credential);

    public async Task<UserCredential> GetCredentialAsync(CancellationToken ct = default)
    {
        if (_credential is not null && IsTokenValid(_credential))
            return _credential;

        if (!File.Exists(_credentialsPath))
            throw new FileNotFoundException(
                $"Google credentials file not found at '{_credentialsPath}'. " +
                "Download it from Google Cloud Console and place it on the Z: drive.",
                _credentialsPath);

        await using var stream = File.OpenRead(_credentialsPath);
        var clientSecrets = await GoogleClientSecrets.FromStreamAsync(stream, ct);

        _credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            clientSecrets.Secrets,
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

    private static bool IsTokenValid(UserCredential credential) =>
        !credential.Token.IsExpired(SystemClock.Default);
}
