using BinggoWallpapers.Core.Services;
using BinggoWallpapers.WinUI.Messages;
using BinggoWallpapers.WinUI.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppNotifications.Builder;
using U5BFA.Libraries;

namespace BinggoWallpapers.WinUI.Views.TrayIcon;

public sealed partial class DefaultTrayIconMeunFlyout : TrayIconMenuFlyout
{
    public DefaultTrayIconMeunFlyoutViewModel ViewModel { get; }
    public DefaultTrayIconMeunFlyout(DefaultTrayIconMeunFlyoutViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    private void OnHome(object sender, RoutedEventArgs e)
    {
        if (App.MainWindow.Visible == false)
        {
            App.MainWindow.Show();
            App.MainWindow.Activate();
        }
    }

    private  void OnExit(object sender, RoutedEventArgs e)
    {
        App.Current.Exit();
    }
}

public partial class DefaultTrayIconMeunFlyoutViewModel(
    IManagementService managementService,
    IMemoryCache memoryCache,
    IAppNotificationService appNotificationService,
    ILogger<DefaultTrayIconMeunFlyoutViewModel> logger) : ObservableRecipient
{
    [RelayCommand(IncludeCancelCommand = true, AllowConcurrentExecutions = false)]
    private async Task OnLoaded(CancellationToken cancellationToken = default)
    {
        await Task.Yield();
    }

    [RelayCommand(IncludeCancelCommand = true, AllowConcurrentExecutions = false)]
    private async Task OnRefresh(CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Run((Func<Task>)(async () =>
            {
                await managementService.RunCollectionAsync(cancellationToken);
                if (memoryCache is MemoryCache cache)
                {
                    cache.Clear();
                }
            }), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "刷新壁纸信息时发生错误");
        }
        finally
        {
            var notification = new AppNotificationBuilder()
                .AddText("所有壁纸信息收集完成！")
                .SetAppLogoOverride(new Uri("ms-appx:///Assets/WindowIcon.ico"), AppNotificationImageCrop.Circle)
                .SetAudioEvent(AppNotificationSoundEvent.Default)
                .SetTimeStamp(DateTime.Now)
                .BuildNotification();

            appNotificationService.Show(notification);
            Messenger.Send(new RefreshWallpapersCompletedMessage());
        }
    }
}
