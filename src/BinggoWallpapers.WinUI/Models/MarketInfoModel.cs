using BinggoWallpapers.Core.DTOs;

namespace BinggoWallpapers.WinUI.Models;

public class MarketInfoModel(string region, MarketInfoDto meta)
{
    public MarketInfoDto Meta { get; } = meta;

    public override string ToString()
    {
        return string.Equals(region, "zh-CN", StringComparison.OrdinalIgnoreCase) ? Meta.CN : Meta.EN;
    }
}
