using DockBar.DockItem.Structs;

namespace DockBar.DockItem.Contacts;

public interface IIconDataRepository
{
    event EventHandler<RepositoryItemChangedEventArgs<IconData>>? ItemsChanged;
    Task<IEnumerable<IconData>> SelectAll();
    Task<IconData?> Select(Guid id);
    Task<bool> Insert(IconData dockItem);
    Task<bool> Update(IconData dockItem);
    Task<bool> InsertOrUpdate(IconData dockItem);
    Task<bool> Delete(Guid id);
}