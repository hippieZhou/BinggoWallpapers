using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using U5BFA.Libraries;

namespace BinggoWallpapers.WinUI.Views.TrayIcon;

public sealed partial class DefaultTrayIconMeunFlyout : TrayIconMenuFlyout
{
    public DefaultTrayIconMeunFlyoutViewModel ViewModel { get; }
    public DefaultTrayIconMeunFlyout(DefaultTrayIconMeunFlyoutViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }

    private void OnHome(object sender, RoutedEventArgs e)
    {
        if (App.MainWindow.Visible == false)
        {
            App.MainWindow.Show();
            App.MainWindow.Activate();
        }
    }

    private  void OnExit(object sender, RoutedEventArgs e)
    {
        App.Current.Exit();
    }
}

public partial class DefaultTrayIconMeunFlyoutViewModel : ObservableObject
{
    [RelayCommand(IncludeCancelCommand = true, AllowConcurrentExecutions = false)]
    private async Task OnLoaded(CancellationToken cancellationToken = default)
    {
        await Task.Yield();
    }
}
