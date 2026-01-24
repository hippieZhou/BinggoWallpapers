// Copyright (c) hippieZhou. All rights reserved.

using BinggoWallpapers.Core;
using BinggoWallpapers.WinUI.Models;
using BinggoWallpapers.WinUI.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;

namespace BinggoWallpapers.WinUI;

public partial class App : Application
{
    /// <summary>
    /// The .NET Generic Host provides dependency injection, configuration, logging, and other services.
    /// https://docs.microsoft.com/dotnet/core/extensions/generic-host
    /// https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
    /// https://docs.microsoft.com/dotnet/core/extensions/configuration
    /// https://docs.microsoft.com/dotnet/core/extensions/logging
    /// </summary>
    private readonly IHost _host;

    public static WindowEx MainWindow { get; } = new ShellWindow();

    public static T GetService<T>() where T : class
    {
        if ((Current as App)!._host.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        }

        return service;
    }

    public App()
    {
        InitializeComponent();

        _host = Host
            .CreateDefaultBuilder()
            .UseContentRoot(AppContext.BaseDirectory)
            .UseDefaultServiceProvider((context, options) =>
            {
                options.ValidateOnBuild = true;
            })
            .ConfigureServices((context, services) =>
            {
                services.AddLogging(context.Configuration);
                services.AddSettings();
                services.AddMvvm();
                services.AddOptions();

                services.AddApplicationLayer(DispatcherQueue.GetForCurrentThread());
                services.AddCorelayer(AppInfo.AppDataPath);
            })
#if DEBUG
            .UseEnvironment(Environments.Development)
#else
            .UseEnvironment(Environments.Production)
#endif
            .Build();

        AppInstance.GetCurrent().Activated += OnActivated;

        SetupGlobalExceptionHandlers();
    }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);
        await _host.StartAsync();

        var activation = GetService<IActivationService>();
        await activation.ActivateAsync(args);

        //var notification = GetService<IAppNotificationService>();
        //notification.Show(string.Format("AppNotificationSamplePayload".GetLocalized(), AppContext.BaseDirectory));

        App.GetService<ILogger<App>>().LogInformation("The app has been launched successfully.");
    }

    private async void OnActivated(object sender, AppActivationArguments e)
    {
        var localArgsDataReference = e.Data;

        var dispatcher = GetService<IUIDispatcher>();

        await dispatcher.EnqueueAsync(async () =>
        {
            await GetService<IActivationService>().ActivateAsync(localArgsDataReference);
        });
    }

    private void SetupGlobalExceptionHandlers()
    {
        // Handle exceptions from the current AppDomain (non-UI thread crashes)
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            if (e.ExceptionObject is Exception ex)
            {
                LogException("AppDomain.UnhandledException", ex);
            }
        };

        // Handle exceptions from background tasks (Task.Run, async void, etc.)
        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            LogException("TaskScheduler.UnobservedTaskException", e.Exception);
            e.SetObserved();
        };

        // Handle exceptions from the UI thread
        UnhandledException += (sender, e) =>
        {
            LogException("Application.UnhandledException", e.Exception);
            e.Handled = true;
        };
    }

    private static void LogException(string source, Exception ex)
    {
        var message = $"[{source}] {ex?.GetType().Name}: {ex?.Message}";
        if (ex?.InnerException != null)
        {
            message += $"\nInner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}";
        }

        message += $"\nStack: {ex?.StackTrace}";

        GetService<ILogger<App>>().LogError(ex, message);
    }
}
