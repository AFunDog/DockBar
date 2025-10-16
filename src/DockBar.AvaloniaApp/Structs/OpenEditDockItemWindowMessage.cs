using CommunityToolkit.Mvvm.Messaging.Messages;
using DockBar.DockItem.Structs;

namespace DockBar.AvaloniaApp.Structs;

public class OpenEditDockItemWindowMessage : AsyncRequestMessage<EditDockItemWindowReply?>
{
    public bool IsAddMode { get; set; }
    public DockItemData? BaseDockItem { get; set; }
    public byte[]? BaseIcon { get; set; }
}

public record EditDockItemWindowReply(DockItemData? DockItemData, byte[]? IconData);