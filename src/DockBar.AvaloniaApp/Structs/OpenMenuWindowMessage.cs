using Avalonia;
using DockBar.DockItem.Structs;

namespace DockBar.AvaloniaApp.Structs;

public record OpenMenuWindowMessage(
    DockItemData? SelectedDockItem,
    int SelectedIndex,
    PixelPoint Pos,
    bool CanAddItem = false);