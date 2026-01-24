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
        (float left, float top, float right, float bottom) cornerRadius = default,
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
        (float left, float top, float right, float bottom) cornerRadius,
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
                var scaledCornerRadius = (
                    left: cornerRadius.left * scaleFactor,
                    top: cornerRadius.top * scaleFactor,
                    right: cornerRadius.right * scaleFactor,
                    bottom: cornerRadius.bottom * scaleFactor);

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
        (float left, float top, float right, float bottom) cornerRadius)
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

        // 如果有圆角，应用圆角裁剪
        if (cornerRadius.left > 0 || cornerRadius.top > 0 || cornerRadius.right > 0 || cornerRadius.bottom > 0)
        {
            var roundedRect = CreateRoundedRectangleGeometry(
                session.Device,
                imageRect,
                cornerRadius.left,
                cornerRadius.top,
                cornerRadius.right,
                cornerRadius.bottom);

            using (session.CreateLayer(1.0f, roundedRect))
            {
                session.DrawImage(
                    combinedEffect,  // 要绘制的效果
                    imageRect,       // 输出区域（绘制的位置和大小）
                    destRect        // 输入区域（原图范围）
                    );
            }
        }
        else
        {
            session.DrawImage(
                combinedEffect,  // 要绘制的效果
                imageRect,       // 输出区域（绘制的位置和大小）
                destRect        // 输入区域（原图范围）
                );
        }
    }

    /// <summary>
    /// 创建具有不同圆角半径的圆角矩形几何路径
    /// </summary>
    private static CanvasGeometry CreateRoundedRectangleGeometry(
        ICanvasResourceCreator resourceCreator,
        Rect rect,
        float topLeft,
        float topRight,
        float bottomRight,
        float bottomLeft)
    {
        var pathBuilder = new CanvasPathBuilder(resourceCreator);
        var x = (float)rect.X;
        var y = (float)rect.Y;
        var width = (float)rect.Width;
        var height = (float)rect.Height;

        // 限制圆角半径不超过矩形尺寸的一半
        topLeft = Math.Min(topLeft, Math.Min(width, height) / 2);
        topRight = Math.Min(topRight, Math.Min(width, height) / 2);
        bottomRight = Math.Min(bottomRight, Math.Min(width, height) / 2);
        bottomLeft = Math.Min(bottomLeft, Math.Min(width, height) / 2);

        // 从左上角开始（如果左上角有圆角，从圆角起点开始）
        if (topLeft > 0)
        {
            pathBuilder.BeginFigure(x + topLeft, y);
        }
        else
        {
            pathBuilder.BeginFigure(x, y);
        }

        // 上边（从左到右）
        if (topRight > 0)
        {
            pathBuilder.AddLine(x + width - topRight, y);
            // 右上角圆弧：从 (x + width - topRight, y) 到 (x + width, y + topRight)
            // 圆弧中心在 (x + width - topRight, y + topRight)，半径 topRight
            pathBuilder.AddArc(
                new System.Numerics.Vector2(x + width - topRight, y + topRight),
                topRight,
                topRight,
                0,
                CanvasSweepDirection.Clockwise,
                CanvasArcSize.Small);
        }
        else
        {
            pathBuilder.AddLine(x + width, y);
        }

        // 右边（从上到下）
        if (bottomRight > 0)
        {
            pathBuilder.AddLine(x + width, y + height - bottomRight);
            // 右下角圆弧：从 (x + width, y + height - bottomRight) 到 (x + width - bottomRight, y + height)
            pathBuilder.AddArc(
                new System.Numerics.Vector2(x + width - bottomRight, y + height - bottomRight),
                bottomRight,
                bottomRight,
                0,
                CanvasSweepDirection.Clockwise,
                CanvasArcSize.Small);
        }
        else
        {
            pathBuilder.AddLine(x + width, y + height);
        }

        // 下边（从右到左）
        if (bottomLeft > 0)
        {
            pathBuilder.AddLine(x + bottomLeft, y + height);
            // 左下角圆弧：从 (x + bottomLeft, y + height) 到 (x, y + height - bottomLeft)
            pathBuilder.AddArc(
                new System.Numerics.Vector2(x + bottomLeft, y + height - bottomLeft),
                bottomLeft,
                bottomLeft,
                0,
                CanvasSweepDirection.Clockwise,
                CanvasArcSize.Small);
        }
        else
        {
            pathBuilder.AddLine(x, y + height);
        }

        // 左边（从下到上）
        if (topLeft > 0)
        {
            pathBuilder.AddLine(x, y + topLeft);
            // 左上角圆弧：从 (x, y + topLeft) 到起始点 (x + topLeft, y)
            pathBuilder.AddArc(
                new System.Numerics.Vector2(x + topLeft, y + topLeft),
                topLeft,
                topLeft,
                0,
                CanvasSweepDirection.Clockwise,
                CanvasArcSize.Small);
        }
        else
        {
            pathBuilder.AddLine(x, y);
        }

        pathBuilder.EndFigure(CanvasFigureLoop.Closed);
        return CanvasGeometry.CreatePath(pathBuilder);
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
