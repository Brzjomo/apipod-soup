using System.Collections.ObjectModel;
using APIPodSoup.Core.Models;
using APIPodSoup.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace APIPodSoup.App.ViewModels;

public partial class HistoryViewModel : ObservableObject
{
    private readonly IHistoryService _historyService;
    private int _currentPage = 0;
    private const int PageSize = 20;

    public HistoryViewModel(IHistoryService historyService)
    {
        _historyService = historyService;
        _ = LoadHistoryAsync();
    }

    public ObservableCollection<HistoryRecord> Records { get; } = [];

    [ObservableProperty]
    private HistoryRecord? _selectedRecord;

    [ObservableProperty]
    private bool _hasMore = true;

    [ObservableProperty]
    private bool _isLoading;

    [RelayCommand]
    private async Task LoadHistoryAsync()
    {
        if (IsLoading) return;
        IsLoading = true;

        try
        {
            var records = await _historyService.GetAllAsync(_currentPage * PageSize, PageSize);
            Records.Clear();
            foreach (var r in records)
                Records.Add(r);

            _currentPage++;
            HasMore = records.Count == PageSize;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        _currentPage = 0;
        await LoadHistoryAsync();
    }

    [RelayCommand]
    private async Task OpenDetailAsync(HistoryRecord? record)
    {
        if (record == null) return;
        var detailWindow = new Views.HistoryDetailWindow(record);
        detailWindow.Owner = System.Windows.Application.Current.MainWindow;
        detailWindow.ShowDialog();
    }

    [RelayCommand]
    private async Task DeleteRecordAsync(HistoryRecord? record)
    {
        if (record == null) return;
        await _historyService.DeleteAsync(record.Id);
        _currentPage = 0;
        await LoadHistoryAsync();
    }

    [RelayCommand]
    private void OpenFile(string? path)
    {
        if (path != null && System.IO.File.Exists(path))
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path)
            {
                UseShellExecute = true
            });
    }
}
