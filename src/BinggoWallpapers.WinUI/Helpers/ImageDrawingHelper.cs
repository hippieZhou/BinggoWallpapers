// Copyright (c) hippieZhou. All rights reserved.

using System.Numerics;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Geometry;
using Windows.Foundation;

namespace BinggoWallpapers.WinUI.Helpers;

/// <summary>
/// 图片绘制辅助类，提供图片效果应用和圆角裁剪的公共方法
/// </summary>
public static class ImageDrawingHelper
{
    /// <summary>
    /// 创建组合的图像效果链
    /// </summary>
    /// <param name="sourceImage">源图片</param>
    /// <param name="effect">效果参数</param>
    /// <returns>组合后的效果对象</returns>
    public static ICanvasImage CreateCombinedEffect(
        CanvasBitmap sourceImage,
        (float contrast, float exposure, float tint, float temperature, float saturation, float blur, float pixelScale) effect)
    {
        return new ContrastEffect
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
                                    Source = sourceImage,
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
    }

    /// <summary>
    /// 绘制图片并应用效果和圆角裁剪
    /// </summary>
    /// <param name="session">绘制会话</param>
    /// <param name="sourceImage">源图片</param>
    /// <param name="effect">效果参数</param>
    /// <param name="targetRect">目标矩形区域（裁剪区域）</param>
    /// <param name="imageDrawRect">图片绘制区域</param>
    /// <param name="cornerRadius">圆角半径（四个角使用相同的值）</param>
    public static void DrawImageWithEffects(
        CanvasDrawingSession session,
        CanvasBitmap sourceImage,
        (float contrast, float exposure, float tint, float temperature, float saturation, float blur, float pixelScale) effect,
        Rect targetRect,
        Rect imageDrawRect,
        float cornerRadius = 0)
    {
        // 先绘制阴影效果（在图片下方）
        //DrawShadow(session, targetRect, cornerRadius);

        // 创建组合效果
        var combinedEffect = CreateCombinedEffect(sourceImage, effect);

        // 计算源图片的完整范围
        var sourceRect = new Rect(0, 0, sourceImage.SizeInPixels.Width, sourceImage.SizeInPixels.Height);

        // 如果有圆角，创建圆角几何路径并裁剪图片的四个角
        if (cornerRadius > 0)
        {
            var width = (float)targetRect.Width;
            var height = (float)targetRect.Height;

            // 限制圆角半径不超过矩形尺寸的一半
            var radius = Math.Min(cornerRadius, Math.Min(width, height) / 2);

            // 使用 Win2D 内置的 CreateRoundedRectangle 方法创建圆角矩形
            var roundedRect = CanvasGeometry.CreateRoundedRectangle(
                session.Device,
                (float)targetRect.X,
                (float)targetRect.Y,
                width,
                height,
                radius,
                radius);

            // 使用圆角路径创建裁剪层，将图片的四个角裁剪为相同的圆角
            using (session.CreateLayer(1.0f, roundedRect))
            {
                session.DrawImage(
                    combinedEffect,  // 要绘制的效果
                    imageDrawRect,   // 输出区域（绘制的位置和大小）
                    sourceRect);     // 输入区域（原图范围）
            }
        }
        else
        {
            // 没有圆角，使用矩形裁剪
            using (session.CreateLayer(1.0f, targetRect))
            {
                session.DrawImage(
                    combinedEffect,  // 要绘制的效果
                    imageDrawRect,   // 输出区域（绘制的位置和大小）
                    sourceRect);     // 输入区域（原图范围）
            }
        }
    }

    /// <summary>
    /// 绘制图片的阴影效果（美观的阴影）
    /// </summary>
    /// <param name="session">绘制会话</param>
    /// <param name="targetRect">目标矩形区域</param>
    /// <param name="cornerRadius">圆角半径（用于匹配图片的圆角形状）</param>
    private static void DrawShadow(
        CanvasDrawingSession session,
        Rect targetRect,
        float cornerRadius)
    {
        // 阴影参数：偏移量、模糊半径、透明度（经过优化的美观参数）
        const float ShadowOffsetX = 0f;      // 水平偏移（0 = 无偏移，向下投影更自然）
        const float ShadowOffsetY = 8f;       // 垂直偏移（向下偏移，创建浮起效果）
        const float ShadowBlurRadius = 24f;  // 模糊半径（较大的值创建更柔和、自然的阴影）
        const float ShadowOpacity = 0.25f;    // 阴影透明度（0.25 = 25%，柔和不突兀）

        var width = (float)targetRect.Width;
        var height = (float)targetRect.Height;

        // 创建包含阴影形状的渲染目标
        var shadowPadding = ShadowBlurRadius * 1.5f;
        var shadowRenderTargetWidth = width + shadowPadding * 2;
        var shadowRenderTargetHeight = height + shadowPadding * 2;

        using var shadowRenderTarget = new CanvasRenderTarget(
            session.Device,
            shadowRenderTargetWidth,
            shadowRenderTargetHeight,
            session.Dpi);

        // 在离屏目标上绘制阴影形状
        using (var shadowSession = shadowRenderTarget.CreateDrawingSession())
        {
            shadowSession.Clear(Windows.UI.Color.FromArgb(0, 0, 0, 0));

            // 计算阴影形状在离屏目标中的位置（居中，但考虑偏移）
            var shadowShapeRect = new Rect(
                shadowPadding - ShadowOffsetX,
                shadowPadding - ShadowOffsetY,
                width,
                height);

            // 创建阴影几何路径（在离屏目标坐标系中）
            CanvasGeometry shadowShapeGeometry;
            if (cornerRadius > 0)
            {
                var radius = Math.Min(cornerRadius, Math.Min(width, height) / 2);
                shadowShapeGeometry = CanvasGeometry.CreateRoundedRectangle(
                    shadowSession.Device,
                    (float)shadowShapeRect.X,
                    (float)shadowShapeRect.Y,
                    (float)shadowShapeRect.Width,
                    (float)shadowShapeRect.Height,
                    radius,
                    radius);
            }
            else
            {
                shadowShapeGeometry = CanvasGeometry.CreateRectangle(
                    shadowSession.Device,
                    (float)shadowShapeRect.X,
                    (float)shadowShapeRect.Y,
                    (float)shadowShapeRect.Width,
                    (float)shadowShapeRect.Height);
            }

            // 绘制半透明的黑色阴影形状
            shadowSession.FillGeometry(
                shadowShapeGeometry,
                Windows.UI.Color.FromArgb(
                    (byte)(255 * ShadowOpacity),
                    0,
                    0,
                    0));
        }

        // 使用 Win2D 的 ShadowEffect 创建阴影效果（更简洁高效）
        var shadowEffect = new ShadowEffect
        {
            Source = shadowRenderTarget,
            BlurAmount = ShadowBlurRadius,
            Optimization = EffectOptimization.Speed
        };

        // 计算阴影在主画布上的绘制位置
        var shadowDrawX = targetRect.X + ShadowOffsetX - shadowPadding;
        var shadowDrawY = targetRect.Y + ShadowOffsetY - shadowPadding;
        var shadowDrawRect = new Rect(
            shadowDrawX,
            shadowDrawY,
            shadowRenderTargetWidth,
            shadowRenderTargetHeight);

        // 源矩形（整个阴影渲染目标）
        var shadowSourceRect = new Rect(0, 0, shadowRenderTargetWidth, shadowRenderTargetHeight);

        // 绘制阴影效果到主画布
        session.DrawImage(shadowEffect, shadowDrawRect, shadowSourceRect);
    }
}
