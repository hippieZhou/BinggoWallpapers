# Supabase 同步快速开始

## 5 分钟快速设置

### 1. 创建 Supabase 项目

1. 访问 [Supabase](https://supabase.com/)
2. 注册/登录账户
3. 创建新项目（选择免费计划即可）

### 2. 创建数据库表

1. 在 Supabase Dashboard 中，点击左侧菜单的 **SQL Editor**
2. 点击 **New query**
3. 复制 `scripts/supabase_schema.sql` 的全部内容
4. 粘贴到 SQL Editor 中
5. 点击 **Run** 执行

### 3. 获取 API 密钥

1. 在 Supabase Dashboard 中，点击左侧菜单的 **Settings** > **API**
2. 复制以下信息：
   - **Project URL**: `https://xxxxx.supabase.co`
   - **service_role key**: `eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...` (⚠️ 注意：这是敏感密钥，不要公开)

### 4. 配置 GitHub Secrets

1. 进入你的 GitHub 仓库
2. 点击 **Settings** > **Secrets and variables** > **Actions**
3. 点击 **New repository secret**
4. 添加以下两个 Secrets：

   | Name | Value |
   |------|-------|
   | `SUPABASE_URL` | 你的 Project URL (例如: `https://xxxxx.supabase.co`) |
   | `SUPABASE_SERVICE_ROLE_KEY` | 你的 service_role key |

### 5. 测试同步

1. 在 GitHub Actions 页面，找到 **Sync Wallpapers to Supabase** workflow
2. 点击 **Run workflow** > **Run workflow** 手动触发
3. 等待执行完成，查看日志确认同步成功

## 验证数据

在 Supabase Dashboard > Table Editor 中，你应该能看到 `wallpapers` 表中有数据。

或者使用 SQL Editor 查询：

```sql
SELECT COUNT(*) FROM wallpapers;
```

## 常见问题

### Q: 同步失败，提示 "relation wallpapers does not exist"
**A**: 确保已执行 `scripts/supabase_schema.sql` 创建表结构

### Q: 同步失败，提示 "401 Unauthorized"
**A**: 检查 GitHub Secrets 中的 `SUPABASE_URL` 和 `SUPABASE_SERVICE_ROLE_KEY` 是否正确

### Q: 如何只同步最新的数据？
**A**: 可以修改 `scripts/sync_to_supabase.py`，添加日期过滤逻辑

## 下一步

- 查看 [SupabaseSync.md](./SupabaseSync.md) 了解详细配置和高级用法
- 使用 Supabase REST API 或客户端 SDK 访问数据
- 设置 Supabase 的实时订阅功能
