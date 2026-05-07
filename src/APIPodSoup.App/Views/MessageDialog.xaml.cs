using System.Windows;
using System.Windows.Input;

namespace APIPodSoup.App.Views;

public partial class MessageDialog : Window
{
    public MessageDialog(string title, string message)
    {
        InitializeComponent();
        TitleText.Text = title;
        MessageText.Text = message;
    }

    public static void ShowError(Window owner, string title, string message)
    {
        var dlg = new MessageDialog(title, message) { Owner = owner };
        dlg.ShowDialog();
    }

    public static void ShowError(string title, string message)
    {
        var owner = Application.Current.MainWindow;
        ShowError(owner, title, message);
    }

    private void OkButton_Click(object sender, RoutedEventArgs e) => Close();

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }
}
