using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DockBar.DockItem;

namespace DockBar.Avalonia.ViewDatas
{
    internal sealed class DockItemData : ObservableObject
    {
        internal string Key => DockItem.Key;

        internal string? IconPath => DockItem.IconPath;

        internal IDockItem DockItem { get; }

        public DockItemData(IDockItem dockItem)
        {
            DockItem = dockItem;
        }
    }
}
