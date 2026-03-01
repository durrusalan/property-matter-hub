using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PropertyMatterHub.App.Services;
using PropertyMatterHub.App.ViewModels;
using PropertyMatterHub.Core.Interfaces;
using PropertyMatterHub.Core.Services;
using PropertyMatterHub.Infrastructure.Data;
using PropertyMatterHub.Infrastructure.Data.Repositories;
using PropertyMatterHub.Infrastructure.Excel;
using PropertyMatterHub.Infrastructure.FileSystem;
using System.IO;
using System.Windows;

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
                cfg.SetBasePath(AppContext.BaseDirectory)
                   .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
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

                // Stub Google services (replaced when Google auth is configured)
                services.AddSingleton<IEmailService, NullEmailService>();
                services.AddSingleton<ICalendarService, NullCalendarService>();

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

                services.AddSingleton<MainWindow>();
            })
            .Build();

        await _host.StartAsync();

        // Initialise the database
        var db = _host.Services.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
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
