// Copyright (c) hippieZhou. All rights reserved.

using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using WinRT.Interop;

namespace BinggoWallpapers.WinUI.Services.Impl;

public class ImageExportService(ILogger<ImageExportService> logger) : IImageExportService
{
    public async Task<bool> ExportWallpaperAsync(
        CanvasBitmap wallpaperImage,
        (float contrast, float exposure, float tint, float temperature, float saturation, float blur, float pixelScale) effect,
        float scaleFactor = 2.0f)
    {
        if (wallpaperImage == null)
        {
            logger.LogWarning("壁纸图片为空，无法导出");
            return false;
        }

        try
        {
            var picker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.PicturesLibrary,
                DefaultFileExtension = ".png",
                SuggestedFileName = $"{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            picker.FileTypeChoices.Add("PNG图片", new[] { ".png" });
            picker.FileTypeChoices.Add("JPEG图片", new[] { ".jpg", ".jpeg" });

            // 获取窗口句柄
            var hwnd = App.MainWindow.GetWindowHandle();
            InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSaveFileAsync();
            if (file == null)
            {
                return false;
            }

            return await ExportWallpaperToFileAsync(wallpaperImage, file, effect, scaleFactor);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"导出失败: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> ExportWallpaperToFileAsync(
        CanvasBitmap wallpaperImage,
        StorageFile file,
        (float contrast, float exposure, float tint, float temperature, float saturation, float blur, float pixelScale) effect,
        float scaleFactor)
    {
        try
        {
            var imageSize = wallpaperImage.Size;
            var highResWidth = (float)(imageSize.Width * scaleFactor);
            var highResHeight = (float)(imageSize.Height * scaleFactor);

            // 创建离屏渲染目标
            using var renderTarget = new CanvasRenderTarget(
                wallpaperImage.Device,
                highResWidth,
                highResHeight,
                wallpaperImage.Dpi * scaleFactor);

            // 在渲染目标上绘制壁纸并应用效果
            using (var session = renderTarget.CreateDrawingSession())
            {
                // 清除背景
                session.Clear(Windows.UI.Color.FromArgb(255, 0, 0, 0)); // 黑色背景

                // 应用缩放变换
                session.Transform = System.Numerics.Matrix3x2.CreateScale(scaleFactor);

                // 绘制壁纸并应用效果
                DrawWallpaperWithEffects(session, wallpaperImage, effect);
            }

            // 导出到文件
            using var stream = await file.OpenAsync(FileAccessMode.ReadWrite);
            await SaveRenderTargetToStreamAsync(renderTarget, stream, file.FileType);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"导出到文件失败: {ex.Message}");
            return false;
        }
    }

    private static void DrawWallpaperWithEffects(
        CanvasDrawingSession session,
        CanvasBitmap wallpaperImage,
        (float contrast, float exposure, float tint, float temperature, float saturation, float blur, float pixelScale) effect)
    {
        var combinedEffect = new ContrastEffect
        {
            Name = "ContrastEffect",
            Source = new ExposureEffect
            {
                Name = "ExposureEffect",
                Source = new TemperatureAndTintEffect
                {
                    Name = "TemperatureAndTintEffect",
                    Source = new SaturationEffect
                    {
                        Name = "SaturationEffect",
                        Source = new GaussianBlurEffect
                        {
                            Name = "GaussianBlurEffect",
                            Source = new ScaleEffect
                            {
                                Name = "ScaleDown",
                                Source = new ScaleEffect
                                {
                                    Name = "ScaleUp",
                                    Source = wallpaperImage,
                                    Scale = new System.Numerics.Vector2(effect.pixelScale, effect.pixelScale),
                                    InterpolationMode = CanvasImageInterpolation.NearestNeighbor,
                                    BorderMode = EffectBorderMode.Hard
                                },
                                Scale = new System.Numerics.Vector2(1f / effect.pixelScale, 1f / effect.pixelScale),
                                InterpolationMode = CanvasImageInterpolation.NearestNeighbor,
                                BorderMode = EffectBorderMode.Hard
                            },
                            BlurAmount = effect.blur,
                            BorderMode = EffectBorderMode.Hard
                        },
                        Saturation = effect.saturation
                    },
                    Temperature = effect.temperature,
                    Tint = effect.tint
                },
                Exposure = effect.exposure
            },
            Contrast = effect.contrast
        };

        session.DrawImage(combinedEffect);
    }

    private static async Task SaveRenderTargetToStreamAsync(CanvasRenderTarget renderTarget, IRandomAccessStream stream, string fileType)
    {
        var format = fileType.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => BitmapEncoder.JpegEncoderId,
            ".png" => BitmapEncoder.PngEncoderId,
            _ => BitmapEncoder.PngEncoderId
        };

        // 获取像素数据
        var pixelBytes = renderTarget.GetPixelBytes();

        // 创建编码器
        var encoder = await BitmapEncoder.CreateAsync(format, stream);

        // 设置像素数据
        encoder.SetPixelData(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied,
            renderTarget.SizeInPixels.Width,
            renderTarget.SizeInPixels.Height,
            renderTarget.Dpi,
            renderTarget.Dpi,
            pixelBytes);

        // 如果是JPEG，设置插值模式以提高质量
        if (format == BitmapEncoder.JpegEncoderId)
        {
            encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Fant;
            // 注意：JPEG质量设置在某些WinUI版本中可能不可用，使用默认质量
        }

        await encoder.FlushAsync();
    }
}
