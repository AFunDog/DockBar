using System.Collections.Generic;
using Avalonia.Input;
using DockBar.DockItem.Items;

namespace DockBar.AvaloniaApp.Helpers;

public static class DropDataHelper
{
    public static IEnumerable<DockItemBase> DropDataToDockItem(IDataObject dropData)
    {
        if (dropData.Contains(DataFormats.Files))
            foreach (var data in dropData.GetFiles() ?? [])
                yield return new DockLinkItem { LinkPath = data.Path.LocalPath };
        else if (dropData.Contains("key"))
            if (dropData.Get("key") is int key)
            {
            }

        yield break;
    }
}