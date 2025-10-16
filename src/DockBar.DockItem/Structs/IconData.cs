using DockBar.DockItem.Contacts;
using MessagePack;

namespace DockBar.DockItem.Structs;

[MessagePackObject]
public partial record IconData : IRepositoryItem
{
    [Key(nameof(Id))]
    public required Guid Id { get; init; }
    
    
    [Key(nameof(Data))]
    public required byte[] Data { get; init; }
}
