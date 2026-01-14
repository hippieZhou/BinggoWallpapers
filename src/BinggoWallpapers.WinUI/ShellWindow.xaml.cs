// Copyright (c) hippieZhou. All rights reserved.

using BinggoWallpapers.WinUI.Helpers;
using BinggoWallpapers.WinUI.Models;
using BinggoWallpapers.WinUI.Selectors;
using BinggoWallpapers.WinUI.ViewModels;
using Microsoft.UI.Windowing;

namespace BinggoWallpapers.WinUI;

public sealed partial class ShellWindow : WindowEx
{
    public ShellViewModel ViewModel { get; } = App.GetService<ShellViewModel>();

    public ShellWindow()
    {
        InitializeComponent();
        this.CenterOnScreen();
        this.SetAppTitleBar(AppTitleBar, "Assets/WindowIcon.ico", AppInfo.AppTitle);

        AppWindow.Closing += OnAppWindowClosing;
        WindowStateChanged += OnWindowStateChanged;
        Closed += OnShellWindowClosed;
        ViewModel.Initialize(
            NavView,
            NavFrame,
            InAppNotification.NotificationQueue);
    }

    private void OnAppWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        var trayIconSelector = App.GetService<ITrayIconSelectorService>();
        if (trayIconSelector.IsEnabled)
        {
            sender.Hide();
            args.Cancel = true;
        }
    }

    private void OnWindowStateChanged(object sender, WindowState e)
    {
        var trayIconSelector = App.GetService<ITrayIconSelectorService>();
        if (trayIconSelector.IsEnabled)
        {
            AppWindow.IsShownInSwitchers = e != WindowState.Minimized;
        }
    }

    private void OnShellWindowClosed(object sender, Microsoft.UI.Xaml.WindowEventArgs args)
    {
        AppWindow.Closing -= OnAppWindowClosing;
        WindowStateChanged -= OnWindowStateChanged;
        Closed -= OnShellWindowClosed;
    }
}
