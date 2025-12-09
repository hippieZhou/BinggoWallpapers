// Copyright (c) hippieZhou. All rights reserved.

using BinggoWallpapers.Core.DTOs;

namespace BinggoWallpapers.WinUI.Selectors;

public interface ILanguageSelectorService
{
    string Language
    {
        get;
    }

    Task InitializeAsync();

    Task SetLanguageAsync(string language);
    Task SetRequestedLanguageAsync();

    string GetMarketDisplayName(MarketInfoDto marketInfo);
}
