using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DockBar.Core.Internals;
using Mapster;
using MessagePack;

namespace DockBar.Core.DockItems;

[MessagePackObject]
[Union(0, typeof(DockLinkItem))]
[Union(1, typeof(WrappedDockItem))]
[Union(2, typeof(KeyActionDockItem))]
public abstract partial class DockItemBase : ObservableObject
{
    // 使用 internal 修饰符，使得只有 DockBar.Core 程序集内的类可以继承 DockItemBase 类
    internal DockItemBase() { }

    //public event Action<IDockItemService?, DockItemBase>? Started;

    [IgnoreMember]
    internal virtual DockItemService? OwnerService { get; set; }

    [Key(0)]
    public int Key { get; internal set; }

    [ObservableProperty]
    [Key(1)]
    public partial string? ShowName { get; set; }

    [ObservableProperty]
    [Key(2)]
    public partial byte[]? IconData { get; set; }

    public void Start()
    {
        StartCore();
        OwnerService?.RaiseDockItemStarted(this);
    }

    protected abstract void StartCore();
}
