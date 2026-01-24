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
        (float left, float top, float right, float bottom) cornerRadius = default)
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

        // 如果有圆角，创建圆角几何路径
        if (cornerRadius.left > 0 || cornerRadius.top > 0 || cornerRadius.right > 0 || cornerRadius.bottom > 0)
        {
            var roundedRect = CreateRoundedRectangleGeometry(
                session.Device,
                screenRect,
                cornerRadius.left,    // topLeft
                cornerRadius.top,     // topRight
                cornerRadius.right,   // bottomRight
                cornerRadius.bottom); // bottomLeft

            // 使用圆角路径创建裁剪层
            using (session.CreateLayer(1.0f, roundedRect))
            {
                session.DrawImage(
                    combinedEffect,  // 要绘制的效果
                    imageDrawRect,   // 输出区域（屏幕上绘制的位置和大小）
                    destRect        // 输入区域（原图范围）
                    );
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
                    destRect        // 输入区域（原图范围）
                    );
            }
        }
    }

    /// <summary>
    /// 创建具有不同圆角半径的圆角矩形几何路径（类似 WinUI3 的 CornerRadius）
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

        // 限制圆角半径不超过矩形尺寸的一半（类似 WinUI3 的行为）
        topLeft = Math.Min(topLeft, Math.Min(width, height) / 2);
        topRight = Math.Min(topRight, Math.Min(width, height) / 2);
        bottomRight = Math.Min(bottomRight, Math.Min(width, height) / 2);
        bottomLeft = Math.Min(bottomLeft, Math.Min(width, height) / 2);

        // 从左上角开始
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
            // 右上角圆弧：AddArc 需要先指定下一个点，然后画圆弧
            // 下一个点是 (x + width, y + topRight)，圆弧中心在 (x + width - topRight, y + topRight)
            pathBuilder.AddLine(x + width, y + topRight);
            pathBuilder.AddArc(
                new Vector2(x + width - topRight, y + topRight),
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
            pathBuilder.AddLine(x + width - bottomRight, y + height);
            pathBuilder.AddArc(
                new Vector2(x + width - bottomRight, y + height - bottomRight),
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
            pathBuilder.AddLine(x, y + height - bottomLeft);
            pathBuilder.AddArc(
                new Vector2(x + bottomLeft, y + height - bottomLeft),
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
            pathBuilder.AddLine(x + topLeft, y);
            pathBuilder.AddArc(
                new Vector2(x + topLeft, y + topLeft),
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

    public void CleanUp(CanvasDrawingSession session)
    {
        session.Clear(Colors.Transparent);
    }
}
