using System.IO;
using System.Text.Json;
using APIPodSoup.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Options;

namespace APIPodSoup.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IOptionsMonitor<AppSettings> _optionsMonitor;

    public SettingsViewModel(IOptionsMonitor<AppSettings> optionsMonitor)
    {
        _optionsMonitor = optionsMonitor;
        var settings = optionsMonitor.CurrentValue;

        ApiKey = settings.ApiKey;
        ApiBaseUrl = settings.ApiBaseUrl;
        OssAccessKey = settings.Oss.AccessKey;
        OssSecretKey = settings.Oss.SecretKey;
        OssEndpoint = settings.Oss.Endpoint;
        OssRegion = settings.Oss.Region;
        OssBucketName = settings.Oss.BucketName;
        DownloadDirectory = settings.DownloadDirectory;
        IsDarkTheme = settings.Theme == "Dark";
    }

    [ObservableProperty] private string _apiKey = string.Empty;
    [ObservableProperty] private string _apiBaseUrl = "https://api.apipod.ai";
    [ObservableProperty] private string _ossAccessKey = string.Empty;
    [ObservableProperty] private string _ossSecretKey = string.Empty;
    [ObservableProperty] private string _ossEndpoint = string.Empty;
    [ObservableProperty] private string _ossRegion = string.Empty;
    [ObservableProperty] private string _ossBucketName = string.Empty;
    [ObservableProperty] private string _downloadDirectory = string.Empty;
    [ObservableProperty] private bool _isDarkTheme = true;
    [ObservableProperty] private string _saveStatus = string.Empty;

    // Password visibility toggles
    [ObservableProperty] private bool _isApiKeyVisible;
    [ObservableProperty] private bool _isOssAkVisible;
    [ObservableProperty] private bool _isOssSkVisible;

    [RelayCommand]
    private void ToggleApiKeyVisibility() => IsApiKeyVisible = !IsApiKeyVisible;

    [RelayCommand]
    private void ToggleOssAkVisibility() => IsOssAkVisible = !IsOssAkVisible;

    [RelayCommand]
    private void ToggleOssSkVisibility() => IsOssSkVisible = !IsOssSkVisible;

    [RelayCommand]
    private void BrowseDownloadDir()
    {
        var dlg = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Select download directory for generated results",
        };

        if (dlg.ShowDialog() == true)
            DownloadDirectory = dlg.FolderName;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            var configPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

            var json = JsonSerializer.Serialize(new AppSettings
            {
                ApiKey = ApiKey,
                ApiBaseUrl = ApiBaseUrl,
                Oss = new OssSettings
                {
                    AccessKey = OssAccessKey,
                    SecretKey = OssSecretKey,
                    Endpoint = OssEndpoint,
                    Region = OssRegion,
                    BucketName = OssBucketName,
                },
                DownloadDirectory = DownloadDirectory,
                Theme = IsDarkTheme ? "Dark" : "Light",
            }, new JsonSerializerOptions { WriteIndented = true });

            await File.WriteAllTextAsync(configPath, json);
            SaveStatus = $"Settings saved at {DateTime.Now:HH:mm:ss}";
            IsApiKeyVisible = false;
            IsOssAkVisible = false;
            IsOssSkVisible = false;
        }
        catch (Exception ex)
        {
            SaveStatus = $"Save failed: {ex.Message}";
        }
    }
}
