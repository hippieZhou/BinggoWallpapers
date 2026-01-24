// Copyright (c) hippieZhou. All rights reserved.

using System.Windows.Input;
using BinggoWallpapers.WinUI.Services;
using CommunityToolkit.WinUI;
using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace BinggoWallpapers.WinUI.Views.UserControls;

public sealed partial class MockupCanvasControl : UserControl
{
    private readonly IImageRenderService _renderService;
    private readonly ILogger<MockupCanvasControl> _logger;

    public MockupCanvasControl()
    {
        InitializeComponent();
        _renderService = App.GetService<IImageRenderService>();
        _logger = App.GetService<ILogger<MockupCanvasControl>>();
    }

    public CanvasControl GetCanvasControl() => Canvas;

    #region DependencyProperties

    [GeneratedDependencyProperty]
    public partial ICommand? CreateResourcesCommand { get; set; }

    [GeneratedDependencyProperty]
    public partial CanvasBitmap? WallpaperImage { get; set; }

    [GeneratedDependencyProperty]
    public partial float ExposureAmount { get; set; }

    [GeneratedDependencyProperty]
    public partial float TemperatureAmount { get; set; }

    [GeneratedDependencyProperty]
    public partial float TintAmount { get; set; }

    [GeneratedDependencyProperty]
    public partial float ContrastAmount { get; set; }

    [GeneratedDependencyProperty]
    public partial float SaturationAmount { get; set; }

    [GeneratedDependencyProperty]
    public partial float BlurAmount { get; set; }

    [GeneratedDependencyProperty]
    public partial float Pixelation { get; set; }

    [GeneratedDependencyProperty]
    public partial bool IsInitialized { get; set; }

    [GeneratedDependencyProperty]
    public partial bool ShowScreenBorder { get; set; }

    [GeneratedDependencyProperty]
    public partial double LeftCornerRadius { get; set; }

    [GeneratedDependencyProperty]
    public partial double TopCornerRadius { get; set; }

    [GeneratedDependencyProperty]
    public partial double RightCornerRadius { get; set; }

    [GeneratedDependencyProperty]
    public partial double BottomCornerRadius { get; set; }

    #endregion

    #region OnPropertyChanged

    partial void OnIsInitializedPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        PR.Visibility = e.NewValue is true ? Visibility.Collapsed : Visibility.Visible;
        Canvas.Invalidate();
    }

    partial void OnExposureAmountPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        Canvas.Invalidate();
    }

    partial void OnTemperatureAmountPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        Canvas.Invalidate();
    }

    partial void OnTintAmountPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        Canvas.Invalidate();
    }

    partial void OnContrastAmountPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        Canvas.Invalidate();
    }

    partial void OnSaturationAmountPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        Canvas.Invalidate();
    }

    partial void OnBlurAmountPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        Canvas.Invalidate();
    }

    partial void OnPixelationPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        Canvas.Invalidate();
    }

    partial void OnLeftCornerRadiusPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        Canvas.Invalidate();
    }

    partial void OnTopCornerRadiusPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        Canvas.Invalidate();
    }

    partial void OnRightCornerRadiusPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        Canvas.Invalidate();
    }

    partial void OnBottomCornerRadiusPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        Canvas.Invalidate();
    }

    #endregion

    private void Canvas_CreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs args)
    {
        CreateResourcesCommand?.Execute(sender);
        Canvas.Invalidate();
    }

    private void Canvas_Draw(CanvasControl sender, CanvasDrawEventArgs args)
    {
        if (IsInitialized is false || WallpaperImage is null)
        {
            return;
        }

        var session = args.DrawingSession;
        var canvasSize = sender.Size;

        try
        {
            // 计算壁纸图片在画布中的显示区域（保持宽高比，居中显示）
            var imageAspectRatio = WallpaperImage.Size.Width / WallpaperImage.Size.Height;
            var canvasAspectRatio = canvasSize.Width / canvasSize.Height;

            Rect imageRect;
            if (imageAspectRatio > canvasAspectRatio)
            {
                // 图片更宽，按宽度填充
                var drawWidth = canvasSize.Width;
                var drawHeight = drawWidth / imageAspectRatio;
                var offsetY = (canvasSize.Height - drawHeight) / 2;
                imageRect = new Rect(0, offsetY, drawWidth, drawHeight);
            }
            else
            {
                // 图片更高，按高度填充
                var drawHeight = canvasSize.Height;
                var drawWidth = drawHeight * imageAspectRatio;
                var offsetX = (canvasSize.Width - drawWidth) / 2;
                imageRect = new Rect(offsetX, 0, drawWidth, drawHeight);
            }

            // 应用效果并绘制壁纸
            _renderService.DrawUserImageOnScreen(session, WallpaperImage, imageRect, imageRect,
                (contrast: ContrastAmount,
                exposure: ExposureAmount,
                tint: TintAmount,
                temperature: TemperatureAmount,
                saturation: SaturationAmount,
                blur: BlurAmount,
                pixelScale: Pixelation),
                (left: (float)LeftCornerRadius,
                top: (float)TopCornerRadius,
                right: (float)RightCornerRadius,
                bottom: (float)BottomCornerRadius));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"绘制壁纸画布时发生错误:{ex.Message}");
        }
    }
}
