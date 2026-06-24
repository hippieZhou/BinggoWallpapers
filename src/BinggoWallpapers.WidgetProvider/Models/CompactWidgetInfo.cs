// Copyright (c) hippieZhou. All rights reserved.

namespace BinggoWallpapers.WidgetProvider.Models;

/// <summary>
/// Widget 信息模型
/// </summary>
public sealed class CompactWidgetInfo
{
    /// <summary>
    /// Widget ID
    /// </summary>
    public string WidgetId { get; set; } = string.Empty;

    /// <summary>
    /// Widget 定义 ID
    /// </summary>
    public string DefinitionId { get; set; } = string.Empty;

    /// <summary>
    /// 自定义状态（JSON 字符串）
    /// </summary>
    public string CustomState { get; set; } = string.Empty;
}
