// Copyright (c) hippieZhou. All rights reserved.

using System.Collections.ObjectModel;
using BinggoWallpapers.Core.DTOs;
using BinggoWallpapers.WinUI.Models;
using BinggoWallpapers.WinUI.Notifications;
using BinggoWallpapers.WinUI.Selectors;
using BinggoWallpapers.WinUI.Views.UserControls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.Windows.Storage.Pickers;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;

namespace BinggoWallpapers.WinUI.ViewModels;

public partial class SettingsViewModel : ObservableRecipient
{
    private readonly ILoggingSelectorService _loggingSelector;
    private readonly IDownloadSelectorService _downloadSelector;
    private readonly ILanguageSelectorService _languageSelector;
    private readonly IThemeSelectorService _themeSelector;
    private readonly ITrayIconSelectorService _trayIconService;
    private readonly IMarketSelectorService _marketSelectorService;
    private readonly IStartupSelectorService _startupSelectorService;
    private readonly IInAppNotificationService _inAppNotificationService;
    private readonly ILogger<SettingsViewModel> _logger;

    public SettingsViewModel(
    ILoggingSelectorService loggingSelector,
    IDownloadSelectorService downloadSelector,
    ILanguageSelectorService languageSelector,
    IThemeSelectorService themeSelector,
    ITrayIconSelectorService trayIconService,
    IMarketSelectorService marketSelectorService,
    IStartupSelectorService startupSelectorService,
    IInAppNotificationService inAppNotificationService,
    ILogger<SettingsViewModel> logger)
    {
        _loggingSelector = loggingSelector;
        _downloadSelector = downloadSelector;
        _languageSelector = languageSelector;
        _themeSelector = themeSelector;
        _trayIconService = trayIconService;
        _marketSelectorService = marketSelectorService;
        _startupSelectorService = startupSelectorService;
        _inAppNotificationService = inAppNotificationService;
        _logger = logger;

        PicturesPath = _downloadSelector.DownloadPath;
        LogFileSizeInBytes = _loggingSelector.FolderSizeInBytes;

        CurrentLanguage = _languageSelector.Language;
        CurrentTheme = _themeSelector.Theme;
        IsTrayIconEnabled = _trayIconService.IsEnabled;
        IsStartupEnabled = _startupSelectorService.IsEnabled;

        Markets = new ObservableCollection<MarketInfoDto>(_marketSelectorService.SupportedMarkets);
        SelectedMarket = _marketSelectorService.Market;
    }

    [ObservableProperty]
    public partial string CurrentLanguage { get; set; }

    [ObservableProperty]
    public partial ElementTheme CurrentTheme { get; set; }

    [ObservableProperty]
    public partial string PicturesPath { get; set; }

    [ObservableProperty]
    public partial long LogFileSizeInBytes { get; set; }
    [ObservableProperty]
    public partial bool IsStartupEnabled { get; set; }
    [ObservableProperty]
    public partial bool IsTrayIconEnabled { get; set; }

    public ObservableCollection<MarketInfoDto> Markets { get; private set; }

    [ObservableProperty]
    public partial MarketInfoDto SelectedMarket { get; set; }

    partial void OnIsStartupEnabledChanged(bool value)
    {
        _logger.LogInformation("开机自启动设置已更改: {IsEnabled}", value);
        _ = Task.Run(async () =>
        {
            try
            {
                await _startupSelectorService.ToggleAsync(value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "切换开机自启动状态失败");
            }
        });
    }

    partial void OnIsTrayIconEnabledChanged(bool value)
    {
        _logger.LogInformation("TrayIcon 设置已更改: {IsEnabled}", value);
        _ = Task.Run(async () =>
        {
            try
            {
                await _trayIconService.ToggleAsync(value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "切换 TrayIcon 状态失败");
            }
        });
    }

    [RelayCommand]
    private async Task OnSwitchLanguage(string language)
    {
        if (CurrentLanguage != language)
        {
            CurrentLanguage = language;
            await _languageSelector.SetLanguageAsync(language);
        }
    }

    [RelayCommand]
    private async Task OnSwitchTheme(ElementTheme param)
    {
        if (CurrentTheme != param)
        {
            CurrentTheme = param;
            await _themeSelector.SetThemeAsync(param);
        }
    }

    [RelayCommand]
    private void OnSwitchMarket(MarketInfoDto market)
    {
        if (market is not null && SelectedMarket?.Code != market.Code)
        {
            SelectedMarket = market;
        }
    }

    partial void OnSelectedMarketChanged(MarketInfoDto oldValue, MarketInfoDto newValue)
    {
        if (oldValue is null || newValue is null || oldValue.Code == newValue.Code)
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await _marketSelectorService.SetMarketAsync(newValue);
                _logger.LogInformation("区域设置已更改: {MarketCode}", newValue.Code);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "切换区域设置失败");
            }
        });
    }

    [RelayCommand]
    private async Task OnFetchFromGithub()
    {
        await SynchronizationDialog.Current.ShowAsync();
    }

    [RelayCommand]
    private async Task OnChangeDownloadFolder(string path)
    {
        var folderPicker = new FolderPicker(App.MainWindow.AppWindow.Id)
        {
            SuggestedStartLocation = PickerLocationId.PicturesLibrary,
            ViewMode = PickerViewMode.List,
        };

        var result = await folderPicker.PickSingleFolderAsync();
        if (result is not null)
        {
            await _downloadSelector.SetDownloadPathAsync(result.Path);
            PicturesPath = _downloadSelector.DownloadPath;
        }
    }

    [RelayCommand]
    private async Task OnResetDownloadFolder()
    {
        await _downloadSelector.SetDownloadPathAsync(AppInfo.DefaultPicturesPath);
        PicturesPath = _downloadSelector.DownloadPath;
    }

    [RelayCommand]
    private void OnLogCleanUp()
    {
        _loggingSelector.CleanUpOldLogs();
        LogFileSizeInBytes = _loggingSelector.FolderSizeInBytes;
    }

    [RelayCommand]
    private async Task OnOpenFolder(string path)
    {
        try
        {
            await Launcher.LaunchFolderPathAsync(path);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
    }

    [RelayCommand]
    private void OnCloneRepo(string agrs)
    {
        var package = new DataPackage();
        package.SetText(agrs);
        Clipboard.SetContent(package);
        _inAppNotificationService.ShowInfo("命令已复制到剪贴板");
    }

    [RelayCommand]
    private async Task OnBugRequest()
    {
        var subject = "多彩壁纸（BinggoWallpapers）客户反馈";

        var body =
    @$"您好，

我想反馈以下问题：
【请填写问题描述】

设备信息：
- System Version: {AppInfo.OSVersion}
- Application Version: {AppInfo.AppVersion}
- WindowsAppSDK Version:{AppInfo.WinAppSdkRuntimeDetails}

谢谢！";
        var email = "hippiezhou@outlook.com";
        var encodedSubject = Uri.EscapeDataString(subject);
        var encodedBody = Uri.EscapeDataString(body);
        var mailtoUri = new Uri($"mailto:{email}?subject={encodedSubject}&body={encodedBody}");
        await Launcher.LaunchUriAsync(mailtoUri);
    }
}
