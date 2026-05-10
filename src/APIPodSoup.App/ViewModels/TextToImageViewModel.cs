using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Media.Imaging;
using APIPodSoup.App.Views;
using APIPodSoup.Core.Enums;
using APIPodSoup.Core.Models;
using APIPodSoup.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using APIPodSoup.Core.Localization;
using CoreTaskStatus = APIPodSoup.Core.Enums.TaskStatus;

namespace APIPodSoup.App.ViewModels;

public partial class TextToImageViewModel : ObservableObject
{
    private readonly IApiPodService _apiService;
    private readonly IOssService _ossService;
    private readonly IDownloadService _downloadService;
    private readonly IHistoryService _historyService;
    private readonly IModelProfileProvider _profileProvider;
    private readonly ILocalizationService _loc;

    public TextToImageViewModel(
        IApiPodService apiService,
        IOssService ossService,
        IDownloadService downloadService,
        IHistoryService historyService,
        IModelProfileProvider profileProvider,
        ILocalizationService loc)
    {
        _apiService = apiService;
        _ossService = ossService;
        _downloadService = downloadService;
        _historyService = historyService;
        _profileProvider = profileProvider;
        _loc = loc;

        _promptLabel = _loc["Gen.Label.ImageDesc"];
        _referenceLabel = _loc["Gen.Label.RefImages"];

        // Re-apply localized labels when language changes
        if (loc is System.ComponentModel.INotifyPropertyChanged inpc)
            inpc.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == "Item[]")
                {
                    PromptLabel = _loc["Gen.Label.ImageDesc"];
                    ReferenceLabel = _loc["Gen.Label.RefImages"];
                }
            };

        OssStatus = _ossService.StatusMessage;

        var profiles = _profileProvider.GetProfilesByCategory(ModelCategory.ImageGeneration);

        foreach (var p in profiles)
            Models.Add(p);

        SelectedModel = profiles.FirstOrDefault();
    }

    public ObservableCollection<ModelProfile> Models { get; } = [];

    [ObservableProperty]
    private ModelProfile? _selectedModel;

    partial void OnSelectedModelChanged(ModelProfile? value)
    {
        if (value == null) return;
        // Clear + re-add so ObservableCollection notifies ComboBox binding
        AspectRatios.Clear();
        foreach (var r in value.SupportedAspectRatios) AspectRatios.Add(r);
        Qualities.Clear();
        foreach (var q in value.SupportedQualities) Qualities.Add(q);
        SelectedAspectRatio = value.SupportedAspectRatios.FirstOrDefault();
        ShowAspectRatio = value.SupportedAspectRatios.Length > 0;
        SelectedQuality = value.SupportedQualities.FirstOrDefault();
        ShowQuality = value.SupportedQualities.Length > 0;
        PromptMaxLength = value.MaxPromptLength;
        MaxReferenceCount = value.MaxReferenceImages;
        ShowReferenceSection = MaxReferenceCount > 0;
        ShowOutputCount = value.ShowOutputCount;
        MaxOutputCount = value.MaxOutputCount;
        SelectedOutputCount = value.ShowOutputCount ? 1 : 0;
        UsesSizeParam = value.UsesSizeParam;
        ShowThinkingMode = value.ShowThinkingMode;
        IsThinkingEnabled = false;
    }

    [ObservableProperty] private string _ossStatus = string.Empty;
    [ObservableProperty] private string _promptLabel = string.Empty;
    [ObservableProperty] private string _referenceLabel = string.Empty;
    [ObservableProperty] private int _promptMaxLength = 4000;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReferenceLimitText))]
    private int _maxReferenceCount = 8;
    [ObservableProperty] private bool _showReferenceSection = true;

    public string ReferenceLimitText => MaxReferenceCount > 0
        ? _loc.Get("Gen.Label.RefLimit", MaxReferenceCount) : "";
    [ObservableProperty] private bool _showQuality = true;
    [ObservableProperty] private bool _showAspectRatio = true;

    [ObservableProperty] private string _prompt = string.Empty;

    public ObservableCollection<string> AspectRatios { get; private set; } = [];
    [ObservableProperty] private string? _selectedAspectRatio;

    public ObservableCollection<string> Qualities { get; private set; } = [];
    [ObservableProperty] private string? _selectedQuality;

    [ObservableProperty] private bool _showOutputCount;
    [ObservableProperty] private int _maxOutputCount = 1;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OutputCountText))]
    private int _selectedOutputCount;
    [ObservableProperty] private bool _usesSizeParam;
    [ObservableProperty] private bool _showThinkingMode;
    [ObservableProperty] private bool _isThinkingEnabled;

    public string OutputCountText => $"{SelectedOutputCount}";

    public ObservableCollection<string> ReferenceImages { get; } = [];
    [ObservableProperty] private bool _isDragging;

    [ObservableProperty] private string _stepText = string.Empty;
    [ObservableProperty] private bool _isProcessing;
    [ObservableProperty] private double _progress;
    [ObservableProperty] private string _progressText = string.Empty;

    public ObservableCollection<string> ResultImages { get; } = [];
    [ObservableProperty] private string? _selectedResultImage;

    [RelayCommand]
    private async Task GenerateAsync()
    {
        if (SelectedModel == null) return;

        if (string.IsNullOrWhiteSpace(Prompt))
        {
            MessageDialog.ShowError(_loc["Dialog.GenerationFailed"], _loc["Gen.Error.NoPrompt"]);
            StepText = _loc["Gen.Error.NoPrompt"];
            return;
        }

        if (ReferenceImages.Count > MaxReferenceCount)
        {
            var msg = _loc.Get("Gen.Error.TooManyRefs", MaxReferenceCount);
            MessageDialog.ShowError(_loc["Dialog.GenerationFailed"], msg);
            StepText = msg;
            return;
        }

        // Models that require reference images
        if (SelectedModel.RequireReferenceImage && ReferenceImages.Count == 0)
        {
            MessageDialog.ShowError(_loc["Dialog.GenerationFailed"], _loc["Gen.Error.RequireRef"]);
            StepText = _loc["Gen.Error.RequireRef"];
            ProgressText = _loc["Gen.Error.RefHint"];
            return;
        }

        // Reference images require OSS — check before starting
        if (ReferenceImages.Count > 0 && !_ossService.IsConfigured)
        {
            StepText = _loc["Gen.Error.OssConfig"];
            ProgressText = _loc["Gen.Error.OssUpload"];
            return;
        }

        IsProcessing = true;
        HistoryRecord? history = null;
        try
        {
            StepText = _loc["Gen.UploadingRefs"];
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

            StepText = _loc["Gen.Submitting"];
            Progress = 0.3;

            var request = new ImageGenerationRequest
            {
                Model = SelectedModel.ModelId,
                Prompt = Prompt,
                AspectRatio = ShowAspectRatio ? (SelectedAspectRatio ?? "1:1") : null,
                Quality = !UsesSizeParam && !string.IsNullOrEmpty(SelectedQuality) ? SelectedQuality : null,
                Size = UsesSizeParam && !string.IsNullOrEmpty(SelectedQuality) ? SelectedQuality : null,
                N = ShowOutputCount ? SelectedOutputCount : null,
                ThinkingMode = ShowThinkingMode ? IsThinkingEnabled : null,
                ImageUrls = ossUrls,
            };

            var submitResult = await _apiService.SubmitImageGenerationAsync(request);
            if (submitResult.Code != 200)
                throw new InvalidOperationException(_loc.Get("Gen.Error.ApiError", submitResult.Message));

            var taskId = submitResult.Data?.TaskId ?? throw new InvalidOperationException(_loc["Gen.Error.NoTaskId"]);

            history = new HistoryRecord
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

            StepText = _loc["Gen.Generating"];
            Progress = 0.4;
            ProgressText = _loc["Gen.Waiting"];

            var stopwatch = Stopwatch.StartNew();
            ProgressText = _loc["Gen.Waiting"];

            // Background timer to update elapsed display
            using var timerCts = new CancellationTokenSource();
            _ = Task.Run(async () =>
            {
                while (!timerCts.Token.IsCancellationRequested)
                {
                    try { await Task.Delay(1000, timerCts.Token); }
                    catch (OperationCanceledException) { break; }
                    var ts = stopwatch.Elapsed;
                    ProgressText = _loc.Get("Gen.WaitingElapsed", ts.Minutes * 60 + ts.Seconds);
                }
            }, timerCts.Token);

            try
            {
                var taskResult = await _apiService.WaitForCompletionAsync(taskId, 5000);

                if (taskResult.Data?.ResultUrls == null || taskResult.Data.ResultUrls.Count == 0)
                    throw new InvalidOperationException(_loc["Gen.Error.NoResultUrl"]);

                Debug.WriteLine($"[Result] Download URLs:");
                foreach (var url in taskResult.Data.ResultUrls)
                    Debug.WriteLine($"  {url}");

                StepText = _loc["Gen.Downloading"];
                Progress = 0.8;

                var prefix = $"{SelectedModel.DisplayName.Replace(" ", "_")}_{SelectedQuality}";
                var localPaths = await _downloadService.DownloadFilesAsync(
                    taskResult.Data.ResultUrls, prefix);

                // Store result files as blobs in the database
                await StoreResultBlobsAsync(history.Id, localPaths);

                history.ApiResponseJson = JsonSerializer.Serialize(taskResult);
                history.ResultUrls = JsonSerializer.Serialize(taskResult.Data.ResultUrls);
                history.LocalResultPaths = JsonSerializer.Serialize(localPaths);
                history.Status = CoreTaskStatus.Completed.ToString().ToLower();
                history.CompletedAt = DateTime.UtcNow;  // used to compute elapsed in detail view
                await _historyService.UpdateAsync(history);

                ResultImages.Clear();
                foreach (var path in localPaths)
                    ResultImages.Add(path);

                var elapsedSec = (int)stopwatch.Elapsed.TotalSeconds;
                StepText = _loc["Gen.Done"];
                Progress = 1.0;
                ProgressText = _loc.Get("Gen.ResultCount", localPaths.Count, elapsedSec);

                // Auto‑navigate to History after a short pause (only if user hasn't navigated away)
                await Task.Delay(1500);
                var mainVm = App.Host.Services.GetRequiredService<MainViewModel>();
                if (mainVm.SelectedNavItem == "TextToImage")
                    mainVm.SelectedNavItem = "History";
            }
            finally
            {
                timerCts.Cancel();
                stopwatch.Stop();
            }
        }
        catch (Exception ex)
        {
            MessageDialog.ShowError(_loc["Dialog.GenerationFailed"],
                $"{_loc["Dialog.ImageGenFailed"]}\n\n{ex.Message}");

            StepText = _loc.Get("Gen.Error.Prefix", ex.Message);
            ProgressText = _loc["Gen.Failed"];
            Progress = 0;

            // Update history record with failure status
            if (history != null)
            {
                try
                {
                    history.Status = CoreTaskStatus.Failed.ToString().ToLower();
                    history.ErrorMessage = ex.Message;
                    history.CompletedAt = DateTime.UtcNow;
                    await _historyService.UpdateAsync(history);
                }
                catch { /* best-effort */ }
            }
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
            // Auto‑match aspect ratio to first reference image
            if (wasEmpty && ReferenceImages.Count > 0)
                AutoMatchAspectRatio(ReferenceImages[0]);
        }
    }

    [RelayCommand]
    private void RemoveReferenceImage(string path)
    {
        ReferenceImages.Remove(path);
    }

    [RelayCommand]
    private void OpenResultFolder()
    {
        if (ResultImages.Count > 0)
        {
            var dir = Path.GetDirectoryName(ResultImages[0]);
            if (dir != null)
                Process.Start("explorer.exe", dir);
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

    public void LoadFromRecord(HistoryRecord record)
    {
        // Select matching model
        var profile = Models.FirstOrDefault(m => m.ModelId == record.ModelType);
        if (profile != null) SelectedModel = profile;

        Prompt = record.Prompt;
        SelectedAspectRatio = record.AspectRatio;

        var qualityMatch = Qualities.FirstOrDefault(q => q == record.Quality);
        if (qualityMatch != null) SelectedQuality = qualityMatch;

        // Restore output count from API request JSON if present
        if (ShowOutputCount && !string.IsNullOrEmpty(record.ApiRequestJson))
        {
            try
            {
                var req = JsonSerializer.Deserialize<ImageGenerationRequest>(record.ApiRequestJson);
                if (req?.N.HasValue == true
                    && req.N.Value >= 1 && req.N.Value <= MaxOutputCount)
                    SelectedOutputCount = req.N.Value;
            }
            catch { }
        }

        // Restore reference images if files still exist
        ReferenceImages.Clear();
        if (!string.IsNullOrEmpty(record.ReferenceImagePaths))
        {
            try
            {
                var paths = System.Text.Json.JsonSerializer.Deserialize<List<string>>(record.ReferenceImagePaths);
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

    public void OnDragEnter() => IsDragging = true;
    public void OnDragLeave() => IsDragging = false;

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
            {
                ReferenceImages.Add(file);
            }
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
        catch
        {
            // Silently ignore — ratio matching is best-effort
        }
    }
}
