using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using APIPodSoup.App.ViewModels;

namespace APIPodSoup.App.Views;

public partial class SettingsPage : UserControl
{
    private SettingsViewModel? _vm;

    public SettingsPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _vm = DataContext as SettingsViewModel;
    }

    // Click on the masked dot area to reveal the field
    private void OnApiKeyMaskClicked(object sender, MouseButtonEventArgs e)
    {
        _vm?.ToggleApiKeyVisibilityCommand.Execute(null);
    }

    private void OnOssAkMaskClicked(object sender, MouseButtonEventArgs e)
    {
        _vm?.ToggleOssAkVisibilityCommand.Execute(null);
    }

    private void OnOssSkMaskClicked(object sender, MouseButtonEventArgs e)
    {
        _vm?.ToggleOssSkVisibilityCommand.Execute(null);
    }
}
