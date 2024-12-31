using DockBar.Core.DockItems;

namespace DockBar.Core.Structs;

public enum DockItemChangeType
{
    Add,
    Remove
}

public class DockItemChangedEventArgs : EventArgs
{
    public required DockItemBase DockItem { get; init; }
    public required DockItemChangeType ChangeType { get; init; }
}
