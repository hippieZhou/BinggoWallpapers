using System.Collections.ObjectModel;
using BinggoWallpapers.Core.DTOs;
using BinggoWallpapers.Core.Services;
using BinggoWallpapers.WinUI.Selectors;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using U5BFA.Libraries;

namespace BinggoWallpapers.WinUI.Views.TrayIcon;
 
public sealed partial class DefaultTrayIconFlyout : TrayIconFlyout
{
    public DefaultTrayIconFlyoutViewModel ViewModel { get; }
    public DefaultTrayIconFlyout(DefaultTrayIconFlyoutViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}

public partial class DefaultTrayIconFlyoutViewModel(
    IManagementService management,
    IMarketSelectorService marketSelector) : ObservableObject
{
    public ObservableCollection<WallpaperInfoDto> Wallpapers { get; private set; } = [];

    [RelayCommand(IncludeCancelCommand = true, AllowConcurrentExecutions = false)]
    private async Task OnLoaded(CancellationToken cancellationToken = default)
    {
        Wallpapers.Clear();
        var market = marketSelector.Market;
        var wallpapers = await management.GetByMarketCodeAsync(market, cancellationToken: cancellationToken);
        foreach (var wallpaper in wallpapers)
        {
            Wallpapers.Add(wallpaper);
        }
    }
}
