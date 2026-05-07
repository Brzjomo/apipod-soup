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

        // If the field already has a value (loaded from config), hide it.
        // If empty, show the editable box so the user can type freely.
        IsApiKeyVisible = string.IsNullOrEmpty(ApiKey);
        IsOssAkVisible = string.IsNullOrEmpty(OssAccessKey);
        IsOssSkVisible = string.IsNullOrEmpty(OssSecretKey);
    }

    // Track previous values to detect when the user starts typing in an empty field
    private string _previousApiKey = string.Empty;
    private string _previousOssAk = string.Empty;
    private string _previousOssSk = string.Empty;

    partial void OnApiKeyChanging(string value) => _previousApiKey = ApiKey;
    partial void OnApiKeyChanged(string value)
    {
        // User just typed into an empty field → keep it visible
        if (string.IsNullOrEmpty(_previousApiKey) && !string.IsNullOrEmpty(value))
            IsApiKeyVisible = true;
    }

    partial void OnOssAccessKeyChanging(string value) => _previousOssAk = OssAccessKey;
    partial void OnOssAccessKeyChanged(string value)
    {
        if (string.IsNullOrEmpty(_previousOssAk) && !string.IsNullOrEmpty(value))
            IsOssAkVisible = true;
    }

    partial void OnOssSecretKeyChanging(string value) => _previousOssSk = OssSecretKey;
    partial void OnOssSecretKeyChanged(string value)
    {
        if (string.IsNullOrEmpty(_previousOssSk) && !string.IsNullOrEmpty(value))
            IsOssSkVisible = true;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowApiKeyMask))]
    [NotifyPropertyChangedFor(nameof(CanShowApiKeyEdit))]
    [NotifyPropertyChangedFor(nameof(ShowApiKeyEye))]
    private string _apiKey = string.Empty;
    [ObservableProperty] private string _apiBaseUrl = "https://api.apipod.ai";
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowOssAkMask))]
    [NotifyPropertyChangedFor(nameof(CanShowOssAkEdit))]
    [NotifyPropertyChangedFor(nameof(ShowOssAkEye))]
    private string _ossAccessKey = string.Empty;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowOssSkMask))]
    [NotifyPropertyChangedFor(nameof(CanShowOssSkEdit))]
    [NotifyPropertyChangedFor(nameof(ShowOssSkEye))]
    private string _ossSecretKey = string.Empty;
    [ObservableProperty] private string _ossEndpoint = string.Empty;
    [ObservableProperty] private string _ossRegion = string.Empty;
    [ObservableProperty] private string _ossBucketName = string.Empty;
    [ObservableProperty] private string _downloadDirectory = string.Empty;
    [ObservableProperty] private bool _isDarkTheme = true;
    [ObservableProperty] private string _saveStatus = string.Empty;

    // Password visibility toggles
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowApiKeyMask))]
    [NotifyPropertyChangedFor(nameof(CanShowApiKeyEdit))]
    private bool _isApiKeyVisible;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowOssAkMask))]
    [NotifyPropertyChangedFor(nameof(CanShowOssAkEdit))]
    private bool _isOssAkVisible;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowOssSkMask))]
    [NotifyPropertyChangedFor(nameof(CanShowOssSkEdit))]
    private bool _isOssSkVisible;

    // Computed: only show the dot-mask when hidden AND the field actually has a value
    public bool ShowApiKeyMask => !IsApiKeyVisible && !string.IsNullOrEmpty(ApiKey);
    public bool ShowOssAkMask => !IsOssAkVisible && !string.IsNullOrEmpty(OssAccessKey);
    public bool ShowOssSkMask => !IsOssSkVisible && !string.IsNullOrEmpty(OssSecretKey);

    // Computed: always show the editable box when value is empty (so field doesn't disappear)
    public bool CanShowApiKeyEdit => IsApiKeyVisible || string.IsNullOrEmpty(ApiKey);
    public bool CanShowOssAkEdit => IsOssAkVisible || string.IsNullOrEmpty(OssAccessKey);
    public bool CanShowOssSkEdit => IsOssSkVisible || string.IsNullOrEmpty(OssSecretKey);

    // Computed: only show the eye toggle when there's actually a value to protect
    public bool ShowApiKeyEye => !string.IsNullOrEmpty(ApiKey);
    public bool ShowOssAkEye => !string.IsNullOrEmpty(OssAccessKey);
    public bool ShowOssSkEye => !string.IsNullOrEmpty(OssSecretKey);

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
            // After saving, hide fields that have values; keep empty fields visible
            IsApiKeyVisible = string.IsNullOrEmpty(ApiKey);
            IsOssAkVisible = string.IsNullOrEmpty(OssAccessKey);
            IsOssSkVisible = string.IsNullOrEmpty(OssSecretKey);
        }
        catch (Exception ex)
        {
            SaveStatus = $"Save failed: {ex.Message}";
        }
    }
}
