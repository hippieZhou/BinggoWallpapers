using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BinggoWallpapers.WinUI.Views.UserControls;

public sealed partial class DownloadProgressList : UserControl
{
    public DownloadProgressList()
    {
        InitializeComponent();
    }

    [GeneratedDependencyProperty]
    public partial object? DownloadProgresses { get; set; }
}
