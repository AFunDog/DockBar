using System;
using System.Collections.Generic;
using Avalonia.Platform;
using DockBar.DockItem.Items;

namespace DockBar.AvaloniaApp;

internal static class KeyActionDockItems
{
    public static KeyActionDockItem SettingDockItem { get; } = new()
    {
        ShowName = "设置",
        ActionKey = "Setting",
        IconData = AssetLoader.Open(IconResource.SettingIconUri).ToMemoryStream().ToArray()
    };

    public static Dictionary<string, Action> KeyActions { get; } = [];
}