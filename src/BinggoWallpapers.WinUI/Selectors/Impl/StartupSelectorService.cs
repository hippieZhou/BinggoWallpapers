// Copyright (c) hippieZhou. All rights reserved.

using BinggoWallpapers.WinUI.Services;
using Microsoft.Extensions.Logging;
using Windows.ApplicationModel;

namespace BinggoWallpapers.WinUI.Selectors.Impl;

/// <summary>
/// 提供开机自启动服务的实现
/// </summary>
public class StartupSelectorService(
    ILocalSettingsService localSettingsService,
    ILogger<StartupSelectorService> logger) :
    SelectorService(localSettingsService), IStartupSelectorService
{
    private const string StartupTaskId = "BinggoWallpapersStartupTask";
    protected override string SettingsKey => "StartupEnabled";

    /// <inheritdoc/>
    public bool IsEnabled { get; private set; } = false;

    /// <inheritdoc/>
    public async Task InitializeAsync()
    {
        IsEnabled = await ReadFromSettingsAsync(false);

        // 同步实际状态（用户可能在系统设置中更改了）
        await SyncStartupTaskStateAsync();

        logger.LogDebug("开机自启动服务初始化完成，当前状态: {IsEnabled}", IsEnabled);
    }

    /// <inheritdoc/>
    public async Task ToggleAsync(bool value)
    {
        IsEnabled = value;
        await SetRequestedStartupAsync();
        
        // 同步实际状态（SetRequestedStartupAsync 可能会更新 IsEnabled，例如用户拒绝启用）
        await SyncStartupTaskStateAsync();
        
        await SaveInSettingsAsync(IsEnabled);
        logger.LogInformation("开机自启动状态已更改: {IsEnabled}", IsEnabled);
    }

    /// <inheritdoc/>
    public async Task SetRequestedStartupAsync()
    {
        try
        {
            await SetStartupTaskAsync(IsEnabled);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "设置开机自启动状态失败");
            throw;
        }
    }

    /// <summary>
    /// 使用 StartupTask API 设置开机自启动
    /// </summary>
    private async Task SetStartupTaskAsync(bool enable)
    {
        try
        {
            var startupTask = await StartupTask.GetAsync(StartupTaskId);
            if (startupTask is null)
            {
                logger.LogError("无法获取启动任务: {TaskId}", StartupTaskId);
                return;
            }

            if (enable)
            {
                var state = await startupTask.RequestEnableAsync();
                switch (state)
                {
                    case StartupTaskState.Enabled:
                        logger.LogInformation("开机自启动已启用");
                        break;
                    case StartupTaskState.Disabled:
                        logger.LogWarning("开机自启动被用户禁用");
                        break;
                    case StartupTaskState.DisabledByUser:
                        logger.LogWarning("开机自启动被用户在系统设置中禁用");
                        break;
                    case StartupTaskState.DisabledByPolicy:
                        logger.LogWarning("开机自启动被组策略禁用");
                        break;
                    default:
                        logger.LogWarning("开机自启动状态未知: {State}", state);
                        break;
                }
            }
            else
            {
                startupTask.Disable();
                logger.LogInformation("开机自启动已禁用");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "设置 StartupTask 状态失败");
            throw;
        }
    }

    /// <summary>
    /// 同步 StartupTask 的实际状态
    /// </summary>
    private async Task SyncStartupTaskStateAsync()
    {
        try
        {
            var startupTask = await StartupTask.GetAsync(StartupTaskId);
            if (startupTask is null)
            {
                return;
            }

            IsEnabled = startupTask.State == StartupTaskState.Enabled;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "同步 StartupTask 状态失败");
        }
    }
}
