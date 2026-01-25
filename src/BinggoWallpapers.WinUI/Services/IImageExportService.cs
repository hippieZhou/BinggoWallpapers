// Copyright (c) hippieZhou. All rights reserved.

using Microsoft.Graphics.Canvas;

namespace BinggoWallpapers.WinUI.Services;

/// <summary>
/// 图片导出服务，负责将Canvas内容导出为图片文件
/// </summary>
public interface IImageExportService
{
    /// <summary>
    /// 导出壁纸图片为文件
    /// </summary>
    Task<bool> ExportWallpaperAsync(
        CanvasBitmap wallpaperImage,
        (float contrast, float exposure, float tint, float temperature, float saturation, float blur, float pixelScale) effect,
        float cornerRadius = 0,
        float scaleFactor = 2.0f);
}
