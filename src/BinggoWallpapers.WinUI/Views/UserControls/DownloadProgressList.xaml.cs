using System.Collections.ObjectModel;
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
    }

    [GeneratedDependencyProperty]
    public partial object? DownloadProgresses { get; set; }
}

public partial class DownloadProgressListViewModel : ObservableObject
{
    private readonly IDownloadService _downloadService;

    public ObservableCollection<DownloadableModel> Downloads = [];

    public DownloadProgressListViewModel()
    {
        _downloadService = App.GetService<IDownloadService>();

        // 订阅下载事件
        _downloadService.DownloadProgressUpdated += OnDownloadProgressUpdated;
        _downloadService.DownloadStatusChanged += OnDownloadStatusChanged;
    }

    /// <summary>
    /// 处理下载进度更新事件
    /// </summary>
    private void OnDownloadProgressUpdated(object sender, DownloadProgressEventArgs e)
    {
        // 确保在 UI 线程上更新
        App.MainWindow.DispatcherQueue.TryEnqueue(() =>
        {
            var downloadModel = Downloads.FirstOrDefault(d => d.DownloadId == e.DownloadInfo.DownloadId);
            if (downloadModel is not null)
            {
                // 更新现有的 DownloadModel
                downloadModel.Update();
            }
            else
            {
                // 创建新的 DownloadModel 并添加到集合中
                downloadModel = new DownloadableModel(e.DownloadInfo);
                Downloads.Add(downloadModel);
            }
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
            var downloadModel = Downloads.FirstOrDefault(d => d.DownloadId == e.DownloadInfo.DownloadId);
            if (downloadModel is not null)
            {
                // 更新现有的 DownloadModel
                downloadModel.Update();
            }
            else
            {
                // 创建新的 DownloadModel 并添加到集合中
                downloadModel = new DownloadableModel(e.DownloadInfo);
                Downloads.Add(downloadModel);
            }
        });
    }

    [RelayCommand]
    private void OnLoaded()
    {
        var downloads = _downloadService.GetAllDownloads();
        foreach (var download in downloads)
        {
            if (Downloads.Any(d => d.DownloadId == download.DownloadId))
            {
                continue;
            }

            var downloadModel = new DownloadableModel(download);
            Downloads.Add(downloadModel);
        }
    }

    [RelayCommand(IncludeCancelCommand = true, AllowConcurrentExecutions = false)]
    private async Task OnClearDownloadQueue(CancellationToken cancellationToken = default)
    {
        await _downloadService.ClearDownloadQueueAsync(cancellationToken);
        Downloads.Clear();
    }
}
