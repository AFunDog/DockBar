using System.Collections.Specialized;

namespace DockBar.DockItem.Structs;

internal sealed class RootGroup : IDockItemGroup
{
    private Dictionary<int, int> DockItems { get; } = [];

    public IReadOnlyList<int> ItemsKey => [.. DockItems.Values];

    public event NotifyCollectionChangedEventHandler? CollectionChanged;
}
