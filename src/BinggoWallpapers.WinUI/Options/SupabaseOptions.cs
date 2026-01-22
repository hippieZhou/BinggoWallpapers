// Copyright (c) hippieZhou. All rights reserved.

namespace BinggoWallpapers.WinUI.Options;

/// <summary>
/// Supabase 配置选项
/// </summary>
public class SupabaseOptions
{
    /// <summary>
    /// Supabase 项目 URL
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Supabase Anon Key (公开 API Key，用于客户端)
    /// 注意：不要使用 Service Role Key，它应该只在服务器端使用
    /// </summary>
    public string AnonKey { get; set; } = string.Empty;

    /// <summary>
    /// 表名（默认为 wallpapers）
    /// </summary>
    public string TableName { get; set; } = "wallpapers";
}
