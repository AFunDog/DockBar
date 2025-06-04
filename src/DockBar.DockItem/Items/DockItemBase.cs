using System.Windows.Markup;
using CommunityToolkit.Mvvm.ComponentModel;
using DockBar.Core.Helpers;
using DockBar.DockItem.Structs;
using MessagePack;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace DockBar.DockItem.Items;

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
    public ILogger Logger { get; internal set; } = Log.Logger;

    [IgnoreMember]
    public virtual IDockItemService DockItemService { get; internal set; } = IDockItemService.Empty;

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

    [ObservableProperty]
    [Key(nameof(ShowName))]
    public partial string? ShowName { get; set; }

    [ObservableProperty]
    [Key(nameof(IconData))]
    public partial byte[]? IconData { get; set; }


    /// <summary>
    /// 当前 <see cref="Execute"/> 是否可执行
    /// </summary>
    /// <remarks>
    /// 由子类决定是否通知改变 <see cref="INotifyPropertyChangedAttribute"/>
    /// </remarks>
    [IgnoreMember]
    public abstract bool CanExecute { get; protected set; }

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
        Logger.Verbose("DockItem {Key} 尝试执行", Key);
        if (CanExecute)
            try
            {
                ExecuteCore();
                Logger.Information("DockItem {Key} 执行完毕", Key);
                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e, "{Key} 尝试执行时出错", Key);
            }

        Logger.Warning("DockItem {Key} 不可执行", Key);
        return false;
    }

    protected abstract void ExecuteCore();
}