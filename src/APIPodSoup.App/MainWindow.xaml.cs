using System.Windows;
using APIPodSoup.App.ViewModels;

namespace APIPodSoup.App;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
