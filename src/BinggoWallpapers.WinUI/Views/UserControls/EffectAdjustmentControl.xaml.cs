using System.Globalization;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BinggoWallpapers.WinUI.Views.UserControls;

public sealed partial class EffectAdjustmentControl : UserControl
{
    public CultureInfo Culture { get; }

    public EffectAdjustmentControl()
    {
        InitializeComponent();
        Culture = CultureInfo.CurrentCulture;
    }

    [GeneratedDependencyProperty(DefaultValue = 0F)]
    public partial float Exposure { get; set; }

    [GeneratedDependencyProperty(DefaultValue = 0F)]
    public partial float Temperature { get; set; } 

    [GeneratedDependencyProperty(DefaultValue = 0F)]
    public partial float Tint { get; set; } 

    [GeneratedDependencyProperty(DefaultValue = 0F)]
    public partial float Contrast { get; set; } 

    [GeneratedDependencyProperty(DefaultValue = 1F)]
    public partial float Saturation { get; set; } 

    [GeneratedDependencyProperty(DefaultValue = 0F)]
    public partial float Blur { get; set; }

    [GeneratedDependencyProperty(DefaultValue = 0F)]
    public partial float PixelScale { get; set; } 
}
