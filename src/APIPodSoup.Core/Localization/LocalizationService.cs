using System.ComponentModel;
using System.IO;
using System.Text.Json;

namespace APIPodSoup.Core.Localization;

public class LocalizationService : ILocalizationService, INotifyPropertyChanged
{
    private Dictionary<string, string> _strings = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _langDir;

    public static LocalizationService Instance { get; private set; } = null!;

    /// <summary>Fires so all indexer bindings refresh when language changes.</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Current language code ("en", "zh", etc.).</summary>
    public string CurrentLanguage { get; private set; } = "en";

    public LocalizationService(string langDir)
    {
        Instance = this;
        _langDir = langDir;
    }

    public string this[string key]
    {
        get => _strings.TryGetValue(key, out var val) ? val : key;
    }

    public string Get(string key) => this[key];

    public string Get(string key, params object?[] args) => string.Format(this[key], args);

    /// <summary>
    /// Load language from a JSON file named {languageCode}.json in the lang directory.
    /// Falls back to "en" if the requested language file doesn't exist.
    /// </summary>
    public void LoadLanguage(string languageCode)
    {
        var path = Path.Combine(_langDir, $"{languageCode}.json");
        if (!File.Exists(path))
        {
            path = Path.Combine(_langDir, "en.json");
            languageCode = "en";
        }

        var json = File.ReadAllText(path);
        var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                   ?? new Dictionary<string, string>();

        _strings = new Dictionary<string, string>(dict, StringComparer.OrdinalIgnoreCase);
        CurrentLanguage = languageCode;

        // Notify all indexer bindings to refresh
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
    }
}
