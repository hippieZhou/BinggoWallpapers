// Copyright (c) hippieZhou. All rights reserved.

namespace BinggoWallpapers.WinUI.Selectors;

/// <summary>
/// 提供开机自启动功能的接口
/// </summary>
public interface IStartupSelectorService
{
    /// <summary>
    /// 获取是否已启用开机自启动
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// 初始化服务，从设置中读取自启动状态
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// 切换开机自启动状态
    /// </summary>
    /// <param name="value">是否启用</param>
    Task ToggleAsync(bool value);

    /// <summary>
    /// 设置请求的开机自启动状态
    /// </summary>
    Task SetRequestedStartupAsync();
}
