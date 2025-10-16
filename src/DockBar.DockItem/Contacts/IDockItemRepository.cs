using DockBar.DockItem.Structs;

namespace DockBar.DockItem.Contacts;

public interface IDockItemRepository
{
    event EventHandler<RepositoryItemChangedEventArgs<Structs.DockItemData>>? ItemsChanged;
    IEnumerable<Structs.DockItemData> SelectAll();
    Structs.DockItemData? Select(Guid id);
    bool Insert(Structs.DockItemData dockItemData);
    bool Update(Structs.DockItemData dockItemData);
    bool InsertOrUpdate(Structs.DockItemData dockItemData);
    bool Delete(Guid id);
}