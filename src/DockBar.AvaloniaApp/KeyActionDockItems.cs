using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Platform;

using DockBar.DockItem.Structs;

namespace DockBar.AvaloniaApp;

internal static class KeyActionDockItems
{
    public static KeyActionDockItem SettingDockItem { get; } =
        new KeyActionDockItem()
        {
            ShowName = "设置",
            ActionKey = "Setting",
            IconData = AssetLoader.Open(IconResource.SettingIconUri).ToMemoryStream().ToArray(),
        };

    public static Dictionary<string, Action> KeyActions { get; } = [];
}
