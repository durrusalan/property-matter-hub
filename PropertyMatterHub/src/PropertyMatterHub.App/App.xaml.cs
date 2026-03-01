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
            .ConfigureAppConfiguration(cfg =>
            {
                cfg.SetBasePath(AppContext.BaseDirectory)
                   .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            })
            .ConfigureServices((ctx, services) =>
            {
                var zDrivePath = ctx.Configuration["ZDrive:DatabasePath"]
                                 ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                                 "PropertyMatterHub", "hub.db");

                Directory.CreateDirectory(Path.GetDirectoryName(zDrivePath)!);

                services.AddDbContext<AppDbContext>(opt =>
                    opt.UseSqlite($"Data Source={zDrivePath};Mode=ReadWriteCreate;Cache=Shared"));

                // Repositories & Core services
                services.AddScoped<IMatterRepository, MatterRepository>();
                services.AddScoped<IClientRepository, ClientRepository>();
                services.AddScoped<IExcelSyncService, ExcelSyncService>();
                services.AddScoped<MatterService>();
                services.AddScoped<ClientService>();
                services.AddScoped<SearchService>();
                services.AddScoped<EmailClassificationService>();

                // Stub Google services (replaced when Google auth is configured)
                services.AddScoped<IEmailService, NullEmailService>();
                services.AddScoped<ICalendarService, NullCalendarService>();

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

                // ViewModels
                services.AddTransient<DashboardViewModel>();
                services.AddTransient<MatterListViewModel>();
                services.AddTransient<MatterDetailViewModel>();
                services.AddTransient<ClientListViewModel>();
                services.AddTransient<EmailViewModel>();
                services.AddTransient<CalendarViewModel>();
                services.AddTransient<SettingsViewModel>();
                services.AddTransient<SearchViewModel>();
                services.AddTransient<MainViewModel>();

                services.AddTransient<MainWindow>();
            })
            .Build();

        await _host.StartAsync();

        // Run EF Core migrations on startup
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
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
}
