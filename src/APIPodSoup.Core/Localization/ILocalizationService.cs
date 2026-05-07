namespace APIPodSoup.Core.Localization;

public interface ILocalizationService
{
    string this[string key] { get; }
    string Get(string key);
    string Get(string key, params object?[] args);
    string CurrentLanguage { get; }
    void LoadLanguage(string languageCode);
}
