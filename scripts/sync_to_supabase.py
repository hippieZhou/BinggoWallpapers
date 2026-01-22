#!/usr/bin/env python3
"""
将 Bing 壁纸数据同步到 Supabase 数据库

使用方法:
    python3 scripts/sync_to_supabase.py

环境变量:
    SUPABASE_URL: Supabase 项目 URL (必需)
    SUPABASE_SERVICE_ROLE_KEY: Supabase Service Role Key (必需)
    SUPABASE_TABLE_NAME: 表名 (可选，默认为 'wallpapers')
"""

import os
import json
import sys
from pathlib import Path
from typing import Dict, List, Any
import requests
from datetime import datetime

# 配置
SUPABASE_URL = os.getenv("SUPABASE_URL")
SUPABASE_KEY = os.getenv("SUPABASE_SERVICE_ROLE_KEY") or os.getenv("SUPABASE_KEY")
TABLE_NAME = os.getenv("SUPABASE_TABLE_NAME", "wallpapers")
ARCHIVE_DIR = Path("archive")

# 市场代码映射
MARKET_CODE_MAP = {
    "China": "zh-CN",
    "UnitedStates": "en-US",
    "UnitedKingdom": "en-GB",
    "Japan": "ja-JP",
    "Germany": "de-DE",
    "France": "fr-FR",
    "Spain": "es-ES",
    "Italy": "it-IT",
    "Russia": "ru-RU",
    "SouthKorea": "ko-KR",
    "Brazil": "pt-BR",
    "Australia": "en-AU",
    "Canada": "en-CA",
    "India": "en-IN",
}

# 分辨率代码映射
RESOLUTION_CODE_MAP = {
    "Standard": "1366x768",
    "FullHD": "1920x1080",
    "HD": "1920x1200",
    "UHD4K": "3840x2160",
}


def parse_date(date_str: str) -> datetime:
    """解析日期字符串 (格式: YYYYMMDD)"""
    try:
        return datetime.strptime(date_str, "%Y%m%d")
    except ValueError:
        return datetime.now()


def transform_wallpaper_data(json_data: Dict[str, Any], country: str, date_str: str) -> List[Dict[str, Any]]:
    """
    将 JSON 数据转换为 Supabase 数据库记录
    
    每个壁纸可能有多个分辨率，需要为每个分辨率创建一条记录
    """
    records = []
    
    # 解析日期
    time_info = json_data.get("timeInfo", {})
    start_date_str = time_info.get("startDate", date_str.replace("-", ""))
    actual_date = parse_date(start_date_str)
    
    # 获取基本信息
    market_code = json_data.get("marketCode", MARKET_CODE_MAP.get(country, ""))
    hash_value = json_data.get("hash", "")
    
    # 为每个分辨率创建记录
    image_resolutions = json_data.get("imageResolutions", [])
    
    if not image_resolutions:
        # 如果没有分辨率信息，创建一条默认记录
        records.append({
            "hash": hash_value,
            "actual_date": actual_date.isoformat(),
            "market_code": market_code,
            "resolution_code": "FullHD",
            "info_json": json.dumps(json_data, ensure_ascii=False),
            "created_at": datetime.now().isoformat(),
            "updated_at": datetime.now().isoformat(),
        })
    else:
        for resolution in image_resolutions:
            resolution_code = resolution.get("resolution", "FullHD")
            
            records.append({
                "hash": hash_value,
                "actual_date": actual_date.isoformat(),
                "market_code": market_code,
                "resolution_code": resolution_code,
                "info_json": json.dumps(json_data, ensure_ascii=False),
                "created_at": datetime.now().isoformat(),
                "updated_at": datetime.now().isoformat(),
            })
    
    return records


def upsert_to_supabase(records: List[Dict[str, Any]]) -> bool:
    """使用 Supabase REST API 批量插入/更新数据"""
    if not SUPABASE_URL or not SUPABASE_KEY:
        print("❌ 错误: 缺少 Supabase 配置")
        print("请设置环境变量: SUPABASE_URL 和 SUPABASE_SERVICE_ROLE_KEY")
        return False
    
    # Supabase REST API 端点
    # 使用 upsert: PostgREST 会自动使用唯一约束处理冲突
    url = f"{SUPABASE_URL}/rest/v1/{TABLE_NAME}"
    
    headers = {
        "apikey": SUPABASE_KEY,
        "Authorization": f"Bearer {SUPABASE_KEY}",
        "Content-Type": "application/json",
        "Prefer": "resolution=merge-duplicates",  # 使用 upsert (合并重复项)
    }
    
    # 批量插入（Supabase 支持批量操作）
    batch_size = 100
    total_inserted = 0
    total_updated = 0
    
    for i in range(0, len(records), batch_size):
        batch = records[i:i + batch_size]
        
        try:
            response = requests.post(
                url,
                json=batch,
                headers=headers,
                timeout=60  # 增加超时时间
            )
            
            if response.status_code in [200, 201]:
                total_inserted += len(batch)
                # 尝试从响应头获取更新的记录数
                prefer_applied = response.headers.get("Preference-Applied", "")
                if "updated" in prefer_applied.lower():
                    # 如果返回了更新信息，尝试解析
                    pass
                print(f"✅ 成功处理 {len(batch)} 条记录 (总计: {total_inserted}/{len(records)})")
            elif response.status_code == 409:
                # 冲突错误，可能是唯一约束问题，尝试逐个插入
                print(f"⚠️  批量插入遇到冲突，尝试逐个处理...")
                for record in batch:
                    try:
                        single_response = requests.post(
                            url,
                            json=[record],
                            headers=headers,
                            timeout=30
                        )
                        if single_response.status_code in [200, 201]:
                            total_inserted += 1
                    except Exception as e:
                        print(f"⚠️  跳过记录 {record.get('hash', 'unknown')}: {e}")
                        continue
            else:
                print(f"❌ 插入失败: {response.status_code}")
                print(f"响应内容: {response.text[:500]}")
                # 不立即返回，尝试继续处理下一批
                continue
                
        except requests.exceptions.Timeout:
            print(f"⚠️  请求超时，跳过当前批次")
            continue
        except requests.exceptions.RequestException as e:
            print(f"❌ 请求异常: {e}")
            # 不立即返回，尝试继续处理下一批
            continue
    
    if total_inserted > 0:
        print(f"\n✅ 成功处理 {total_inserted}/{len(records)} 条记录")
        return True
    else:
        print(f"\n❌ 没有成功处理任何记录")
        return False


def main():
    """主函数"""
    print("=" * 60)
    print("🚀 Bing 壁纸数据同步到 Supabase")
    print("=" * 60)
    
    # 检查配置
    if not SUPABASE_URL or not SUPABASE_KEY:
        print("❌ 错误: 缺少 Supabase 配置")
        print("\n请在 GitHub Secrets 中设置:")
        print("  - SUPABASE_URL: Supabase 项目 URL")
        print("  - SUPABASE_SERVICE_ROLE_KEY: Supabase Service Role Key")
        sys.exit(1)
    
    print(f"📊 Supabase URL: {SUPABASE_URL}")
    print(f"📋 表名: {TABLE_NAME}")
    print(f"📁 数据目录: {ARCHIVE_DIR}")
    print()
    
    # 收集所有 JSON 文件
    json_files = list(ARCHIVE_DIR.glob("**/*.json"))
    print(f"📂 找到 {len(json_files)} 个 JSON 文件")
    
    if not json_files:
        print("⚠️  没有找到数据文件")
        return
    
    # 处理所有文件
    all_records = []
    processed_count = 0
    error_count = 0
    
    for json_file in json_files:
        try:
            # 解析文件路径获取国家和日期
            parts = json_file.parts
            if len(parts) < 3:
                continue
            
            country = parts[-2]  # archive/Country/date.json
            date_str = json_file.stem  # 文件名（不含扩展名）
            
            # 读取 JSON 文件
            with open(json_file, "r", encoding="utf-8") as f:
                json_data = json.load(f)
            
            # 转换数据
            records = transform_wallpaper_data(json_data, country, date_str)
            all_records.extend(records)
            processed_count += 1
            
            if processed_count % 100 == 0:
                print(f"📄 已处理 {processed_count}/{len(json_files)} 个文件...")
                
        except Exception as e:
            print(f"⚠️  处理文件失败 {json_file}: {e}")
            error_count += 1
            continue
    
    print(f"\n✅ 处理完成: {processed_count} 个文件成功, {error_count} 个文件失败")
    print(f"📊 总共生成 {len(all_records)} 条记录")
    print()
    
    # 同步到 Supabase
    if all_records:
        print("🔄 开始同步数据到 Supabase...")
        success = upsert_to_supabase(all_records)
        
        if success:
            print(f"\n✅ 同步成功! 共同步 {len(all_records)} 条记录")
        else:
            print("\n❌ 同步失败")
            sys.exit(1)
    else:
        print("⚠️  没有可同步的记录")


if __name__ == "__main__":
    main()
