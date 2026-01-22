# Supabase 客户端安全配置指南

## ⚠️ 重要安全原则

**客户端应用永远不应该使用 Service Role Key！**

Service Role Key 拥有完全权限，可以绕过所有安全策略。如果暴露在客户端代码中，任何人都可以：
- 删除所有数据
- 修改任何记录
- 访问敏感信息

## 正确的做法

### 1. 使用 Anon Key（公开 API Key）

Supabase 提供了两种 API Key：
- **Anon Key** ✅ 用于客户端（公开，配合 RLS 策略）
- **Service Role Key** ❌ 仅用于服务器端（完全权限，必须保密）

### 2. 配置 Row Level Security (RLS)

RLS 策略限制客户端只能访问允许的数据，即使 Anon Key 被公开，也无法访问未授权的数据。

## 配置步骤

### 步骤 1: 获取 Anon Key

1. 在 Supabase Dashboard 中，点击 **Settings** > **API**
2. 复制 **anon public** key（不是 service_role key！）

### 步骤 2: 配置应用

#### 方式 A: 使用 appsettings.json（推荐用于开发）

在 `appsettings.json` 中添加配置：

```json
{
  "SupabaseOptions": {
    "Url": "https://your-project.supabase.co",
    "AnonKey": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "TableName": "wallpapers"
  }
}
```

**注意**:
- ⚠️ `appsettings.json` 会被包含在应用中，Anon Key 会被公开
- ✅ 这是安全的，因为 Anon Key 本身就是设计为公开的
- ✅ RLS 策略会保护数据安全

#### 方式 B: 使用用户设置（推荐用于生产）

允许用户在应用设置中配置 Supabase URL 和 Key：

```csharp
// 在设置页面让用户输入
await localSettingsService.SaveSettingAsync("SupabaseUrl", userInputUrl);
await localSettingsService.SaveSettingAsync("SupabaseAnonKey", userInputKey);
```

#### 方式 C: 使用环境变量（仅用于开发）

```bash
# 设置环境变量
export SUPABASE_URL="https://your-project.supabase.co"
export SUPABASE_ANON_KEY="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
```

### 步骤 3: 确保 RLS 策略正确配置

在 Supabase Dashboard > SQL Editor 中执行以下 SQL，确保 RLS 策略正确：

```sql
-- 确保 RLS 已启用
ALTER TABLE wallpapers ENABLE ROW LEVEL SECURITY;

-- 允许所有人读取（因为壁纸数据是公开的）
CREATE POLICY "Allow public read access" ON wallpapers
    FOR SELECT
    USING (true);

-- 禁止客户端写入（写入应该通过服务器端 API）
CREATE POLICY "Deny client insert" ON wallpapers
    FOR INSERT
    WITH CHECK (false);

CREATE POLICY "Deny client update" ON wallpapers
    FOR UPDATE
    USING (false);

CREATE POLICY "Deny client delete" ON wallpapers
    FOR DELETE
    USING (false);
```

### 步骤 4: 注册配置和服务

在 `App.xaml.cs` 或 `ServiceCollectionExtensions.cs` 中：

```csharp
// 注册配置
services.Configure<SupabaseOptions>(
    configuration.GetSection(nameof(SupabaseOptions)));

// 注册 Supabase 客户端服务
services.AddSingleton<ISupabaseClientService, SupabaseClientService>();
```

## 代码示例

### 创建 Supabase 客户端服务

```csharp
using Microsoft.Extensions.Options;
using Supabase;

namespace BinggoWallpapers.WinUI.Services;

public interface ISupabaseClientService
{
    Task<Supabase.Client> GetClientAsync();
}

public class SupabaseClientService : ISupabaseClientService
{
    private readonly SupabaseOptions _options;
    private Supabase.Client? _client;

    public SupabaseClientService(IOptions<SupabaseOptions> options)
    {
        _options = options.Value;
    }

    public async Task<Supabase.Client> GetClientAsync()
    {
        if (_client == null)
        {
            _client = new Supabase.Client(
                _options.Url,
                _options.AnonKey,
                new SupabaseOptions
                {
                    AutoRefreshToken = true,
                    AutoConnectRealtime = false
                });

            await _client.InitializeAsync();
        }

        return _client;
    }
}
```

### 使用客户端查询数据

```csharp
var client = await supabaseService.GetClientAsync();

// 查询最新壁纸（只读操作）
var response = await client
    .From<WallpaperEntity>()
    .Where(w => w.MarketCode == "zh-CN")
    .Order(w => w.ActualDate, Postgrest.Models.Ordering.Descending)
    .Limit(10)
    .Get();

var wallpapers = response.Models;
```

## 安全最佳实践

### ✅ 应该做的

1. **使用 Anon Key** - 这是设计为公开的
2. **启用 RLS** - 限制数据访问权限
3. **只读操作** - 客户端只进行查询，不进行写入
4. **验证输入** - 验证用户输入，防止 SQL 注入
5. **使用参数化查询** - Supabase 客户端自动处理

### ❌ 不应该做的

1. **不要使用 Service Role Key** - 永远不要在客户端使用
2. **不要禁用 RLS** - 这会暴露所有数据
3. **不要允许客户端写入** - 写入操作应该通过服务器端 API
4. **不要硬编码密钥** - 使用配置文件或用户设置
5. **不要信任客户端输入** - 始终在服务器端验证

## 密钥存储位置对比

| 存储方式 | 安全性 | 适用场景 | 说明 |
|---------|--------|---------|------|
| `appsettings.json` | ⚠️ 公开 | 开发/测试 | Anon Key 可以公开，配合 RLS |
| 用户设置 | ✅ 较高 | 生产环境 | 用户自行配置，更灵活 |
| 环境变量 | ⚠️ 公开 | 开发环境 | 仅用于本地开发 |
| 代码硬编码 | ❌ 不安全 | 不推荐 | 难以维护，不灵活 |

## 常见问题

### Q: Anon Key 被公开了怎么办？

**A**: 这是正常的！Anon Key 本身就是设计为公开的。安全性依赖于：
- RLS 策略限制数据访问
- 只允许必要的操作（如只读）
- 服务器端 API 处理敏感操作

### Q: 如果用户修改了 Anon Key 怎么办？

**A**:
- 如果使用错误的 Key，Supabase 会返回认证错误
- RLS 策略仍然有效，即使用户使用其他项目的 Key
- 考虑添加应用级别的 API 密钥验证

### Q: 如何保护敏感数据？

**A**:
- 敏感数据不应该存储在客户端可访问的表中
- 使用服务器端 API 处理敏感操作
- 使用 Supabase Auth 进行用户认证和授权

### Q: 可以在客户端写入数据吗？

**A**:
- 技术上可以，但不推荐
- 如果必须写入，应该：
  1. 使用 RLS 策略限制写入权限
  2. 验证数据完整性
  3. 考虑使用服务器端 API 作为中间层

## 参考资源

- [Supabase RLS 文档](https://supabase.com/docs/guides/auth/row-level-security)
- [Supabase 客户端库](https://github.com/supabase/supabase-csharp)
- [Supabase 安全最佳实践](https://supabase.com/docs/guides/auth/security)
