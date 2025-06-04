using DockBar.DockItem.Items;
using MessagePack;

namespace DockBar.DockItem.Structs;

[MessagePackObject(true)]
public record DockItemData(int[] RootItemKeys, DockItemBase[] DockItems);