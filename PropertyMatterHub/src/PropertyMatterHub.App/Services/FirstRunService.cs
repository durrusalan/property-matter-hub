using System.IO;
using System.Text.Json;

namespace PropertyMatterHub.App.Services;

/// <summary>
/// Persists user-specific settings to a JSON file in %LocalAppData% so that
/// first-run detection and Z: drive path changes survive app updates.
/// </summary>
public class FirstRunService
{
    private readonly string _configPath;

    public FirstRunService(string configPath) => _configPath = configPath;

    /// <summary>True until <see cref="SaveUserSettingsAsync"/> is called at least once.</summary>
    public bool IsFirstRun => !File.Exists(_configPath);

    public async Task SaveUserSettingsAsync(
        UserSettings settings,
        string? googleClientId     = null,
        string? googleClientSecret = null,
        CancellationToken ct = default)
    {
        var dir = Path.GetDirectoryName(_configPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var payload = new
        {
            ZDrive = new
            {
                RootPath          = settings.ZDriveRoot,
                CaseFolderPattern = settings.CaseFolderPattern,
                CaseFolderDepth   = settings.CaseFolderDepth,
                ExcelPath         = settings.ExcelPath,
                DatabasePath      = settings.DatabasePath
            },
            Google = string.IsNullOrWhiteSpace(googleClientId) ? null : new
            {
                ClientId     = googleClientId,
                ClientSecret = googleClientSecret
            }
        };

        var json = JsonSerializer.Serialize(payload,
            new JsonSerializerOptions { WriteIndented = true });

        await File.WriteAllTextAsync(_configPath, json, ct);
    }

    /// <summary>
    /// Factory helper — resolves the config path from environment, matching
    /// App.xaml.cs so both places use the same file.
    /// </summary>
    public static string DefaultConfigPath() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PropertyMatterHub", "appsettings.user.json");
}

/// <summary>Snapshot of the settings the user configures in the wizard / Settings view.</summary>
public record UserSettings(
    string ZDriveRoot,
    string CaseFolderPattern,
    int    CaseFolderDepth,
    string ExcelPath,
    string DatabasePath);
