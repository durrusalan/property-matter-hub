using FluentAssertions;
using PropertyMatterHub.App.Services;
using Xunit;

namespace PropertyMatterHub.App.Tests;

/// <summary>
/// RED tests for FirstRunService — first-launch detection and user-settings persistence.
/// Uses a temp directory as the settings store so tests never touch real AppData.
/// </summary>
public class FirstRunServiceTests : IDisposable
{
    private readonly string _tempDir  = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly string _cfgPath;

    public FirstRunServiceTests()
    {
        Directory.CreateDirectory(_tempDir);
        _cfgPath = Path.Combine(_tempDir, "appsettings.user.json");
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    private FirstRunService BuildSut() => new(_cfgPath);

    // ── IsFirstRun ────────────────────────────────────────────────────────────

    [Fact]
    public void IsFirstRun_ReturnsTrue_WhenUserConfigFileAbsent()
    {
        BuildSut().IsFirstRun.Should().BeTrue();
    }

    [Fact]
    public void IsFirstRun_ReturnsFalse_WhenUserConfigFileExists()
    {
        File.WriteAllText(_cfgPath, "{}");
        BuildSut().IsFirstRun.Should().BeFalse();
    }

    // ── SaveUserSettingsAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task SaveUserSettingsAsync_CreatesConfigFile()
    {
        var settings = new UserSettings(
            ZDriveRoot:          @"Z:\Clients",
            CaseFolderPattern:   @"^(?<ClientName>.+?)\s*-\s*(?<CaseNumber>.+)$",
            CaseFolderDepth:     1,
            ExcelPath:           @"Z:\ClientDatabase.xlsx",
            DatabasePath:        @"Z:\PropertyMatterHub\hub.db");

        await BuildSut().SaveUserSettingsAsync(settings);

        File.Exists(_cfgPath).Should().BeTrue();
    }

    [Fact]
    public async Task SaveUserSettingsAsync_WritesZDriveSection_ToJson()
    {
        var settings = new UserSettings(
            ZDriveRoot:          @"Z:\Clients",
            CaseFolderPattern:   @"^pattern$",
            CaseFolderDepth:     2,
            ExcelPath:           @"Z:\data.xlsx",
            DatabasePath:        @"Z:\hub.db");

        await BuildSut().SaveUserSettingsAsync(settings);

        var json = await File.ReadAllTextAsync(_cfgPath);
        json.Should().Contain(@"Z:\\Clients");
        json.Should().Contain("CaseFolderDepth");
    }

    [Fact]
    public async Task SaveUserSettingsAsync_IsIdempotent_OverwritesExisting()
    {
        // Write a unique sentinel that can't collide with any generated JSON key.
        File.WriteAllText(_cfgPath, "SENTINEL_SHOULD_BE_GONE");

        await BuildSut().SaveUserSettingsAsync(new UserSettings(
            @"Z:\Clients", @"pattern", 1, @"Z:\data.xlsx", @"Z:\hub.db"));

        var json = await File.ReadAllTextAsync(_cfgPath);
        json.Should().NotContain("SENTINEL");
    }

    [Fact]
    public async Task SaveUserSettingsAsync_CreatesDirectoryIfMissing()
    {
        var deepPath = Path.Combine(_tempDir, "nested", "deep", "settings.json");
        var sut = new FirstRunService(deepPath);

        await sut.SaveUserSettingsAsync(new UserSettings(
            @"Z:\Clients", @"pattern", 1, @"Z:\data.xlsx", @"Z:\hub.db"));

        File.Exists(deepPath).Should().BeTrue();
    }

    [Fact]
    public async Task IsFirstRun_ReturnsFalse_AfterSave()
    {
        var sut = BuildSut();
        sut.IsFirstRun.Should().BeTrue();

        await sut.SaveUserSettingsAsync(new UserSettings(
            @"Z:\Clients", @"pattern", 1, @"Z:\data.xlsx", @"Z:\hub.db"));

        sut.IsFirstRun.Should().BeFalse();
    }
}
