using CommunityToolkit.Mvvm.ComponentModel;
using MessagePack;

namespace DockBar.DockItem.Items;

[MessagePackObject(AllowPrivate = true)]
public sealed partial class KeyActionDockItem : DockItemBase
{
    [ObservableProperty]
    [Key(nameof(ActionKey))]
    public partial string? ActionKey { get; set; }

    public override bool CanExecute { get; protected set; } = true;

    protected internal override bool Execute()
    {
        // 没有任何行为，所有行为都通过 ActionKey 标识符由外部程序决定
        return true;
    }
}