using System.Collections.ObjectModel;
using BinggoWallpapers.Core.DTOs;
using BinggoWallpapers.Core.Services;
using BinggoWallpapers.WinUI.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BinggoWallpapers.WinUI.Views.UserControls; 

public sealed partial class DownloadProgressList : UserControl
{
    public DownloadProgressListViewModel ViewModel { get; } = new();
    public DownloadProgressList()
    {
        InitializeComponent();
        Unloaded += OnUnloaded;
    }

    private void OnUnloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        // 取消事件订阅，防止内存泄漏
        ViewModel.UnsubscribeEvents();
    }

    [GeneratedDependencyProperty]
    public partial object? DownloadProgresses { get; set; }
}

public partial class DownloadProgressListViewModel : ObservableObject
{
    private readonly IDownloadService _downloadService;
    private readonly Dictionary<Guid, DownloadableModel> _downloadModelsIndex = new();

    public ObservableCollection<DownloadableModel> Downloads = [];

    public DownloadProgressListViewModel()
    {
        _downloadService = App.GetService<IDownloadService>();

        // 订阅下载事件
        _downloadService.DownloadProgressUpdated += OnDownloadProgressUpdated;
        _downloadService.DownloadStatusChanged += OnDownloadStatusChanged;
    }

    /// <summary>
    /// 取消事件订阅，防止内存泄漏
    /// </summary>
    public void UnsubscribeEvents()
    {
        _downloadService.DownloadProgressUpdated -= OnDownloadProgressUpdated;
        _downloadService.DownloadStatusChanged -= OnDownloadStatusChanged;
    }

    /// <summary>
    /// 更新或添加下载模型（公共方法，避免代码重复）
    /// </summary>
    /// <param name="downloadInfo">下载信息</param>
    private void UpdateOrAddDownloadModel(DownloadInfoDto downloadInfo)
    {
        // 使用字典索引快速查找，O(1) 时间复杂度
        if (_downloadModelsIndex.TryGetValue(downloadInfo.DownloadId, out var existingModel))
        {
            // 更新现有的 DownloadModel
            existingModel.Update();
        }
        else
        {
            // 创建新的 DownloadModel 并添加到集合和索引中
            var downloadModel = new DownloadableModel(downloadInfo, _downloadService);
            Downloads.Add(downloadModel);
            _downloadModelsIndex[downloadInfo.DownloadId] = downloadModel;
        }
    }

    /// <summary>
    /// 处理下载进度更新事件
    /// </summary>
    private void OnDownloadProgressUpdated(object sender, DownloadProgressEventArgs e)
    {
        // 确保在 UI 线程上更新
        App.MainWindow.DispatcherQueue.TryEnqueue(() =>
        {
            UpdateOrAddDownloadModel(e.DownloadInfo);
        });
    }

    /// <summary>
    /// 处理下载状态变更事件
    /// </summary>
    private void OnDownloadStatusChanged(object sender, DownloadStatusEventArgs e)
    {
        // 确保在 UI 线程上更新
        App.MainWindow.DispatcherQueue.TryEnqueue(() =>
        {
            UpdateOrAddDownloadModel(e.DownloadInfo);
        });
    }

    [RelayCommand]
    private void OnLoaded()
    {
        var downloads = _downloadService.GetAllDownloads();
        foreach (var download in downloads)
        {
            // 使用字典索引快速查找，避免重复添加
            if (_downloadModelsIndex.ContainsKey(download.DownloadId))
            {
                continue;
            }

            var downloadModel = new DownloadableModel(download, _downloadService);
            Downloads.Add(downloadModel);
            _downloadModelsIndex[download.DownloadId] = downloadModel;
        }
    }

    [RelayCommand(IncludeCancelCommand = true, AllowConcurrentExecutions = false)]
    private async Task OnClearDownloadQueue(CancellationToken cancellationToken = default)
    {
        await _downloadService.ClearDownloadQueueAsync(cancellationToken);
        Downloads.Clear();
        _downloadModelsIndex.Clear();
    }
}
