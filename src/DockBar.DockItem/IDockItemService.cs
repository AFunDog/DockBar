using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DockBar.DockItem;

public interface IDockItemService
{
    IReadOnlyCollection<IDockItem> DockItems { get; }

    event EventHandler<DockItemChangedEventArgs>? DockItemChanged;

    void AddDockLinkItem(string key, string linkPath);

    void AddDockItem(IDockItem dockItem);

    void RemoveDockItem(string key);

    IDockItem? GetDockItem(string key);

    void SaveData(string filePath);
    void ReadData(string filePath);
}

public class DockItemChangedEventArgs : EventArgs
{
    public required IDockItem DockItem { get; init; }
    public required bool IsAdd { get; init; }
}
