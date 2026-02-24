// Copyright (c) hippieZhou. All rights reserved.

using System.Runtime.InteropServices;
using BinggoWallpapers.Core;
using BinggoWallpapers.WidgetProvider.Com;
using BinggoWallpapers.WidgetProvider.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BinggoWallpapers.WidgetProvider;

/// <summary>
/// Widget Provider 入口点
/// </summary>
internal static class Program
{
    private const uint CLSCTX_LOCAL_SERVER = 0x4;
    private const uint REGCLS_MULTIPLEUSE = 0x1;

    private static IServiceProvider? _serviceProvider;

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("ole32.dll")]
    private static extern int CoRegisterClassObject(
        [MarshalAs(UnmanagedType.LPStruct)] Guid rclsid,
        [MarshalAs(UnmanagedType.Interface)] IClassFactory pUnk,
        uint dwClsContext,
        uint flags,
        out uint lpdwRegister);

    [DllImport("ole32.dll")]
    private static extern int CoRevokeClassObject(uint dwRegister);

    /// <summary>
    /// 获取服务提供者（用于 COM 激活）
    /// </summary>
    internal static IServiceProvider? GetServiceProvider() => _serviceProvider;

    /// <summary>
    /// 应用程序入口点
    /// </summary>
    [MTAThread]
    private static void Main(string[] args)
    {
        if (args.Length == 0 ||
            !string.Equals(args[0], "-RegisterProcessAsComServer", StringComparison.Ordinal))
        {
            Console.WriteLine("Not launched for widget provider activation. Exiting.");
            return;
        }

        WinRT.ComWrappersSupport.InitializeComWrappers();

        // 配置服务
        var services = ConfigureServices();
        _serviceProvider = services.BuildServiceProvider();

        var logger = _serviceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Widget Provider 正在启动...");

        uint cookie = 0;

        // 创建 WidgetProvider 实例的工厂函数
        var factory = new WidgetProviderFactory(() =>
        {
            if (_serviceProvider == null)
            {
                throw new InvalidOperationException("Service provider not initialized");
            }

            var wallpaperService = _serviceProvider.GetRequiredService<BingWallpaperWidgetService>();
            var widgetLogger = _serviceProvider.GetRequiredService<ILogger<WidgetProvider>>();
            return new WidgetProvider(wallpaperService, widgetLogger);
        });

        var registerResult = CoRegisterClassObject(
            typeof(WidgetProvider).GUID,
            factory,
            CLSCTX_LOCAL_SERVER,
            REGCLS_MULTIPLEUSE,
            out cookie);

        if (registerResult < 0)
        {
            Marshal.ThrowExceptionForHR(registerResult);
        }

        try
        {
            logger.LogInformation("Widget Provider 已注册，等待 Widget 激活...");

            if (GetConsoleWindow() != IntPtr.Zero)
            {
                Console.WriteLine("Widget provider registered. Press ENTER to exit.");
                Console.ReadLine();
            }
            else
            {
                WidgetProvider.GetEmptyWidgetListEvent().WaitOne();
            }
        }
        finally
        {
            if (cookie != 0)
            {
                CoRevokeClassObject(cookie);
            }

            logger.LogInformation("Widget Provider 正在关闭...");
        }
    }

    /// <summary>
    /// 配置服务
    /// </summary>
    private static IServiceCollection ConfigureServices()
    {
        var services = new ServiceCollection();

        // 配置应用设置
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        services.AddSingleton<IConfiguration>(configuration);

        // 配置日志
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // 添加 Core 层服务
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BinggoWallpapers");

        services.AddCorelayer(appDataPath);

        // 注册 Widget 服务
        services.AddSingleton<BingWallpaperWidgetService>();

        return services;
    }
}
