using System.Collections.Specialized;
using MessagePack;

namespace DockBar.DockItem.Structs;

[MessagePackObject(AllowPrivate = true)]
public sealed partial class DockItemFolder : DockItemBase, IDockItemGroup
{
    [Key(nameof(DockItems))]
    private Dictionary<int, int> DockItems { get; set; } = [];

    [IgnoreMember]
    public IReadOnlyList<int> ItemsKey => [.. DockItems.Keys];

    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    protected override void ExecuteCore() { }
}
