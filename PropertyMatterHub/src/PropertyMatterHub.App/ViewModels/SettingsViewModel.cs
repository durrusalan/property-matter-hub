using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
using PropertyMatterHub.App.Services;
using PropertyMatterHub.Infrastructure.FileSystem;
using PropertyMatterHub.Infrastructure.Google;

namespace PropertyMatterHub.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IConfiguration      _config;
    private readonly ZDriveScanner       _scanner;
    private readonly ZDriveIndexingService _indexer;
    private readonly IGoogleAuthService  _googleAuth;
    private readonly FirstRunService?    _firstRunService;

    [ObservableProperty] private string _zDriveRoot         = @"Z:\";
    [ObservableProperty] private string _excelPath          = @"Z:\ClientDatabase.xlsx";
    [ObservableProperty] private string _caseFolderPattern  = @"^(?<ClientName>.+?)\s*-\s*(?<CaseNumber>.+)$";
    [ObservableProperty] private int    _caseFolderDepth    = 1;
    [ObservableProperty] private string _patternTestResult  = string.Empty;
    [ObservableProperty] private string _indexingResult     = string.Empty;
    [ObservableProperty] private bool   _isIndexing;
    [ObservableProperty] private bool   _isGoogleAuthorized;
    [ObservableProperty] private string _googleAuthStatus   = "Not connected";
    [ObservableProperty] private bool   _isSaving;
    [ObservableProperty] private bool   _isConnectingGoogle;

    public SettingsViewModel(
        IConfiguration config,
        ZDriveScanner scanner,
        ZDriveIndexingService indexer,
        IGoogleAuthService googleAuth,
        FirstRunService? firstRunService = null)
    {
        _config          = config;
        _scanner         = scanner;
        _indexer         = indexer;
        _googleAuth      = googleAuth;
        _firstRunService = firstRunService;
        LoadFromConfig();
        RefreshGoogleStatus();
    }

    // ── Z: drive ─────────────────────────────────────────────────────────────

    [RelayCommand]
    private void TestPattern()
    {
        try
        {
            var cfg     = new FolderStructureConfig
            {
                RootPath          = ZDriveRoot,
                CaseFolderPattern = CaseFolderPattern,
                CaseFolderDepth   = CaseFolderDepth
            };
            var scanner = new ZDriveScanner(cfg);
            var results = scanner.ScanFolders();
            var matched = results.Count(r => r.IsMatched);
            PatternTestResult = $"Found {results.Count} folder(s), {matched} matched the pattern.";
        }
        catch (Exception ex)
        {
            PatternTestResult = $"Pattern error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task RescanZDriveAsync()
    {
        IsIndexing     = true;
        IndexingResult = "Scanning…";
        try
        {
            var summary = await _indexer.RunAsync();
            IndexingResult = summary.ClientsCreated == 0 && summary.MattersCreated == 0
                ? $"Up to date — {summary.FoldersMatched} folders matched, nothing new to import."
                : $"Done: {summary.ClientsCreated} client(s) and {summary.MattersCreated} matter(s) imported from {summary.FoldersMatched} folders.";
        }
        catch (Exception ex)
        {
            IndexingResult = $"Error: {ex.Message}";
        }
        finally { IsIndexing = false; }
    }

    // ── Google auth ───────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task ConnectGoogleAsync()
    {
        IsConnectingGoogle = true;
        try
        {
            await _googleAuth.GetCredentialAsync();
            RefreshGoogleStatus();
        }
        catch (Exception ex)
        {
            GoogleAuthStatus = $"Connection failed: {ex.Message}";
        }
        finally { IsConnectingGoogle = false; }
    }

    /// <summary>
    /// Called by the view's code-behind after the user provides credentials in the dialog.
    /// Sets them on the auth service, persists them to the user config, then starts
    /// the browser OAuth flow.
    /// </summary>
    public async Task SetAndConnectAsync(string clientId, string clientSecret)
    {
        _googleAuth.SetClientSecrets(clientId, clientSecret);

        // Persist so the credentials survive restarts.
        if (_firstRunService is not null)
        {
            await _firstRunService.SaveUserSettingsAsync(new UserSettings(
                ZDriveRoot:        ZDriveRoot,
                CaseFolderPattern: CaseFolderPattern,
                CaseFolderDepth:   CaseFolderDepth,
                ExcelPath:         ExcelPath,
                DatabasePath:      _config["ZDrive:DatabasePath"] ?? @"Z:\PropertyMatterHub\hub.db"),
                googleClientId:    clientId,
                googleClientSecret: clientSecret);
        }

        await ConnectGoogleAsync();
    }

    [RelayCommand]
    private async Task DisconnectGoogleAsync()
    {
        await _googleAuth.RevokeAsync();
        RefreshGoogleStatus();
    }

    // ── Save ──────────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task SaveAsync()
    {
        IsSaving = true;
        try { await Task.Delay(100); }
        finally { IsSaving = false; }
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void LoadFromConfig()
    {
        ZDriveRoot        = _config["ZDrive:RootPath"]          ?? @"Z:\";
        ExcelPath         = _config["ZDrive:ExcelPath"]         ?? @"Z:\ClientDatabase.xlsx";
        CaseFolderPattern = _config["ZDrive:CaseFolderPattern"] ?? CaseFolderPattern;
        CaseFolderDepth   = int.TryParse(_config["ZDrive:CaseFolderDepth"], out var d) ? d : 1;
    }

    private void RefreshGoogleStatus()
    {
        IsGoogleAuthorized = _googleAuth.IsAuthorised;
        GoogleAuthStatus   = IsGoogleAuthorized ? "Connected ✓" : "Not connected";
    }
}
