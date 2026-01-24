// Copyright (c) hippieZhou. All rights reserved.

using System.Globalization;
using BinggoWallpapers.WinUI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BinggoWallpapers.WinUI.Views;

public sealed partial class DetailPage : Page
{
    public CultureInfo Culture { get; } 
    public DetailViewModel ViewModel { get; }

    public DetailPage()
    {
        InitializeComponent();
        Culture = CultureInfo.CurrentCulture;
        ViewModel = App.GetService<DetailViewModel>();
    }

    private void ToggleEditState()
    {
        WallpaperView.IsPaneOpen = !WallpaperView.IsPaneOpen;
    }

    private void ShadowRect_Loaded(object sender, RoutedEventArgs e)
    {
        shadow.Receivers.Add(ShadowCastGrid);
    }
}
