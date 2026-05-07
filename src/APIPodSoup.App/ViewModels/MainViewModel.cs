using System.Windows.Input;
using APIPodSoup.Core.Enums;
using APIPodSoup.Core.Models;
using APIPodSoup.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace APIPodSoup.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IOptionsMonitor<AppSettings> _settings;
    private readonly IModelProfileProvider _profileProvider;

    [ObservableProperty]
    private object? _currentView;

    [ObservableProperty]
    private string _selectedNavItem = "TextToImage";

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private string _balanceText = "--";

    public MainViewModel(IOptionsMonitor<AppSettings> settings, IModelProfileProvider profileProvider)
    {
        _settings = settings;
        _profileProvider = profileProvider;
        NavigateCommand = new RelayCommand<string?>(Navigate);
        CurrentView = App.Host.Services.GetRequiredService<TextToImageViewModel>();
        RefreshStatus();
    }

    public ICommand NavigateCommand { get; }

    public void RefreshStatus()
    {
        var hasApiKey = !string.IsNullOrWhiteSpace(_settings.CurrentValue.ApiKey);
        IsConnected = hasApiKey;
        StatusText = hasApiKey ? "API available" : "API unavailable — configure in Settings";
    }

    private void Navigate(string? target)
    {
        if (target == null) return;
        SelectedNavItem = target;
        RefreshStatus();
    }

    /// <summary>
    /// Navigate to the generation page matching this record's model category,
    /// and pre-fill all parameters for reuse.
    /// </summary>
    public void NavigateToRecord(HistoryRecord record)
    {
        var profile = _profileProvider.GetProfile(record.ModelType);
        if (profile == null) return;

        var target = profile.Category == ModelCategory.VideoGeneration
            ? "TextToVideo" : "TextToImage";

        if (target == "TextToVideo")
        {
            var vm = App.Host.Services.GetRequiredService<TextToVideoViewModel>();
            vm.LoadFromRecord(record);
            CurrentView = vm;
        }
        else
        {
            var vm = App.Host.Services.GetRequiredService<TextToImageViewModel>();
            vm.LoadFromRecord(record);
            CurrentView = vm;
        }

        // Set field directly to avoid OnSelectedNavItemChanged overwriting CurrentView
        #pragma warning disable MVVMTK0034
        _selectedNavItem = target;
        #pragma warning restore MVVMTK0034
        OnPropertyChanged(nameof(SelectedNavItem));
        RefreshStatus();
    }

    partial void OnSelectedNavItemChanged(string value)
    {
        CurrentView = value switch
        {
            "TextToImage" => App.Host.Services.GetRequiredService<TextToImageViewModel>(),
            "TextToVideo" => App.Host.Services.GetRequiredService<TextToVideoViewModel>(),
            "History" => App.Host.Services.GetRequiredService<HistoryViewModel>(),
            "Settings" => App.Host.Services.GetRequiredService<SettingsViewModel>(),
            _ => CurrentView
        };
    }
}
