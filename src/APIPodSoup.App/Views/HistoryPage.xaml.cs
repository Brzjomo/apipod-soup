using System.Windows.Controls;
using System.Windows.Input;
using APIPodSoup.App.ViewModels;
using APIPodSoup.Core.Models;

namespace APIPodSoup.App.Views;

public partial class HistoryPage : UserControl
{
    public HistoryPage()
    {
        InitializeComponent();
    }

    private void OnRecordClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.Tag is HistoryRecord record)
        {
            var vm = (HistoryViewModel)DataContext;
            vm.OpenDetailCommand.Execute(record);
        }
    }
}
