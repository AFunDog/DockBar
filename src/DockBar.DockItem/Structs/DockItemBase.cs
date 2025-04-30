using CommunityToolkit.Mvvm.ComponentModel;
using DockBar.Core.Helpers;
using MessagePack;
using Serilog;

namespace DockBar.DockItem.Structs;

[MessagePackObject(AllowPrivate = true)]
[Union(0, typeof(DockLinkItem))]
[Union(1, typeof(KeyActionDockItem))]
[Union(2, typeof(DockItemFolder))]
public abstract partial class DockItemBase : ObservableObject
{
    /// <summary>
    /// 基于 <see cref="Serilog"/> 的日志系统
    /// </summary>
    [IgnoreMember]
    public ILogger Logger { get; set; } = Log.Logger;

    /// <summary>
    /// 是否使用生成的图标
    /// </summary>
    [ObservableProperty]
    [Key(nameof(UseGeneratedIcon))]
    public partial bool UseGeneratedIcon { get; set; } = true;

    /// <summary>
    /// 是否使用生成的名称
    /// </summary>
    [ObservableProperty]
    [Key(nameof(UseGeneratedShowName))]
    public partial bool UseGeneratedShowName { get; set; } = true;

    [Key(nameof(Key))]
    public int Key { get; internal set; }

    // [ObservableProperty]
    [Key(nameof(Index))]
    public int Index { get; internal set; }

    [ObservableProperty]
    [Key(nameof(ShowName))]
    public partial string? ShowName { get; set; }

    [ObservableProperty]
    [Key(nameof(IconData))]
    public partial byte[]? IconData { get; set; }


    /// <summary>
    /// 当前 <see cref="Execute"/> 是否可执行
    /// </summary>
    [IgnoreMember]
    public bool CanExecute => CanExecuteCore;

    /// <summary>
    /// 由子类控制的是否可执行属性
    /// </summary>
    [ObservableProperty]
    [IgnoreMember]
    protected partial bool CanExecuteCore { get; set; }

    // 使用 internal 修饰符，使得只有 DockBar.DockItem 程序集内的类可以继承 DockItemBase 类
    internal DockItemBase()
    {
    }

    /// <summary>
    /// 执行 <see cref="DockItemBase"/>
    /// </summary>
    /// <remarks>
    /// 由 <see cref="IDockItemService"/> 执行
    /// </remarks>
    /// <returns></returns>
    internal bool Execute()
    {
        using var _ = LogHelper.Trace();
        Logger.Verbose("DockItem {Key} 尝试执行",Key);
        if (CanExecute)
        {
            try
            {
                ExecuteCore();
                Logger.Information("DockItem {Key} 执行完毕",Key);
                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e,"{Key} 尝试执行",Key);
            }
        }
        Logger.Warning("DockItem {Key} 不可执行",Key);
        return false;
    }

    protected abstract void ExecuteCore();
}