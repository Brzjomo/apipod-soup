using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using APIPodSoup.App.ViewModels;

namespace APIPodSoup.App.Views;

public partial class TextToImagePage : UserControl
{
    public TextToImagePage()
    {
        InitializeComponent();
    }

    private void OnDragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
            var vm = (TextToImageViewModel)DataContext;
            vm.OnDragEnter();
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
    }

    private void OnDragLeave(object sender, DragEventArgs e)
    {
        var vm = (TextToImageViewModel)DataContext;
        vm.OnDragLeave();
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            var vm = (TextToImageViewModel)DataContext;
            vm.OnDrop(files);
        }
    }

    private void OnResultImageClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is Image img && img.Tag is string path)
        {
            var vm = (TextToImageViewModel)DataContext;
            vm.OpenResultInDefaultAppCommand.Execute(path);
        }
    }
}
