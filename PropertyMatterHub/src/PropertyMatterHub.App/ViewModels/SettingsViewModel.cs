using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
using PropertyMatterHub.Infrastructure.FileSystem;

namespace PropertyMatterHub.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IConfiguration _config;
    private readonly ZDriveScanner _scanner;

    [ObservableProperty] private string _zDriveRoot = @"Z:\";
    [ObservableProperty] private string _excelPath = @"Z:\ClientDatabase.xlsx";
    [ObservableProperty] private string _caseFolderPattern = @"^(?<ClientName>.+?)\s*-\s*(?<CaseNumber>.+)$";
    [ObservableProperty] private int _caseFolderDepth = 1;
    [ObservableProperty] private string _patternTestResult = string.Empty;
    [ObservableProperty] private bool _isGoogleAuthorized;
    [ObservableProperty] private string _googleAuthStatus = "Not connected";
    [ObservableProperty] private bool _isSaving;

    public SettingsViewModel(IConfiguration config, ZDriveScanner scanner)
    {
        _config  = config;
        _scanner = scanner;
        LoadFromConfig();
    }

    private void LoadFromConfig()
    {
        ZDriveRoot         = _config["ZDrive:RootPath"]          ?? @"Z:\";
        ExcelPath          = _config["ZDrive:ExcelPath"]         ?? @"Z:\ClientDatabase.xlsx";
        CaseFolderPattern  = _config["ZDrive:CaseFolderPattern"] ?? CaseFolderPattern;
        CaseFolderDepth    = int.TryParse(_config["ZDrive:CaseFolderDepth"], out var d) ? d : 1;
    }

    [RelayCommand]
    private void TestPattern()
    {
        try
        {
            var cfg = new FolderStructureConfig
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
    private async Task SaveAsync()
    {
        IsSaving = true;
        try
        {
            // In a real app, write to appsettings.json and notify services
            await Task.Delay(200);
        }
        finally { IsSaving = false; }
    }
}
