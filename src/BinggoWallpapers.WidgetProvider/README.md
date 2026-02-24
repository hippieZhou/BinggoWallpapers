# BinggoWallpapers Widget Provider

Windows 11 Widget Provider for BinggoWallpapers - 显示必应每日壁纸的 Widget。

## 功能特性

- 📱 支持三种尺寸：小、中、大
- 🖼️ 显示今日必应壁纸信息
- 🔄 支持手动刷新
- 💾 状态持久化（重启后恢复）
- 🌍 集成现有的壁纸服务

## 开发说明

### 项目结构

```
BinggoWallpapers.WidgetProvider/
├── Templates/              # Adaptive Card 模板
│   ├── BingWallpaperTemplate.json
│   └── LoadingTemplate.json
├── Models/                 # 数据模型
│   └── CompactWidgetInfo.cs
├── Services/               # 业务服务
│   └── BingWallpaperWidgetService.cs
├── Com/                    # COM 相关
│   └── FactoryHelper.cs
├── WidgetProvider.cs       # Widget Provider 实现
├── Program.cs              # 入口点
└── Package.appxmanifest    # 包清单
```

### 构建和部署

1. 确保已启用开发者模式（Settings → For developers）
2. 在 Visual Studio 中选择 "Provider on launch" 启动配置
3. 按 F5 启动调试
4. 打开 Widget Board (Win+W)
5. 添加 "必应每日壁纸" Widget

### 注意事项

- Widget Provider 是一个后台进程，没有 UI 窗口
- 通过 COM 激活机制运行
- 需要 MSIX 打包才能正常工作
- GUID 必须在三个地方保持一致：
  - `WidgetProvider.cs` 的 `[Guid]` 特性
  - `Package.appxmanifest` 的 `com:Class Id`
  - `Package.appxmanifest` 的 `CreateInstance ClassId`

## 参考文档

- [Windows Widgets 官方文档](https://learn.microsoft.com/en-us/windows/apps/develop/widgets/)
- [创建 Windows 11 Widget 教程](https://xakpc.dev/windows-widgets/create-windows-widget/)
