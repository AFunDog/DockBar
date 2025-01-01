using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DockBar.Core.DockItems;

namespace DockBar.Avalonia;

internal static class KeyActionDockItems
{
    public static KeyActionDockItem SettingDockItem { get; } =
        new KeyActionDockItem()
        {
            ShowName = "设置",
            ActionKey = "Setting",
            IconData = IconResource.SettingIconStream.ToArray()
        };

    public static Dictionary<string, Action> KeyActions { get; } = [];
}
