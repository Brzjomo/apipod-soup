using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using APIPodSoup.Core.Models;
using APIPodSoup.App.ViewModels;
using APIPodSoup.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace APIPodSoup.App.Views;

public partial class HistoryDetailWindow : Window
{
    private readonly HistoryRecord _record;

    public HistoryDetailWindow(HistoryRecord record)
    {
        InitializeComponent();
        _record = record;
        DataContext = record;
        Loaded += OnLoaded;

        // Show elapsed time next to status
        if (record.CompletedAt.HasValue)
        {
            var elapsed = record.CompletedAt.Value - record.CreatedAt;
            ElapsedText.Text = elapsed.TotalSeconds >= 60
                ? $"{elapsed.Minutes:D2}:{elapsed.Seconds:D2}"
                : $"{elapsed.Seconds}s";
        }
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var historyService = App.Host.Services.GetRequiredService<IHistoryService>();
            var blobs = await historyService.GetResultBlobsAsync(_record.Id);

            if (blobs.Count == 0) return;

            foreach (var blob in blobs)
            {
                if (IsImageContentType(blob.ContentType))
                {
                    var bitmap = CreateBitmapFromBytes(blob.Data);
                    if (bitmap != null)
                        ResultItems.Items.Add(new ImageItem { Image = bitmap, FileName = blob.FileName });
                }
            }
        }
        catch
        {
            // Fallback to local files if blobs unavailable
            LoadFromLocalPaths();
        }
    }

    private void LoadFromLocalPaths()
    {
        if (string.IsNullOrEmpty(_record.LocalResultPaths)) return;
        var paths = System.Text.Json.JsonSerializer.Deserialize<List<string>>(_record.LocalResultPaths);
        if (paths == null) return;
        foreach (var path in paths)
        {
            if (File.Exists(path))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(path);
                    bitmap.EndInit();
                    bitmap.Freeze();
                    ResultItems.Items.Add(new ImageItem
                    {
                        Image = bitmap,
                        FileName = Path.GetFileName(path)
                    });
                }
                catch { }
            }
        }
    }

    private static BitmapImage? CreateBitmapFromBytes(byte[] data)
    {
        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = new MemoryStream(data);
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsImageContentType(string contentType) =>
        contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

    private void OnImageClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not System.Windows.Controls.Image img) return;
        if (img.DataContext is not ImageItem item) return;

        // Save to temp and open
        try
        {
            var historyService = App.Host.Services.GetRequiredService<IHistoryService>();
            var blobs = historyService.GetResultBlobsAsync(_record.Id).Result;
            var blob = blobs.FirstOrDefault(b => b.FileName == item.FileName);
            if (blob != null)
            {
                var tmpPath = Path.Combine(Path.GetTempPath(), blob.FileName);
                File.WriteAllBytes(tmpPath, blob.Data);
                Process.Start(new ProcessStartInfo(tmpPath) { UseShellExecute = true });
            }
        }
        catch { }
    }

    private async void OnExportClick(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Select export directory",
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            var historyService = App.Host.Services.GetRequiredService<IHistoryService>();
            await historyService.ExportBlobsAsync(_record.Id, dlg.FolderName);
            MessageBox.Show($"Exported to {dlg.FolderName}", "Export Complete",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Export failed: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnReuseClick(object sender, RoutedEventArgs e)
    {
        var mainVm = App.Host.Services.GetRequiredService<MainViewModel>();
        mainVm.NavigateToRecord(_record);
        Close();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}

/// <summary>Holds a loaded image and its original filename for the ItemsControl.</summary>
public class ImageItem
{
    public BitmapImage? Image { get; set; }
    public string FileName { get; set; } = string.Empty;
}
