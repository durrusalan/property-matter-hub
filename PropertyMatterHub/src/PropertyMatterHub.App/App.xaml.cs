using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PropertyMatterHub.App.Services;
using PropertyMatterHub.App.ViewModels;
using PropertyMatterHub.App.Views;
using PropertyMatterHub.Core.Interfaces;
using PropertyMatterHub.Core.Services;
using PropertyMatterHub.Infrastructure.Data;
using PropertyMatterHub.Infrastructure.Data.Repositories;
using PropertyMatterHub.Infrastructure.Excel;
using PropertyMatterHub.Infrastructure.FileSystem;
using PropertyMatterHub.Infrastructure.Google;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Microsoft.Extensions.Logging;
using Wpf.Ui.Appearance;

namespace PropertyMatterHub.App;

public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = Host.CreateDefaultBuilder()
            .UseDefaultServiceProvider(opt =>
            {
                // In debug let DI warn; in release validate on build only
                opt.ValidateScopes = false;
            })
            .ConfigureAppConfiguration(cfg =>
            {
                var userCfgPath = FirstRunService.DefaultConfigPath();
                cfg.SetBasePath(AppContext.BaseDirectory)
                   .AddJsonFile("appsettings.json",           optional: true, reloadOnChange: true)
                   .AddJsonFile(userCfgPath,                  optional: true, reloadOnChange: false)
                   .AddEnvironmentVariables();
            })
            .ConfigureServices((ctx, services) =>
            {
                var dbPath = ResolveDatabasePath(ctx.Configuration);
                Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

                services.AddDbContext<AppDbContext>(opt =>
                    opt.UseSqlite($"Data Source={dbPath};Mode=ReadWriteCreate;Cache=Shared"),
                    ServiceLifetime.Singleton);   // Singleton so the same connection is shared app-wide

                // Repositories & Core services
                services.AddSingleton<IMatterRepository, MatterRepository>();
                services.AddSingleton<IClientRepository, ClientRepository>();
                services.AddSingleton<IExcelSyncService, ExcelSyncService>();
                services.AddSingleton<MatterService>();
                services.AddSingleton<ClientService>();
                services.AddSingleton<SearchService>();
                services.AddSingleton<EmailClassificationService>();

                // Google auth + live adapters
                services.AddSingleton<GoogleAuthService>();
                services.AddSingleton<IGoogleAuthService>(sp => sp.GetRequiredService<GoogleAuthService>());
                services.AddSingleton<IGmailRawClient, LiveGmailRawClient>();
                services.AddSingleton<IGmailApiAdapter, LiveGmailApiAdapter>();

                // Always register live Google services.
                // They are guarded at runtime by GoogleAuthService.HasCredentials — if the
                // user hasn't connected yet, the service gracefully returns empty results.
                services.AddSingleton<IEmailService, GmailEmailService>();
                services.AddSingleton<IGoogleCalendarClient, LiveGoogleCalendarClient>();
                services.AddSingleton<ICalendarService, GoogleCalendarService>();

                // Infrastructure
                services.AddSingleton(sp =>
                {
                    var cfg = ctx.Configuration;
                    return new FolderStructureConfig
                    {
                        RootPath          = cfg["ZDrive:RootPath"]          ?? @"Z:\",
                        CaseFolderPattern = cfg["ZDrive:CaseFolderPattern"] ?? @"^(?<ClientName>.+?)\s*-\s*(?<CaseNumber>.+)$",
                        CaseFolderDepth   = int.TryParse(cfg["ZDrive:CaseFolderDepth"], out var d) ? d : 1
                    };
                });
                services.AddSingleton<ZDriveScanner>();
                services.AddSingleton<ZDriveIndexingService>();

                // ViewModels (singleton so they keep state while navigating)
                services.AddSingleton<DashboardViewModel>();
                services.AddSingleton<MatterListViewModel>();
                services.AddSingleton<MatterDetailViewModel>();
                services.AddSingleton<ClientListViewModel>();
                services.AddSingleton<EmailViewModel>();
                services.AddSingleton<CalendarViewModel>();
                services.AddSingleton<SettingsViewModel>();
                services.AddSingleton<SearchViewModel>();
                services.AddSingleton<MainViewModel>();

                // First-run wizard
                var userCfgPath = FirstRunService.DefaultConfigPath();
                services.AddSingleton(new FirstRunService(userCfgPath));
                services.AddTransient<FirstRunWindow>();

                services.AddSingleton<MainWindow>();
            })
            .Build();

        await _host.StartAsync();

        // Initialise the database schema
        var db = _host.Services.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        // Seed from Z: drive — idempotent, only creates what's missing
        var indexer = _host.Services.GetRequiredService<ZDriveIndexingService>();
        var summary = await indexer.RunAsync();
        var logger  = _host.Services.GetRequiredService<ILogger<App>>();
        logger.LogInformation(
            "Z: drive index: {Matched}/{Total} folders matched, {C} clients + {M} matters added.",
            summary.FoldersMatched, summary.FoldersFound, summary.ClientsCreated, summary.MattersCreated);

        // Theme: light (white) with Wilson Daly logo purple as accent (#6E3D6E from logo)
        ApplicationThemeManager.Apply(ApplicationTheme.Light);
        ApplicationAccentColorManager.Apply(
            Color.FromRgb(0x6E, 0x3D, 0x6E),
            ApplicationTheme.Light);

        // Show first-run wizard if this is the user's first launch.
        var firstRunSvc = _host.Services.GetRequiredService<FirstRunService>();
        if (firstRunSvc.IsFirstRun)
        {
            var wizard = _host.Services.GetRequiredService<FirstRunWindow>();
            var accepted = wizard.ShowDialog();
            // If the user closed the wizard without clicking "Get Started", still continue.
        }

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        // Trigger initial data load for visible ViewModels
        var mainVm = _host.Services.GetRequiredService<MainViewModel>();
        _ = mainVm.Dashboard.LoadCommand.ExecuteAsync(null);
        _ = mainVm.MatterList.LoadCommand.ExecuteAsync(null);
        _ = mainVm.ClientList.LoadCommand.ExecuteAsync(null);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
        base.OnExit(e);
    }

    /// <summary>
    /// Returns the SQLite database path. If Z: drive exists, uses it.
    /// Falls back to %LocalAppData%\PropertyMatterHub\hub.db so the app
    /// still works on machines without a Z: drive mapped.
    /// </summary>
    private static string ResolveDatabasePath(IConfiguration config)
    {
        var configured = config["ZDrive:DatabasePath"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            var root = Path.GetPathRoot(configured);
            if (!string.IsNullOrEmpty(root) && Directory.Exists(root))
                return configured;
        }

        // Z: drive not available → local fallback
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PropertyMatterHub", "hub.db");
    }
}
