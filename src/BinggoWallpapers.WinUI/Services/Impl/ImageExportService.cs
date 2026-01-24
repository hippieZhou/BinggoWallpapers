// Copyright (c) hippieZhou. All rights reserved.

using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Geometry;
using Windows.Foundation;
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
        float cornerRadius = 0,
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

            return await ExportWallpaperToFileAsync(wallpaperImage, file, effect, cornerRadius, scaleFactor);
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
        float cornerRadius,
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
                // 清除背景（透明背景，以便圆角效果可见）
                session.Clear(Windows.UI.Color.FromArgb(0, 0, 0, 0));

                // 应用缩放变换
                session.Transform = System.Numerics.Matrix3x2.CreateScale(scaleFactor);

                var imageRect = new Rect(0, 0, wallpaperImage.Size.Width, wallpaperImage.Size.Height);
                var scaledCornerRadius = cornerRadius * scaleFactor;

                // 绘制壁纸并应用效果和圆角
                DrawWallpaperWithEffects(session, wallpaperImage, effect, imageRect, scaledCornerRadius);
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
        (float contrast, float exposure, float tint, float temperature, float saturation, float blur, float pixelScale) effect,
        Rect imageRect,
        float cornerRadius)
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

        var destRect = new Rect(0, 0, wallpaperImage.SizeInPixels.Width, wallpaperImage.SizeInPixels.Height);

        // 如果有圆角，创建圆角几何路径并裁剪图片的四个角
        if (cornerRadius > 0)
        {
            var width = (float)imageRect.Width;
            var height = (float)imageRect.Height;

            // 限制圆角半径不超过矩形尺寸的一半
            var radius = Math.Min(cornerRadius, Math.Min(width, height) / 2);

            // 使用 Win2D 内置的 CreateRoundedRectangle 方法创建圆角矩形
            var roundedRect = CanvasGeometry.CreateRoundedRectangle(
                session.Device,
                (float)imageRect.X,
                (float)imageRect.Y,
                width,
                height,
                radius,
                radius);

            // 使用圆角路径创建裁剪层，将图片的四个角裁剪为相同的圆角
            using (session.CreateLayer(1.0f, roundedRect))
            {
                session.DrawImage(
                    combinedEffect,  // 要绘制的效果
                    imageRect,       // 输出区域（绘制的位置和大小）
                    destRect);       // 输入区域（原图范围）
            }
        }
        else
        {
            // 没有圆角，直接绘制
            session.DrawImage(
                combinedEffect,  // 要绘制的效果
                imageRect,       // 输出区域（绘制的位置和大小）
                destRect);       // 输入区域（原图范围）
        }
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
