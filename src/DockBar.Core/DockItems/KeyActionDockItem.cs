using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using MessagePack;

namespace DockBar.Core.DockItems;

[MessagePackObject(AllowPrivate = true)]
public sealed partial class KeyActionDockItem : DockItemBase
{
    [ObservableProperty]
    [Key(3)]
    public partial string? ActionKey { get; set; }

    protected override void StartCore()
    {
        // 没有任何行为，所有行为都通过 ActionKey 标识符由外部程序决定
    }
}
