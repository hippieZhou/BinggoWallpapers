using BinggoWallpapers.Core.DTOs;
using BinggoWallpapers.Core.Http.Enums;
using BinggoWallpapers.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;

namespace BinggoWallpapers.WinUI.Models;

public partial class DownloadableModel : ObservableObject
{
    private readonly DownloadInfoDto _downloadInfo;
    private readonly IDownloadService _downloadService;
    private readonly ILogger<DownloadableModel> _logger;
    
    public DownloadableModel(DownloadInfoDto downloadInfo, IDownloadService downloadService)
    {
        _downloadInfo = downloadInfo;
        _downloadService = downloadService;
        _logger = App.GetService<ILogger<DownloadableModel>>();
    }

    public Guid DownloadId => _downloadInfo.DownloadId;
    public string FileName => _downloadInfo.FileName;
    public string ResolutionName => _downloadInfo.Resolution.Name;
    public WallpaperInfoDto? ModelDetails => _downloadInfo.Wallpaper;

    [ObservableProperty]
    public partial double Progress { get; set; }

    [ObservableProperty]
    public partial double DownloadSpeed { get; set; }

    [ObservableProperty]
    public partial TimeSpan EstimatedTimeRemaining { get; set; }

    [ObservableProperty]
    public partial long TotalBytes { get; set; }

    [ObservableProperty]
    public partial DownloadStatus Status { get; set; } = DownloadStatus.Waiting;

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    public bool IsDownloadEnabled => true;

    public Visibility DownloadStatusProgressVisibility(DownloadStatus status)
    {
        return status is DownloadStatus.InProgress or DownloadStatus.Waiting ? Visibility.Visible : Visibility.Collapsed;
    }

    public Visibility VisibleWhenDownloaded(DownloadStatus status)
    {
        return status == DownloadStatus.Completed ? Visibility.Visible : Visibility.Collapsed;
    }

    public Visibility VisibleWhenCanceled(DownloadStatus status)
    {
        return status == DownloadStatus.Canceled ? Visibility.Visible : Visibility.Collapsed;
    }

    public Visibility VisibleWhenDownloading(DownloadStatus status)
    {
        return status is DownloadStatus.InProgress or DownloadStatus.Waiting ? Visibility.Visible : Visibility.Collapsed;
    }

    public Visibility VisibleWhenFailed(DownloadStatus status)
    {
        return status == DownloadStatus.Failed ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>
    /// 更新下载信息（仅在值变化时更新，减少不必要的 UI 刷新）
    /// </summary>
    public void Update()
    {
        // 仅在值变化时更新属性，减少不必要的 UI 刷新
        if (Status != _downloadInfo.Status)
        {
            Status = _downloadInfo.Status;
        }

        if (Math.Abs(Progress - _downloadInfo.ProgressPercentage) > 0.01)
        {
            Progress = _downloadInfo.ProgressPercentage;
        }

        if (Math.Abs(DownloadSpeed - _downloadInfo.DownloadSpeed) > 0.01)
        {
            DownloadSpeed = _downloadInfo.DownloadSpeed;
        }

        if (EstimatedTimeRemaining != _downloadInfo.EstimatedTimeRemaining)
        {
            EstimatedTimeRemaining = _downloadInfo.EstimatedTimeRemaining;
        }

        if (TotalBytes != _downloadInfo.TotalBytes)
        {
            TotalBytes = _downloadInfo.TotalBytes;
        }

        if (ErrorMessage != _downloadInfo.ErrorMessage)
        {
            ErrorMessage = _downloadInfo.ErrorMessage;
        }
    }

    public string StatusToText(DownloadStatus status)
    {
        return status switch
        {
            DownloadStatus.Waiting => "Waiting..",
            DownloadStatus.InProgress => "Downloading..",
            DownloadStatus.Completed => "Downloaded",
            DownloadStatus.Canceled => "Canceled",
            DownloadStatus.Failed => "Failed", // 统一大小写格式
            _ => string.Empty,
        };
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task OnCancel(CancellationToken cancellationToken = default)
    {
        try
        {
            await _downloadService.CancelDownloadAsync(DownloadId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取消下载任务失败: {DownloadId}", DownloadId);
        }
    }

    /// <summary>
    /// 重试下载任务
    /// 注意：重试会创建新的下载任务，ViewModel 会通过事件自动更新模型
    /// </summary>
    [RelayCommand(IncludeCancelCommand = true)]
    private async Task OnRetry(CancellationToken cancellationToken = default)
    {
        try
        {
            // 检查是否可以重试（只有失败或取消的任务才能重试）
            if (Status is not (DownloadStatus.Failed or DownloadStatus.Canceled))
            {
                _logger.LogWarning("下载任务状态不允许重试: {DownloadId}, 状态: {Status}", DownloadId, Status);
                return;
            }

            _logger.LogInformation("开始重试下载任务: {DownloadId}", DownloadId);

            // 重新创建下载任务
            // 注意：新任务会有新的 DownloadId，ViewModel 会通过 DownloadStatusChanged 事件
            // 自动创建新的 DownloadableModel，旧的模型会被新模型替换
            var newDownloadId = await _downloadService.DownloadAsync(
                _downloadInfo.Wallpaper,
                _downloadInfo.Resolution,
                cancellationToken);

            _logger.LogInformation("重试下载任务创建成功: 原ID {OldId} -> 新ID {NewId}", DownloadId, newDownloadId);
            
            // 注意：由于新任务有新的 DownloadId，当前模型会保留旧状态
            // ViewModel 会通过事件处理添加新模型，用户界面会显示新的下载任务
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "重试下载任务失败: {DownloadId}", DownloadId);
        }
    }

    [RelayCommand]
    private void OnOpen()
    {
        try
        {
            var filePath = _downloadInfo?.FilePath;
            if (File.Exists(filePath))
            {
                // 使用系统默认浏览器打开原图链接
                var argument = $"/select, \"{filePath}\"";
                System.Diagnostics.Process.Start("explorer.exe", argument);
                _logger.LogInformation("打开文件所在目录: {FilePath}", filePath);
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "打开原图链接失败: {DownloadId}", DownloadId);
        }
    }
}
