using CommunityToolkit.Mvvm.ComponentModel;
using MessagePack;

namespace DockBar.DockItem.Structs;

[MessagePackObject(AllowPrivate = true)]
public sealed partial class KeyActionDockItem : DockItemBase
{
    [ObservableProperty]
    [Key(nameof(ActionKey))]
    public partial string? ActionKey { get; set; }

    protected override void ExecuteCore()
    {
        // 没有任何行为，所有行为都通过 ActionKey 标识符由外部程序决定
    }
}
