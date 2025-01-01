using DockBar.Core.DockItems;
using DockBar.Core.Internals;
using DockBar.Core.Structs;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace DockBar.Core;

public interface IDockItemService
{
    IEnumerable<DockItemBase> DockItems { get; }

    event Action<IDockItemService, DockItemChangedEventArgs>? DockItemChanged;
    event Action<IDockItemService, DockItemBase>? DockItemStarted;

    DockItemBase? GetDockItem(int key);
    void RegisterDockItem(DockItemBase dockItem);
    void UnregisterDockItem(int key);
    void LoadData(string filePath);
    void SaveData(string filePath);
}
