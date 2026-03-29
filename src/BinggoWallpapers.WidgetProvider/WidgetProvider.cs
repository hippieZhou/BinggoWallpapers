// Copyright (c) hippieZhou. All rights reserved.

using System.Runtime.InteropServices;
using BinggoWallpapers.WidgetProvider.Models;
using BinggoWallpapers.WidgetProvider.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Windows.Widgets.Providers;

namespace BinggoWallpapers.WidgetProvider;

/// <summary>
/// Widget Provider 实现
/// </summary>
[ComVisible(true)]
[ComDefaultInterface(typeof(IWidgetProvider))]
[Guid("A1B2C3D4-E5F6-4A7B-8C9D-0E1F2A3B4C5D")]
internal sealed class WidgetProvider : IWidgetProvider
{
    private const string DefaultState = "{}";
    private const string RefreshError = "无法刷新壁纸，请稍后重试。";

    private readonly Lock _sync = new();
    private readonly Dictionary<string, CompactWidgetInfo> _runningWidgets = new(StringComparer.Ordinal);
    private readonly BingWallpaperWidgetService _wallpaperService;
    private readonly ILogger<WidgetProvider> _logger;
    private static readonly ManualResetEvent EmptyWidgetListEvent = new(false);

    private static readonly Lazy<string> MainTemplate = new(() => LoadTemplate("Templates/BingWallpaperTemplate.json"));
    private static readonly Lazy<string> LoadingTemplate = new(() => LoadTemplate("Templates/LoadingTemplate.json"));

    /// <summary>
    /// 获取空 Widget 列表事件
    /// </summary>
    public static WaitHandle GetEmptyWidgetListEvent() => EmptyWidgetListEvent;

    /// <summary>
    /// 初始化 <see cref="WidgetProvider"/> 的新实例
    /// </summary>
    /// <param name="wallpaperService">壁纸服务</param>
    /// <param name="logger">日志记录器</param>
    public WidgetProvider(
        BingWallpaperWidgetService wallpaperService,
        ILogger<WidgetProvider> logger)
    {
        _wallpaperService = wallpaperService;
        _logger = logger;
        RecoverRunningWidgets();
    }

    /// <inheritdoc/>
    public void CreateWidget(WidgetContext widgetContext)
    {
        _logger.LogInformation("创建 Widget: {WidgetId}, DefinitionId: {DefinitionId}", widgetContext.Id, widgetContext.DefinitionId);

        var widget = GetOrCreateWidget(widgetContext, null);
        SendLoadingWidget(widget);
        _ = RefreshAndUpdateAsync(widget.WidgetId);
    }

    /// <inheritdoc/>
    public void DeleteWidget(string widgetId, string customState)
    {
        _logger.LogInformation("删除 Widget: {WidgetId}", widgetId);

        lock (_sync)
        {
            _runningWidgets.Remove(widgetId);

            if (_runningWidgets.Count == 0)
            {
                EmptyWidgetListEvent.Set();
            }
        }
    }

    /// <inheritdoc/>
    public void OnActionInvoked(WidgetActionInvokedArgs actionInvokedArgs)
    {
        if (!string.Equals(actionInvokedArgs.Verb, "refresh", StringComparison.Ordinal))
        {
            return;
        }

        if (!HasWidget(actionInvokedArgs.WidgetContext.Id))
        {
            return;
        }

        _logger.LogInformation("刷新 Widget: {WidgetId}", actionInvokedArgs.WidgetContext.Id);
        _ = RefreshAndUpdateAsync(actionInvokedArgs.WidgetContext.Id);
    }

    /// <inheritdoc/>
    public void OnWidgetContextChanged(WidgetContextChangedArgs contextChangedArgs)
    {
        var widget = GetWidgetSnapshot(contextChangedArgs.WidgetContext.Id);
        if (widget == null)
        {
            return;
        }

        _logger.LogInformation("Widget 上下文变更: {WidgetId}", contextChangedArgs.WidgetContext.Id);
        SendFactWidget(widget, null);
    }

    /// <inheritdoc/>
    public void Activate(WidgetContext widgetContext)
    {
        _logger.LogInformation("激活 Widget: {WidgetId}", widgetContext.Id);

        var widget = GetOrCreateWidget(widgetContext, null);
        SendFactWidget(widget, null);
        _ = RefreshAndUpdateAsync(widget.WidgetId);
    }

    /// <inheritdoc/>
    public void Deactivate(string widgetId)
    {
        _logger.LogInformation("停用 Widget: {WidgetId}", widgetId);
        // 可以在这里暂停后台轮询等操作
    }

    private void RecoverRunningWidgets()
    {
        try
        {
            var infos = WidgetManager.GetDefault().GetWidgetInfos();
            if (infos == null)
            {
                return;
            }

            lock (_sync)
            {
                foreach (var info in infos)
                {
                    var context = info.WidgetContext;
                    _runningWidgets[context.Id] = new CompactWidgetInfo
                    {
                        WidgetId = context.Id,
                        DefinitionId = context.DefinitionId,
                        CustomState = string.IsNullOrWhiteSpace(info.CustomState) ? DefaultState : info.CustomState
                    };
                }

                if (_runningWidgets.Count == 0)
                {
                    EmptyWidgetListEvent.Set();
                }
                else
                {
                    EmptyWidgetListEvent.Reset();
                }
            }

            _logger.LogInformation("恢复了 {Count} 个 Widget", _runningWidgets.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "恢复 Widget 失败");
        }
    }

    private CompactWidgetInfo GetOrCreateWidget(WidgetContext context, string? customState)
    {
        lock (_sync)
        {
            if (_runningWidgets.TryGetValue(context.Id, out var existing))
            {
                if (!string.IsNullOrWhiteSpace(customState))
                {
                    existing.CustomState = customState;
                }

                return Clone(existing);
            }

            var created = new CompactWidgetInfo
            {
                WidgetId = context.Id,
                DefinitionId = context.DefinitionId,
                CustomState = string.IsNullOrWhiteSpace(customState) ? DefaultState : customState
            };

            _runningWidgets[created.WidgetId] = created;
            EmptyWidgetListEvent.Reset();
            return Clone(created);
        }
    }

    private bool HasWidget(string widgetId)
    {
        lock (_sync)
        {
            return _runningWidgets.ContainsKey(widgetId);
        }
    }

    private CompactWidgetInfo? GetWidgetSnapshot(string widgetId)
    {
        lock (_sync)
        {
            return _runningWidgets.TryGetValue(widgetId, out var widget) ? Clone(widget) : null;
        }
    }

    private void SendLoadingWidget(CompactWidgetInfo widget)
    {
        var update = new WidgetUpdateRequestOptions(widget.WidgetId)
        {
            Template = LoadingTemplate.Value,
            Data = "{}",
            CustomState = widget.CustomState
        };

        WidgetManager.GetDefault().UpdateWidget(update);
    }

    private void SendFactWidget(CompactWidgetInfo widget, string? errorMessage)
    {
        var stateJson = widget.CustomState;
        var wallpaper = ParseState(stateJson);

        var update = new WidgetUpdateRequestOptions(widget.WidgetId)
        {
            Template = MainTemplate.Value,
            Data = BingWallpaperWidgetService.BuildDataPayload(wallpaper, errorMessage),
            CustomState = stateJson
        };

        WidgetManager.GetDefault().UpdateWidget(update);
    }

    private async Task RefreshAndUpdateAsync(string widgetId)
    {
        Core.DTOs.WallpaperInfoDto? fetchedWallpaper = null;
        string? errorMessage = null;

        try
        {
            fetchedWallpaper = await _wallpaperService.GetTodayWallpaperAsync().ConfigureAwait(false);
            if (fetchedWallpaper == null)
            {
                errorMessage = RefreshError;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "刷新 Widget 失败: {WidgetId}", widgetId);
            errorMessage = RefreshError;
        }

        CompactWidgetInfo? snapshot;
        lock (_sync)
        {
            if (!_runningWidgets.TryGetValue(widgetId, out var widget))
            {
                return; // Widget 已被删除
            }

            if (fetchedWallpaper != null)
            {
                widget.CustomState = SerializeState(fetchedWallpaper);
            }

            snapshot = Clone(widget);
        }

        SendFactWidget(snapshot, errorMessage);
    }

    private static string SerializeState(Core.DTOs.WallpaperInfoDto wallpaper)
    {
        return System.Text.Json.JsonSerializer.Serialize(new
        {
            title = wallpaper.Title,
            copyright = wallpaper.Copyright,
            caption = wallpaper.Caption,
            url = wallpaper.Url
        });
    }

    private static Core.DTOs.WallpaperInfoDto? ParseState(string? stateJson)
    {
        if (string.IsNullOrWhiteSpace(stateJson) || stateJson == "{}")
        {
            return null;
        }

        try
        {
            var state = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(stateJson);
            if (state == null)
            {
                return null;
            }

            // 这里简化处理，实际应该从数据库获取完整信息
            return null;
        }
        catch
        {
            return null;
        }
    }

    private static string LoadTemplate(string relativePath)
    {
        var normalizedPath = relativePath.Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(AppContext.BaseDirectory, normalizedPath);
        return File.ReadAllText(fullPath);
    }

    private static CompactWidgetInfo Clone(CompactWidgetInfo widget)
    {
        return new CompactWidgetInfo
        {
            WidgetId = widget.WidgetId,
            DefinitionId = widget.DefinitionId,
            CustomState = widget.CustomState
        };
    }
}
