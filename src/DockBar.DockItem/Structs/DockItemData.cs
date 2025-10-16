using CommunityToolkit.Mvvm.ComponentModel;
using DockBar.DockItem.Contacts;
using MessagePack;

namespace DockBar.DockItem.Structs;

[MessagePackObject]
public partial record DockItemData : IRepositoryItem
{
    [Key(nameof(Id))]
    public required Guid Id { get; init; }

    [Key(nameof(Name))]
    public required string Name { get; init; }

    [Key(nameof(IconId))]
    public required Guid IconId { get; init; }
    
    [Key(nameof(Type))]
    public required string Type { get; init; }
    
    [Key(nameof(ParentPath))]
    public required string ParentPath { get; init; }

    [Key(nameof(Index))]
    public required int Index { get; init; }

    [Key(nameof(Metadata))]
    public required DockItemMetadata Metadata { get; init; }
}

