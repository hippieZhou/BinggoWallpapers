using BinggoWallpapers.Core.DTOs;
using BinggoWallpapers.Core.Http.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;

namespace BinggoWallpapers.WinUI.Models;

public partial class DownloadableModel : ObservableObject
{
    [ObservableProperty]
    public partial WallpaperInfoDto? Model { get; set; }

    [ObservableProperty]
    public partial float Progress { get; set; }

    [ObservableProperty]
    public partial bool CanDownload { get; set; }

    [ObservableProperty]
    public partial DownloadStatus Status { get; set; } = DownloadStatus.Pending;

    [ObservableProperty]
    public partial string? VerificationFailureMessage { get; set; }

    public bool IsDownloadEnabled => true;

    public Visibility DownloadStatusProgressVisibility(DownloadStatus status)
    {
        return Visibility.Visible;
    }

    public Visibility VisibleWhenDownloaded(DownloadStatus status)
    {
        return Visibility.Visible;
    }

    public Visibility VisibleWhenCanceled(DownloadStatus status)
    {
        return Visibility.Visible;
    }

    public Visibility VisibleWhenDownloading(DownloadStatus status)
    {
        return Visibility.Visible;
    }

    public Visibility VisibleWhenVerificationFailed(DownloadStatus status)
    {
        return Visibility.Visible;
    }
}
