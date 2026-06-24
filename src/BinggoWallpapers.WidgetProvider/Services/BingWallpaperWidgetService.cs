// Copyright (c) hippieZhou. All rights reserved.

using System.Text.Json;
using BinggoWallpapers.Core.DTOs;
using BinggoWallpapers.Core.Services;
using Microsoft.Extensions.Logging;

namespace BinggoWallpapers.WidgetProvider.Services;

/// <summary>
/// 必应壁纸 Widget 服务
/// </summary>
public sealed class BingWallpaperWidgetService
{
    private readonly IManagementService _managementService;
    private readonly ILogger<BingWallpaperWidgetService> _logger;

    /// <summary>
    /// 初始化 <see cref="BingWallpaperWidgetService"/> 的新实例
    /// </summary>
    /// <param name="managementService">壁纸管理服务</param>
    /// <param name="logger">日志记录器</param>
    public BingWallpaperWidgetService(
        IManagementService managementService,
        ILogger<BingWallpaperWidgetService> logger)
    {
        _managementService = managementService;
        _logger = logger;
    }

    /// <summary>
    /// 获取今日壁纸信息
    /// </summary>
    /// <param name="marketCode">市场代码（默认 CN）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>壁纸信息，失败时返回 null</returns>
    public async Task<WallpaperInfoDto?> GetTodayWallpaperAsync(
        string marketCode = "CN",
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("正在获取 {MarketCode} 地区的今日壁纸...", marketCode);

            var markets = await _managementService.GetSupportedMarketCodesAsync(cancellationToken);
            var market = markets.FirstOrDefault(m => m.Code.Equals(marketCode, StringComparison.OrdinalIgnoreCase));

            if (market == null)
            {
                _logger.LogWarning("未找到市场代码: {MarketCode}，使用默认市场 CN", marketCode);
                market = markets.FirstOrDefault(m => m.Code.Equals("CN", StringComparison.OrdinalIgnoreCase));
            }

            if (market == null)
            {
                _logger.LogError("无法获取市场信息");
                return null;
            }

            var wallpaper = await _managementService.GetLatestAsync(market, cancellationToken);
            _logger.LogInformation("成功获取今日壁纸: {Title}", wallpaper.Title);

            return wallpaper;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取今日壁纸失败");
            return null;
        }
    }

    /// <summary>
    /// 构建 Widget 数据负载
    /// </summary>
    /// <param name="wallpaper">壁纸信息</param>
    /// <param name="errorMessage">错误消息（如果有）</param>
    /// <returns>JSON 数据负载字符串</returns>
    public static string BuildDataPayload(WallpaperInfoDto? wallpaper, string? errorMessage = null)
    {
        if (wallpaper == null)
        {
            return $$"""{"title":"必应每日壁纸","copyright":"","caption":"","backgroundImageUrl":"","errorMessage":{{(errorMessage != null ? $"\"{EscapeJson(errorMessage)}\"" : "null")}}}""";
        }

        var title = EscapeJson(wallpaper.Title);
        var copyright = EscapeJson(wallpaper.Copyright);
        var caption = EscapeJson(wallpaper.Caption ?? string.Empty);
        var backgroundImageUrl = EscapeJson(wallpaper.Url ?? string.Empty);
        var errorJson = errorMessage != null ? $",\"errorMessage\":\"{EscapeJson(errorMessage)}\"" : string.Empty;

        return $$"""{"title":"{{title}}","copyright":"{{copyright}}","caption":"{{caption}}","backgroundImageUrl":"{{backgroundImageUrl}}"{{errorJson}}}""";
    }

    /// <summary>
    /// 转义 JSON 字符串
    /// </summary>
    private static string EscapeJson(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }
}
