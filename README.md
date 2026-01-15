<div align="center">

# Binggo Wallpapers

  <img src="design/app_icon.svg" alt="BinggoWallpapers" width="128" height="128">

  <br/>

  <a href="https://apps.microsoft.com/detail/9ph6t26g23xh?referrer=appbadge&mode=direct">
	  <img src="https://get.microsoft.com/images/en-us%20dark.svg" width="200"/>
  </a>

</div>

> A WinUI 3-based Bing wallpaper application that lets you easily browse, download, and set beautiful wallpapers from around the world.

[English](README.md) | **[中文](README.zh-CN.md)**

[![Build and Test](https://github.com/hippiezhou/BinggoWallpapers/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/hippiezhou/BinggoWallpapers/actions/workflows/build-and-test.yml)
[![codecov](https://codecov.io/gh/hippiezhou/BinggoWallpapers/branch/main/graph/badge.svg?token=SX3PU5ZP2I)](https://codecov.io/gh/hippiezhou/BinggoWallpapers)
![Platform](https://img.shields.io/badge/Platform-Windows-blue)
![WinUI 3](https://img.shields.io/badge/WinUI-3.0-blue)
![.NET 10](https://img.shields.io/badge/.NET-10.0-blue)
![License](https://img.shields.io/badge/License-MIT-green)

## ✨ Features

### 🌍 Multi-Region Support

-   **14 Regions**: China 🇨🇳, United States 🇺🇸, United Kingdom 🇬🇧, Japan 🇯🇵, Germany 🇩🇪, France 🇫🇷, Spain 🇪🇸, Italy 🇮🇹, Russia 🇷🇺, South Korea 🇰🇷, Brazil 🇧🇷, Australia 🇦🇺, Canada 🇨🇦, India 🇮🇳
-   **Localized Content**: Each region offers unique wallpaper content reflecting local culture and scenery
-   **One-Click Switch**: Easily switch between regions to discover more amazing wallpapers
-   **Smart Language Adaptation**: Automatically sets corresponding language headers based on region
-   **Concurrent Collection**: Supports concurrent data collection across multiple regions for improved efficiency

### 🖼️ Wallpaper Management

-   **Real-Time Browsing**: View the latest Bing daily wallpapers
-   **Historical Archive**: Browse historical wallpapers and discover classic works with incremental loading support
-   **Multi-Resolution Download**: Support for 4 resolutions (1366x768, 1920x1080, 1920x1200, 3840x2160 4K)
-   **Local Database**: SQLite storage for wallpaper information with offline browsing support
-   **Incremental Loading**: Smooth infinite scrolling using IncrementalLoadingCollection

### 🎨 Image Editing & Effects

-   **Real-Time Effect Preview**: 7 image effects (Exposure, Temperature, Tint, Blur, Contrast, Saturation, Pixelation)
-   **Win2D Graphics Engine**: High-performance image processing based on Microsoft.Graphics.Win2D
-   **Mockup Preview**: Preview wallpaper effects in device models
-   **High-Quality Export**: Support for 2x high-resolution export in PNG/JPEG format
-   **Real-Time Rendering**: Smooth real-time effect preview using CanvasControl

### 💎 User Experience

-   **Modern Interface**: Fluent Design language based on WinUI 3
-   **Responsive Layout**: Adapts to different screen sizes with elegant animation transitions
-   **Loading Animations**: Shimmer loading effects and elegant loading state indicators
-   **Error Handling**: User-friendly error prompts and retry mechanisms with comprehensive logging
-   **In-App Notifications**: Elegant message notifications using StackedNotificationsBehavior
-   **System Notifications**: Support for Windows system-level notifications

## 🚀 Quick Start

Check out the [Quick Start Guide](docs/QuickStart.md) for detailed information about the tech stack, system requirements, development environment setup, configuration and running the application, as well as database management operations.

## 🔄 Continuous Integration

This project uses GitHub Actions for continuous integration and automation. Check out the [GitHub Actions Documentation](.github/ACTIONS.md) for detailed workflow configuration and usage.

-   **Build Status**: Click the "Build and Test" badge at the top to view the latest build status
-   **Test Coverage**: Click the "codecov" badge to view detailed code coverage reports
-   **Workflow History**: Visit the [Actions](https://github.com/hippiezhou/BinggoWallpapers/actions) page to view all build history

## 🤝 Contributing

Contributions of all kinds are welcome!

### How to Contribute

1. **Fork the Project**
2. **Create a Feature Branch** (`git checkout -b feature/AmazingFeature`)
3. **Commit Your Changes** (`git commit -m 'Add some AmazingFeature'`)
4. **Push to the Branch** (`git push origin feature/AmazingFeature`)
5. **Open a Pull Request**

### Development Guidelines

-   **Code Style**: Follow C# coding standards and .editorconfig configuration
-   **Naming Conventions**: Use meaningful variable and function names
-   **Comment Standards**: Add XML documentation comments
-   **Testing Requirements**: Add unit tests for new features, ensure test coverage does not decrease
-   **Commit Standards**: Use clear commit messages (Conventional Commits recommended)
-   **Code Review**: Ensure code passes all tests and CI checks

## 🔒 Privacy Policy

We take your privacy seriously. This application:

-   ✅ **Does not collect** any personal information
-   ✅ **Does not track** user behavior
-   ✅ **Does not upload** any data to servers
-   ✅ All data is **stored locally only**

View the complete [Privacy Policy](PRIVACY_POLICY.md) or [online version](https://hippiezhou.github.io/products/binggo-wallpapers/privacy-policy).

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE.txt) file for details.

## 🙏 Acknowledgments

-   [Microsoft WinUI](https://github.com/microsoft/microsoft-ui-xaml) - Modern Windows UI framework
-   [CommunityToolkit](https://github.com/CommunityToolkit) - Excellent WinUI toolkit
-   [Bing Wallpaper API](https://www.bing.com/HPImageArchive.aspx) - Bing wallpaper data source
-   [Microsoft.Graphics.Win2D](https://github.com/Microsoft/Win2D) - High-performance 2D graphics rendering
-   [Entity Framework Core](https://github.com/dotnet/efcore) - Data access ORM framework
-   [Serilog](https://github.com/serilog/serilog) - Structured logging framework

## 📞 Contact

-   **Project Homepage**: [GitHub Repository](https://github.com/hippiezhou/BinggoWallpapers)
-   **Issue Reporting**: [Issues](https://github.com/hippiezhou/BinggoWallpapers/issues)
-   **Feature Suggestions**: [Discussions](https://github.com/hippiezhou/BinggoWallpapers/discussions)

---

⭐ If this project helps you, please give it a star!
