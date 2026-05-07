using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using APIPodSoup.App.ViewModels;

namespace APIPodSoup.App.Views;

public partial class TextToVideoPage : UserControl
{
    public TextToVideoPage()
    {
        InitializeComponent();
    }

    private void OnDragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
            var vm = (TextToVideoViewModel)DataContext;
            vm.OnDragEnter();
        }
        else e.Effects = DragDropEffects.None;
    }

    private void OnDragLeave(object sender, DragEventArgs e)
    {
        var vm = (TextToVideoViewModel)DataContext;
        vm.OnDragLeave();
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            var vm = (TextToVideoViewModel)DataContext;
            vm.OnDrop(files);
        }
    }
}
