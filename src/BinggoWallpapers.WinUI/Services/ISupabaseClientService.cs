// Copyright (c) hippieZhou. All rights reserved.

namespace BinggoWallpapers.WinUI.Services;

/// <summary>
/// Supabase 客户端服务接口
/// </summary>
public interface ISupabaseClientService
{
    /// <summary>
    /// 获取 Supabase 客户端实例
    /// </summary>
    /// <returns>Supabase 客户端</returns>
    Task<object> GetClientAsync();

    /// <summary>
    /// 检查 Supabase 配置是否有效
    /// </summary>
    /// <returns>如果配置有效返回 true</returns>
    bool IsConfigured();
}
