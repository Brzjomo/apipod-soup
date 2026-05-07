using System.Windows;
using System.IO;
using APIPodSoup.App.ViewModels;
using APIPodSoup.App.Views;
using APIPodSoup.Core.Data;
using APIPodSoup.Core.Models;
using APIPodSoup.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace APIPodSoup.App;

public partial class App : Application
{
    public static IHost Host { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var dbPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "history.db");

        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();

        builder.Configuration
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

        builder.Services.Configure<AppSettings>(builder.Configuration);

        // Database
        builder.Services.AddDbContextFactory<AppDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        // HTTP — AuthHandler injects API key on every request, reads live from settings
        builder.Services.AddTransient<AuthHandler>();
        builder.Services.AddHttpClient<IApiPodService, ApiPodService>()
            .AddHttpMessageHandler<AuthHandler>();
        builder.Services.AddHttpClient<IDownloadService, DownloadService>();

        // Services
        builder.Services.AddSingleton<IModelProfileProvider, ModelProfileProvider>();
        builder.Services.AddSingleton<IOssService, VolcengineOssService>();
        builder.Services.AddSingleton<IHistoryService, HistoryService>();

        // ViewModels
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddTransient<TextToImageViewModel>();
        builder.Services.AddTransient<TextToVideoViewModel>();
        builder.Services.AddTransient<HistoryViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();

        // Views
        builder.Services.AddTransient<MainWindow>();

        Host = builder.Build();

        // Ensure database and all tables exist (including new ones on upgrade)
        using var scope = Host.Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        using var db = factory.CreateDbContext();
        db.Database.EnsureCreated();
        // Manually create the ResultBlobs table for databases created before this table existed
        db.Database.ExecuteSqlRaw(
            "CREATE TABLE IF NOT EXISTS ResultBlobs (" +
            "Id TEXT NOT NULL PRIMARY KEY, " +
            "HistoryRecordId TEXT NOT NULL, " +
            "FileName TEXT NOT NULL, " +
            "ContentType TEXT NOT NULL, " +
            "Data BLOB NOT NULL, " +
            "CreatedAt TEXT NOT NULL)");
        db.Database.ExecuteSqlRaw(
            "CREATE INDEX IF NOT EXISTS IX_ResultBlobs_HistoryRecordId ON ResultBlobs(HistoryRecordId)");

        var mainWindow = Host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Host.Dispose();
        base.OnExit(e);
    }
}
