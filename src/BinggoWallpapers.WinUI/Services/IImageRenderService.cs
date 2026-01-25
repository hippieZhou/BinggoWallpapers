// Copyright (c) hippieZhou. All rights reserved.

using Microsoft.Graphics.Canvas;
using Windows.Foundation;

namespace BinggoWallpapers.WinUI.Services;

/// <summary>
/// 图片渲染服务，负责处理壁纸图片的绘制逻辑和效果应用
/// </summary>
public interface IImageRenderService
{
    /// <summary>
    /// 计算用户图片在指定区域内的最佳填充区域
    /// </summary>
    Rect CalculateUserImageRect(Rect targetRect, Size userImageSize, double targetAspectRatio);

    /// <summary>
    /// 绘制用户图片并应用效果
    /// </summary>
    void DrawUserImageOnScreen(CanvasDrawingSession session, CanvasBitmap userImage, Rect targetRect, Rect imageDrawRect,
        (float contrast, float exposure, float tint, float temperature, float saturation, float blur, float pixelScale) effect,
        float cornerRadius = 0);

    /// <summary>
    /// 清空绘制资源
    /// </summary>
    void CleanUp(CanvasDrawingSession session);
}
