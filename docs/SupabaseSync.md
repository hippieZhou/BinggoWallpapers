# Supabase 数据同步指南

本文档说明如何使用 GitHub Actions 将 Bing 壁纸数据同步到 Supabase 数据库。

## 前置要求

1. **Supabase 账户和项目**
   - 访问 [Supabase](https://supabase.com/) 创建账户
   - 创建新项目或使用现有项目

2. **GitHub Secrets 配置**
   - 在 GitHub 仓库设置中添加以下 Secrets：
     - `SUPABASE_URL`: Supabase 项目 URL（格式：`https://xxxxx.supabase.co`）
     - `SUPABASE_SERVICE_ROLE_KEY`: Supabase Service Role Key（在项目设置 > API 中获取）

## 设置步骤

### 1. 创建数据库表

在 Supabase Dashboard > SQL Editor 中执行 `scripts/supabase_schema.sql` 脚本：

```sql
-- 执行 scripts/supabase_schema.sql 中的 SQL 语句
```

这将创建 `wallpapers` 表及其索引和策略。

### 2. 配置 GitHub Secrets

1. 进入 GitHub 仓库
2. 点击 **Settings** > **Secrets and variables** > **Actions**
3. 添加以下 Secrets：

   | Secret 名称 | 说明 | 获取位置 |
   |------------|------|---------|
   | `SUPABASE_URL` | Supabase 项目 URL | Supabase Dashboard > Settings > API > Project URL |
   | `SUPABASE_SERVICE_ROLE_KEY` | Service Role Key | Supabase Dashboard > Settings > API > service_role key |

### 3. 触发同步

同步会在以下情况自动触发：

- **定时触发**: 每天 UTC 01:30（在收集壁纸后）
- **收集完成后**: 当 `Collect Bing Wallpapers` workflow 成功完成后
- **手动触发**: 在 GitHub Actions 页面手动运行 `Sync Wallpapers to Supabase` workflow

## 数据同步说明

### 数据转换

- 每个 JSON 文件会被转换为多条数据库记录（每个分辨率一条）
- 使用 `hash` + `market_code` + `resolution_code` + `actual_date` 作为唯一标识
- 如果记录已存在，会使用 `upsert` 更新

### 数据表结构

```sql
wallpapers (
    id BIGSERIAL PRIMARY KEY,
    hash VARCHAR(255) NOT NULL,
    actual_date TIMESTAMPTZ NOT NULL,
    market_code VARCHAR(20) NOT NULL,
    resolution_code VARCHAR(20) NOT NULL,
    info_json JSONB NOT NULL,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE (hash, market_code, resolution_code, actual_date)
)
```

### 查询示例

在 Supabase Dashboard > SQL Editor 中可以使用以下查询：

```sql
-- 查询最新的壁纸（按市场）
SELECT 
    market_code,
    actual_date,
    info_json->>'title' as title,
    info_json->>'copyright' as copyright,
    resolution_code
FROM wallpapers
WHERE market_code = 'zh-CN'
ORDER BY actual_date DESC
LIMIT 10;

-- 查询特定日期的所有分辨率
SELECT 
    market_code,
    resolution_code,
    info_json->'imageResolutions'->0->>'url' as image_url
FROM wallpapers
WHERE actual_date = '2026-01-20'
AND market_code = 'zh-CN';

-- 统计各市场的壁纸数量
SELECT 
    market_code,
    COUNT(*) as count
FROM wallpapers
GROUP BY market_code
ORDER BY count DESC;
```

## 本地测试

你也可以在本地运行同步脚本进行测试：

```bash
# 安装 Python 依赖
pip install requests

# 设置环境变量
export SUPABASE_URL="https://your-project.supabase.co"
export SUPABASE_SERVICE_ROLE_KEY="your-service-role-key"

# 运行同步脚本
python3 scripts/sync_to_supabase.py
```

## 故障排除

### 1. 同步失败：认证错误

**错误**: `401 Unauthorized`

**解决方案**:
- 检查 `SUPABASE_URL` 和 `SUPABASE_SERVICE_ROLE_KEY` 是否正确
- 确保使用的是 `service_role` key 而不是 `anon` key

### 2. 同步失败：表不存在

**错误**: `relation "wallpapers" does not exist`

**解决方案**:
- 在 Supabase Dashboard > SQL Editor 中执行 `scripts/supabase_schema.sql`

### 3. 同步失败：权限错误

**错误**: `permission denied`

**解决方案**:
- 检查 RLS (Row Level Security) 策略是否正确配置
- 确保 Service Role Key 有写入权限

### 4. 数据重复

**解决方案**:
- 脚本使用 `upsert` 操作，重复数据会自动更新
- 检查唯一约束是否正确设置

## 性能优化

- 脚本使用批量插入（每批 100 条记录）
- 数据库已创建必要的索引
- 对于大量数据，建议分批同步或使用 Supabase 的批量导入功能

## API 使用

同步到 Supabase 后，你可以通过 Supabase REST API 访问数据：

```bash
# 获取最新壁纸
curl "https://your-project.supabase.co/rest/v1/wallpapers?market_code=eq.zh-CN&order=actual_date.desc&limit=10" \
  -H "apikey: YOUR_ANON_KEY" \
  -H "Authorization: Bearer YOUR_ANON_KEY"

# 使用 PostgREST 查询语法
curl "https://your-project.supabase.co/rest/v1/wallpapers?select=market_code,actual_date,info_json&market_code=eq.zh-CN" \
  -H "apikey: YOUR_ANON_KEY"
```

## 相关文件

- `.github/workflows/sync-to-supabase.yml`: GitHub Actions workflow 配置
- `scripts/sync_to_supabase.py`: Python 同步脚本
- `scripts/supabase_schema.sql`: 数据库表结构定义
