using BinggoWallpapers.Core.DTOs;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BinggoWallpapers.WinUI.Views.UserControls;

public sealed partial class WallpaperInfoControl : UserControl
{
    public WallpaperInfoControl()
    {
        InitializeComponent();
    }

    [GeneratedDependencyProperty]
    public partial WallpaperInfoDto? Model { get; set; }

    [GeneratedDependencyProperty]
    public partial TransitionHelper? Helper { get; set; }
}
