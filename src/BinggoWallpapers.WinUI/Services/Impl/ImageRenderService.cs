// Copyright (c) hippieZhou. All rights reserved.

using BinggoWallpapers.WinUI.Helpers;
using Microsoft.Graphics.Canvas;
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
        // 使用 Helper 方法绘制图片并应用效果和圆角裁剪
        ImageDrawingHelper.DrawImageWithEffects(session, userImage, effect, screenRect, imageDrawRect, cornerRadius);
    }

    public void CleanUp(CanvasDrawingSession session)
    {
        session.Clear(Colors.Transparent);
    }
}
