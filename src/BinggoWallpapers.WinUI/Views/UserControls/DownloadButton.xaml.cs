using BinggoWallpapers.WinUI.Models;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BinggoWallpapers.WinUI.Views.UserControls;

public sealed partial class DownloadButton : UserControl
{
    public DownloadButton()
    {
        InitializeComponent();
    }

    private async void ManageDownloadsClicked(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = AppInfo.DefaultPicturesPath;
            await Launcher.LaunchFolderPathAsync(path);
        }
        catch (Exception ex)
        {
            App.GetService<ILogger<DownloadButton>>().LogError(ex, ex.Message);
        }
    }
}
