using BinggoWallpapers.Core.DTOs;
using BinggoWallpapers.WinUI.Selectors;
using Microsoft.UI.Xaml.Data;

namespace BinggoWallpapers.WinUI.Helpers;

public partial class MarketInfoDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is MarketInfoDto marketInfo)
        {
            var languageSelector = App.GetService<ILanguageSelectorService>();
            return languageSelector.GetMarketDisplayName(marketInfo);
        }
        else
        {
            return value?.ToString();
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
