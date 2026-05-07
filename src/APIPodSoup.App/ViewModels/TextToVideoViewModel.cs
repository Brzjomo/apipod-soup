using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Media.Imaging;
using APIPodSoup.Core.Enums;
using APIPodSoup.Core.Models;
using APIPodSoup.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using CoreTaskStatus = APIPodSoup.Core.Enums.TaskStatus;

namespace APIPodSoup.App.ViewModels;

public partial class TextToVideoViewModel : ObservableObject
{
    private readonly IApiPodService _apiService;
    private readonly IOssService _ossService;
    private readonly IDownloadService _downloadService;
    private readonly IHistoryService _historyService;
    private readonly IModelProfileProvider _profileProvider;

    public TextToVideoViewModel(
        IApiPodService apiService,
        IOssService ossService,
        IDownloadService downloadService,
        IHistoryService historyService,
        IModelProfileProvider profileProvider)
    {
        _apiService = apiService;
        _ossService = ossService;
        _downloadService = downloadService;
        _historyService = historyService;
        _profileProvider = profileProvider;

        OssStatus = _ossService.StatusMessage;

        var profiles = _profileProvider.GetProfilesByCategory(ModelCategory.VideoGeneration);
        foreach (var p in profiles)
            Models.Add(p);

        SelectedModel = profiles.FirstOrDefault();
    }

    // ---- Model ----
    public ObservableCollection<ModelProfile> Models { get; } = [];

    [ObservableProperty]
    private ModelProfile? _selectedModel;

    partial void OnSelectedModelChanged(ModelProfile? value)
    {
        if (value == null) return;
        AspectRatios.Clear();
        foreach (var r in value.SupportedAspectRatios) AspectRatios.Add(r);
        SelectedAspectRatio = value.SupportedAspectRatios.FirstOrDefault();
        ShowAspectRatio = value.SupportedAspectRatios.Length > 0;
        ShowQuality = value.SupportedQualities.Length > 0;
        Qualities.Clear();
        foreach (var q in value.SupportedQualities) Qualities.Add(q);
        SelectedQuality = value.SupportedQualities.FirstOrDefault();
        PromptLabel = value.PromptLabel;
        ReferenceLabel = value.ReferenceLabel;
        PromptMaxLength = value.MaxPromptLength;
        MaxReferenceCount = value.MaxReferenceImages;
    }

    // ---- Labels ----
    [ObservableProperty] private string _ossStatus = string.Empty;
    [ObservableProperty] private string _promptLabel = "Video Description";
    [ObservableProperty] private string _referenceLabel = "Reference Image (optional)";
    [ObservableProperty] private int _promptMaxLength = 4000;
    [ObservableProperty] private int _maxReferenceCount = 1;
    [ObservableProperty] private bool _showQuality;
    [ObservableProperty] private bool _showAspectRatio = true;

    // ---- Inputs ----
    [ObservableProperty] private string _prompt = string.Empty;

    public ObservableCollection<string> AspectRatios { get; private set; } = [];
    [ObservableProperty] private string? _selectedAspectRatio;

    public ObservableCollection<string> Qualities { get; private set; } = [];
    [ObservableProperty] private string? _selectedQuality;

    public ObservableCollection<string> ReferenceImages { get; } = [];
    [ObservableProperty] private bool _isDragging;

    // ---- Progress ----
    [ObservableProperty] private string _stepText = string.Empty;
    [ObservableProperty] private bool _isProcessing;
    [ObservableProperty] private double _progress;
    [ObservableProperty] private string _progressText = string.Empty;

    // ---- Results ----
    public ObservableCollection<string> ResultVideos { get; } = [];

    // ---- Commands ----
    [RelayCommand]
    private async Task GenerateAsync()
    {
        if (string.IsNullOrWhiteSpace(Prompt) || SelectedModel == null) return;

        if (ReferenceImages.Count > 0 && !_ossService.IsConfigured)
        {
            StepText = "Error: OSS not configured. To use reference images, set up OSS credentials in Settings.";
            ProgressText = "Reference image upload failed";
            return;
        }

        IsProcessing = true;
        try
        {
            StepText = "Uploading reference images...";
            Progress = 0.1;

            var ossUrls = new List<string>();
            if (ReferenceImages.Count > 0)
            {
                for (int i = 0; i < ReferenceImages.Count; i++)
                {
                    var url = await _ossService.UploadFileAsync(ReferenceImages[i]);
                    ossUrls.Add(url);
                    Progress = 0.1 + 0.2 * (i + 1) / ReferenceImages.Count;
                }
            }

            StepText = "Submitting video generation task...";
            Progress = 0.3;

            var request = new ImageGenerationRequest
            {
                Model = SelectedModel.ModelId,
                Prompt = Prompt,
                AspectRatio = SelectedAspectRatio ?? "16:9",
                Quality = SelectedQuality ?? "",
                ImageUrls = ossUrls,
            };

            var submitResult = await _apiService.SubmitVideoGenerationAsync(request);
            if (submitResult.Code != 200)
                throw new InvalidOperationException($"API error: {submitResult.Message}");

            var taskId = submitResult.Data?.TaskId ?? throw new InvalidOperationException("No task ID returned");

            var history = new HistoryRecord
            {
                ModelType = SelectedModel.ModelId,
                ModelDisplayName = SelectedModel.DisplayName,
                Prompt = Prompt,
                AspectRatio = SelectedAspectRatio ?? "",
                Quality = SelectedQuality ?? "",
                ReferenceImagePaths = JsonSerializer.Serialize(ReferenceImages.ToList()),
                OssUrls = JsonSerializer.Serialize(ossUrls),
                ApiRequestJson = JsonSerializer.Serialize(request),
                TaskId = taskId,
                Status = CoreTaskStatus.Submitted.ToString().ToLower(),
            };
            await _historyService.CreateAsync(history);

            StepText = "Generating video...";
            Progress = 0.4;
            ProgressText = "Video generation may take several minutes...";

            var stopwatch = Stopwatch.StartNew();
            ProgressText = "Video generation may take several minutes... (0s)";

            using var timerCts = new CancellationTokenSource();
            _ = Task.Run(async () =>
            {
                while (!timerCts.Token.IsCancellationRequested)
                {
                    try { await Task.Delay(1000, timerCts.Token); }
                    catch (OperationCanceledException) { break; }
                    var ts = stopwatch.Elapsed;
                    ProgressText = $"Generating video... ({ts.Minutes * 60 + ts.Seconds}s)";
                }
            }, timerCts.Token);

            var taskResult = await _apiService.WaitForVideoCompletionAsync(taskId, 5000);
            timerCts.Cancel();
            stopwatch.Stop();

            if (taskResult.Data?.ResultUrls == null || taskResult.Data.ResultUrls.Count == 0)
                throw new InvalidOperationException("No result URLs returned");

            StepText = "Downloading video...";
            Progress = 0.8;

            var prefix = $"{SelectedModel.DisplayName.Replace(" ", "_")}_{SelectedAspectRatio}";
            var localPaths = await _downloadService.DownloadFilesAsync(
                taskResult.Data.ResultUrls, prefix);

            await StoreResultBlobsAsync(history.Id, localPaths);

            history.ApiResponseJson = JsonSerializer.Serialize(taskResult);
            history.ResultUrls = JsonSerializer.Serialize(taskResult.Data.ResultUrls);
            history.LocalResultPaths = JsonSerializer.Serialize(localPaths);
            history.Status = CoreTaskStatus.Completed.ToString().ToLower();
            history.CompletedAt = DateTime.UtcNow;
            await _historyService.UpdateAsync(history);

            ResultVideos.Clear();
            foreach (var path in localPaths)
                ResultVideos.Add(path);

            var elapsedSec = (int)stopwatch.Elapsed.TotalSeconds;
            StepText = "Done!";
            Progress = 1.0;
            ProgressText = $"Generated {localPaths.Count} video(s) in {elapsedSec}s";

            await Task.Delay(1500);
            var mainVm = App.Host.Services.GetRequiredService<MainViewModel>();
            mainVm.SelectedNavItem = "History";
        }
        catch (Exception ex)
        {
            StepText = $"Error: {ex.Message}";
            ProgressText = "Generation failed";
            Progress = 0;
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private void AddReferenceImages()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Images|*.png;*.jpg;*.jpeg;*.webp",
            Multiselect = true,
        };
        if (dlg.ShowDialog() == true)
        {
            var wasEmpty = ReferenceImages.Count == 0;
            foreach (var file in dlg.FileNames)
            {
                if (ReferenceImages.Count >= MaxReferenceCount) break;
                if (!ReferenceImages.Contains(file))
                    ReferenceImages.Add(file);
            }
            if (wasEmpty && ReferenceImages.Count > 0)
                AutoMatchAspectRatio(ReferenceImages[0]);
        }
    }

    [RelayCommand]
    private void RemoveReferenceImage(string path) => ReferenceImages.Remove(path);

    [RelayCommand]
    private void OpenResultFolder()
    {
        if (ResultVideos.Count > 0)
        {
            var dir = Path.GetDirectoryName(ResultVideos[0]);
            if (dir != null) Process.Start("explorer.exe", dir);
        }
    }

    [RelayCommand]
    private void OpenResultInDefaultApp(string? path)
    {
        if (path != null && File.Exists(path))
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }

    private async Task StoreResultBlobsAsync(string historyId, List<string> localPaths)
    {
        var blobs = new List<ResultBlob>();
        foreach (var path in localPaths)
        {
            var data = await File.ReadAllBytesAsync(path);
            var contentType = Path.GetExtension(path).ToLower() switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".webp" => "image/webp",
                ".gif" => "image/gif",
                ".mp4" => "video/mp4",
                ".mov" => "video/quicktime",
                ".webm" => "video/webm",
                _ => "application/octet-stream",
            };
            blobs.Add(new ResultBlob
            {
                HistoryRecordId = historyId,
                FileName = Path.GetFileName(path),
                ContentType = contentType,
                Data = data,
            });
        }
        await _historyService.SaveResultBlobsAsync(historyId, blobs);
    }

    public void OnDragEnter() => IsDragging = true;
    public void OnDragLeave() => IsDragging = false;

    public void LoadFromRecord(HistoryRecord record)
    {
        var profile = Models.FirstOrDefault(m => m.ModelId == record.ModelType);
        if (profile != null) SelectedModel = profile;

        Prompt = record.Prompt;
        SelectedAspectRatio = record.AspectRatio;

        var qualityMatch = Qualities.FirstOrDefault(q => q == record.Quality);
        if (qualityMatch != null) SelectedQuality = qualityMatch;

        ReferenceImages.Clear();
        if (!string.IsNullOrEmpty(record.ReferenceImagePaths))
        {
            try
            {
                var paths = JsonSerializer.Deserialize<List<string>>(record.ReferenceImagePaths);
                if (paths != null)
                {
                    foreach (var p in paths)
                    {
                        if (File.Exists(p) && ReferenceImages.Count < MaxReferenceCount)
                            ReferenceImages.Add(p);
                    }
                }
            }
            catch { }
        }
    }

    public void OnDrop(string[] files)
    {
        IsDragging = false;
        var wasEmpty = ReferenceImages.Count == 0;
        foreach (var file in files)
        {
            var ext = Path.GetExtension(file).ToLower();
            if (ReferenceImages.Count >= MaxReferenceCount) break;
            if (new[] { ".png", ".jpg", ".jpeg", ".webp" }.Contains(ext) &&
                !ReferenceImages.Contains(file))
                ReferenceImages.Add(file);
        }
        if (wasEmpty && ReferenceImages.Count > 0)
            AutoMatchAspectRatio(ReferenceImages[0]);
    }

    private void AutoMatchAspectRatio(string imagePath)
    {
        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(imagePath);
            bitmap.EndInit();
            bitmap.Freeze();

            var imageRatio = (double)bitmap.PixelWidth / bitmap.PixelHeight;

            string? bestMatch = null;
            double bestDiff = double.MaxValue;
            foreach (var ratio in AspectRatios)
            {
                var parts = ratio.Split(':');
                if (parts.Length == 2 &&
                    double.TryParse(parts[0], out var w) &&
                    double.TryParse(parts[1], out var h) && h > 0)
                {
                    var diff = Math.Abs(imageRatio - w / h);
                    if (diff < bestDiff)
                    {
                        bestDiff = diff;
                        bestMatch = ratio;
                    }
                }
            }

            if (bestMatch != null)
                SelectedAspectRatio = bestMatch;
        }
        catch { }
    }
}
