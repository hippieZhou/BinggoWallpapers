// Copyright (c) hippieZhou. All rights reserved.

namespace BinggoWallpapers.WinUI.Selectors;

public interface ITrayIconSelectorService
{
    bool IsEnabled { get; }

    Task InitializeAsync();

    Task ToggleAsync(bool value);

    Task SetRequestedTrayIconAsync();
}
