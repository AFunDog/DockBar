using System.Collections.Specialized;

namespace DockBar.DockItem.Structs;

public interface IDockItemGroup : INotifyCollectionChanged
{
    IReadOnlyList<int> ItemsKey { get; }
}
