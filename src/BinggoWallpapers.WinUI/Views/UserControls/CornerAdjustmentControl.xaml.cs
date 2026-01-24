using System.Globalization;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BinggoWallpapers.WinUI.Views.UserControls;

public sealed partial class CornerAdjustmentControl : UserControl
{
    public CultureInfo Culture { get; }
    public CornerAdjustmentControl()
    {
        InitializeComponent();
        Culture = CultureInfo.CurrentCulture;
    }

    [GeneratedDependencyProperty(DefaultValue = 0D)]
    public partial double Left { get; set; }

    [GeneratedDependencyProperty(DefaultValue = 0D)]
    public partial double Top { get; set; }

    [GeneratedDependencyProperty(DefaultValue = 0D)]
    public partial double Right { get; set; }

    [GeneratedDependencyProperty(DefaultValue = 0D)]
    public partial double Bottom { get; set; }
}
