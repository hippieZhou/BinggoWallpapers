# Supabase 客户端使用示例

## 快速开始

### 1. 安装 Supabase 客户端库

```bash
dotnet add src/BinggoWallpapers.WinUI/BinggoWallpapers.WinUI.csproj package Supabase
```

### 2. 配置 Supabase 选项

在 `appsettings.json` 中配置（开发环境）：

```json
{
  "SupabaseOptions": {
    "Url": "https://your-project.supabase.co",
    "AnonKey": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "TableName": "wallpapers"
  }
}
```

### 3. 注册服务

在 `ServiceCollectionExtensions.cs` 中：

```csharp
// 注册 Supabase 配置
services.Configure<SupabaseOptions>(
    configuration.GetSection(nameof(SupabaseOptions)));

// 注册 Supabase 客户端服务
services.AddSingleton<ISupabaseClientService, SupabaseClientService>();
```

### 4. 使用服务

在 ViewModel 或 Service 中注入使用：

```csharp
public class HomeViewModel : ObservableObject
{
    private readonly ISupabaseClientService _supabaseService;

    public HomeViewModel(ISupabaseClientService supabaseService)
    {
        _supabaseService = supabaseService;
    }

    public async Task LoadWallpapersAsync()
    {
        if (!_supabaseService.IsConfigured())
        {
            // 提示用户配置 Supabase
            return;
        }

        var client = await _supabaseService.GetClientAsync();
        // 使用客户端查询数据...
    }
}
```

## 完整实现示例

### SupabaseClientService 实现

```csharp
using Microsoft.Extensions.Options;
using Supabase;
using BinggoWallpapers.WinUI.Options;

namespace BinggoWallpapers.WinUI.Services.Impl;

public class SupabaseClientService : ISupabaseClientService
{
    private readonly SupabaseOptions _options;
    private Supabase.Client? _client;
    private readonly ILogger<SupabaseClientService> _logger;

    public SupabaseClientService(
        IOptions<SupabaseOptions> options,
        ILogger<SupabaseClientService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public bool IsConfigured()
    {
        return !string.IsNullOrEmpty(_options.Url) 
            && !string.IsNullOrEmpty(_options.AnonKey);
    }

    public async Task<object> GetClientAsync()
    {
        if (!IsConfigured())
        {
            throw new InvalidOperationException(
                "Supabase 未配置。请在 appsettings.json 中配置 SupabaseOptions，或在应用设置中配置。");
        }

        if (_client == null)
        {
            try
            {
                _client = new Supabase.Client(
                    _options.Url,
                    _options.AnonKey,
                    new SupabaseOptions
                    {
                        AutoRefreshToken = false, // 客户端不需要认证
                        AutoConnectRealtime = false // 不需要实时功能
                    });

                await _client.InitializeAsync();
                _logger.LogInformation("Supabase 客户端初始化成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Supabase 客户端初始化失败");
                throw;
            }
        }

        return _client;
    }
}
```

### 查询壁纸数据示例

```csharp
public class WallpaperService
{
    private readonly ISupabaseClientService _supabaseService;

    public async Task<List<WallpaperEntity>> GetLatestWallpapersAsync(
        string marketCode, 
        int limit = 10)
    {
        var client = await _supabaseService.GetClientAsync() as Supabase.Client;
        
        var response = await client!
            .From<WallpaperEntity>()
            .Where(w => w.MarketCode == marketCode)
            .Order(w => w.ActualDate, Postgrest.Models.Ordering.Descending)
            .Limit(limit)
            .Get();

        return response.Models;
    }

    public async Task<WallpaperEntity?> GetWallpaperByDateAsync(
        string marketCode, 
        DateTime date, 
        string resolutionCode = "FullHD")
    {
        var client = await _supabaseService.GetClientAsync() as Supabase.Client;
        
        var response = await client!
            .From<WallpaperEntity>()
            .Where(w => w.MarketCode == marketCode)
            .Where(w => w.ResolutionCode == resolutionCode)
            .Where(w => w.ActualDate == date.Date)
            .Single();

        return response;
    }
}
```

## 安全注意事项

1. **只使用 Anon Key** - 永远不要在客户端使用 Service Role Key
2. **只读操作** - 客户端只进行查询，不进行写入
3. **RLS 策略** - 确保 Supabase 数据库已正确配置 RLS
4. **错误处理** - 妥善处理网络错误和认证错误

## 参考

- [Supabase C# 客户端文档](https://github.com/supabase/supabase-csharp)
- [SupabaseClientSecurity.md](./SupabaseClientSecurity.md) - 安全配置指南
