// Copyright (c) hippieZhou. All rights reserved.

using System.Numerics;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.UI;
using Windows.Foundation;

namespace BinggoWallpapers.WinUI.Services.Impl;

public class ImageRenderService : IImageRenderService
{
    public Rect CalculateUserImageRect(Rect targetRect, Size userImageSize, double targetAspectRatio)
    {
        var userImageAspectRatio = userImageSize.Width / userImageSize.Height;

        if (userImageAspectRatio > targetAspectRatio)
        {
            // 用户图片比目标区域更宽，按高度填充，裁剪左右
            var drawHeight = targetRect.Height;
            var drawWidth = drawHeight * userImageAspectRatio;
            var offsetX = (targetRect.Width - drawWidth) / 2;

            return new Rect(
                targetRect.X + offsetX,
                targetRect.Y,
                drawWidth,
                drawHeight
            );
        }
        else
        {
            // 用户图片比目标区域更高，按宽度填充，裁剪上下
            var drawWidth = targetRect.Width;
            var drawHeight = drawWidth / userImageAspectRatio;
            var offsetY = (targetRect.Height - drawHeight) / 2;

            return new Rect(
                targetRect.X,
                targetRect.Y + offsetY,
                drawWidth,
                drawHeight
            );
        }
    }

    public void DrawUserImageOnScreen(CanvasDrawingSession session, CanvasBitmap userImage, Rect screenRect, Rect imageDrawRect,
        (float contrast, float exposure, float tint, float temperature, float saturation, float blur, float pixelScale) effect,
        float cornerRadius = 0)
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
                                    Source = userImage,
                                    Scale = new Vector2(effect.pixelScale, effect.pixelScale), // 放大倍数
                                    InterpolationMode = CanvasImageInterpolation.NearestNeighbor,
                                    BorderMode = EffectBorderMode.Hard
                                },
                                Scale = new Vector2(1f / effect.pixelScale, 1f / effect.pixelScale), // 缩小倍数
                                InterpolationMode = CanvasImageInterpolation.NearestNeighbor,
                                BorderMode = EffectBorderMode.Hard
                            },
                            BlurAmount = effect.blur, // 模糊半径
                            BorderMode = EffectBorderMode.Hard
                        },
                        Saturation = effect.saturation // 0 = 灰度, 1 = 原图, >1 = 增强
                    },
                    Temperature = effect.temperature, // -1 (冷) 到 1 (暖)
                    Tint = effect.tint // -1 (绿) 到 1 (紫)
                },
                Exposure = effect.exposure // -1 (暗) 到 1 (亮)
            },
            Contrast = effect.contrast // 0 (灰度) 到 1 (原图), >1 (增强)
        };

        var destRect = new Rect(0, 0, userImage.SizeInPixels.Width, userImage.SizeInPixels.Height);

        // 如果有圆角，创建圆角几何路径并裁剪图片的四个角
        if (cornerRadius > 0)
        {
            var width = (float)screenRect.Width;
            var height = (float)screenRect.Height;

            // 限制圆角半径不超过矩形尺寸的一半
            var radius = Math.Min(cornerRadius, Math.Min(width, height) / 2);

            // 使用 Win2D 内置的 CreateRoundedRectangle 方法创建圆角矩形
            var roundedRect = CanvasGeometry.CreateRoundedRectangle(
                session.Device,
                (float)screenRect.X,
                (float)screenRect.Y,
                width,
                height,
                radius,
                radius);

            // 使用圆角路径创建裁剪层，将图片的四个角裁剪为相同的圆角
            using (session.CreateLayer(1.0f, roundedRect))
            {
                session.DrawImage(
                    combinedEffect,  // 要绘制的效果
                    imageDrawRect,   // 输出区域（屏幕上绘制的位置和大小）
                    destRect);       // 输入区域（原图范围）
            }
        }
        else
        {
            // 没有圆角，使用矩形裁剪
            using (session.CreateLayer(1.0f, screenRect))
            {
                session.DrawImage(
                    combinedEffect,  // 要绘制的效果
                    imageDrawRect,   // 输出区域（屏幕上绘制的位置和大小）
                    destRect);       // 输入区域（原图范围）
            }
        }
    }

    public void CleanUp(CanvasDrawingSession session)
    {
        session.Clear(Colors.Transparent);
    }
}
