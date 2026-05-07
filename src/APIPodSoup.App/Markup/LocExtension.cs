using System.Windows.Data;
using System.Windows.Markup;
using APIPodSoup.Core.Localization;

namespace APIPodSoup.App.Markup;

/// <summary>
/// XAML markup extension: {loc:Loc Key=Nav.TextToImage}
/// Creates a OneWay binding that auto-updates when language changes.
/// </summary>
public class LocExtension : MarkupExtension
{
    public string Key { get; set; } = string.Empty;

    public LocExtension() { }

    public LocExtension(string key) => Key = key;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var binding = new Binding($"[{Key}]")
        {
            Source = LocalizationService.Instance,
            Mode = BindingMode.OneWay,
        };
        return binding.ProvideValue(serviceProvider);
    }
}
