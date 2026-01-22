-- Supabase 数据库表结构
-- 在 Supabase Dashboard > SQL Editor 中执行此脚本

-- 创建壁纸表
CREATE TABLE IF NOT EXISTS wallpapers (
    id BIGSERIAL PRIMARY KEY,
    hash VARCHAR(255) NOT NULL,
    actual_date TIMESTAMPTZ NOT NULL,
    market_code VARCHAR(20) NOT NULL,
    resolution_code VARCHAR(20) NOT NULL,
    info_json JSONB NOT NULL,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),

    -- 唯一约束：同一市场、分辨率、日期的壁纸只能有一条记录
    CONSTRAINT unique_wallpaper UNIQUE (hash, market_code, resolution_code, actual_date)
);

-- 创建索引以提高查询性能
CREATE INDEX IF NOT EXISTS idx_wallpapers_hash ON wallpapers(hash);
CREATE INDEX IF NOT EXISTS idx_wallpapers_market_code ON wallpapers(market_code);
CREATE INDEX IF NOT EXISTS idx_wallpapers_actual_date ON wallpapers(actual_date);
CREATE INDEX IF NOT EXISTS idx_wallpapers_resolution_code ON wallpapers(resolution_code);
CREATE INDEX IF NOT EXISTS idx_wallpapers_created_at ON wallpapers(created_at);

-- 创建复合索引用于常见查询
CREATE INDEX IF NOT EXISTS idx_wallpapers_market_date ON wallpapers(market_code, actual_date DESC);
CREATE INDEX IF NOT EXISTS idx_wallpapers_market_resolution ON wallpapers(market_code, resolution_code);

-- 创建 GIN 索引用于 JSONB 查询
CREATE INDEX IF NOT EXISTS idx_wallpapers_info_json ON wallpapers USING GIN (info_json);

-- 创建更新时间触发器函数
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- 创建触发器
CREATE TRIGGER update_wallpapers_updated_at
    BEFORE UPDATE ON wallpapers
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- 启用 Row Level Security (RLS)
-- 这是客户端安全的关键：即使 Anon Key 被公开，RLS 也会限制数据访问
ALTER TABLE wallpapers ENABLE ROW LEVEL SECURITY;

-- 创建允许所有人读取的策略（客户端使用 Anon Key）
-- 壁纸数据是公开的，所以允许所有人读取
CREATE POLICY "Allow public read access" ON wallpapers
    FOR SELECT
    USING (true);

-- 禁止客户端写入（客户端应该只读）
-- 写入操作应该通过服务器端 API（使用 Service Role Key）
CREATE POLICY "Deny client insert" ON wallpapers
    FOR INSERT
    WITH CHECK (false);

CREATE POLICY "Deny client update" ON wallpapers
    FOR UPDATE
    USING (false);

CREATE POLICY "Deny client delete" ON wallpapers
    FOR DELETE
    USING (false);

-- 注意：Service Role Key 会绕过 RLS，所以服务器端可以正常写入
-- 这些策略只影响使用 Anon Key 的客户端请求

-- 注释
COMMENT ON TABLE wallpapers IS 'Bing 每日壁纸数据表';
COMMENT ON COLUMN wallpapers.hash IS '壁纸的唯一哈希值';
COMMENT ON COLUMN wallpapers.actual_date IS '壁纸的实际日期';
COMMENT ON COLUMN wallpapers.market_code IS '市场代码 (如 zh-CN, en-US)';
COMMENT ON COLUMN wallpapers.resolution_code IS '分辨率代码 (Standard, FullHD, HD, UHD4K)';
COMMENT ON COLUMN wallpapers.info_json IS '完整的壁纸信息 JSON 数据';
