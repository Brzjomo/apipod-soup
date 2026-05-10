using CommunityToolkit.Mvvm.ComponentModel;

namespace APIPodSoup.App.ViewModels;

public partial class AboutViewModel : ObservableObject
{
    public string AppVersion => "1.0.2";
    public string Author => "Brzjomo";
    public string ProjectUrl => "https://github.com/Brzjomo/apipod-soup";
    public string Copyright => $"© {DateTime.Now.Year} APIPodSoup";

    // These are placeholder for localization - the XAML will use loc:Loc bindings instead
}
