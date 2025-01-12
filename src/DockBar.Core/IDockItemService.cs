using DockBar.Core.DockItems;
using DockBar.Core.Internals;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace DockBar.Core;

public interface IDockItemService
{
    /// <summary>
    /// 停靠项目列表，停靠的项目会按照索引顺序排列
    /// </summary>
    IReadOnlyList<DockItemBase> DockItems { get; }

    event Action<IDockItemService, DockItemBase>? DockItemAdded;
    event Action<IDockItemService, DockItemBase>? DockItemRemoved;
    event Action<IDockItemService, (int oldIndex, int newIndex)>? DockItemMoved;
    event Action<IDockItemService, DockItemBase>? DockItemStarted;

    /// <summary>
    /// 获取指定 <paramref name="key"/> 的 <see cref="DockItemBase"/>
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    DockItemBase? GetDockItem(int key);

    /// <summary>
    /// 注册 <paramref name="dockItem"/>
    /// </summary>
    /// <remarks>
    /// 注册完后将<paramref name="dockItem"/> 插在 <see cref="DockItems"/> 的 <paramref name="index"/> 位置
    /// <br/>
    /// 会给 <paramref name="dockItem"/> 重新分配 <see cref="DockItemBase.Key"/>
    /// <br/>
    /// 并且触发 <see cref="DockItemAdded"/> 事件
    /// </remarks>
    /// <returns>
    /// 返回注册成功后的 <see cref="DockItemBase.Key"/>
    /// 如果注册失败返回 <see cref="int">-1</see>
    /// </returns>
    int RegisterDockItem(int index, DockItemBase dockItem);

    /// <summary>
    /// 注销指定 <paramref name="key"/> 的 <see cref="DockItemBase"/>
    /// </summary>
    /// <param name="key"></param>
    /// <returns>
    /// 返回是否注销成功
    /// </returns>
    bool UnregisterDockItem(int key);
    void MoveDockItemTo(int key, int index);
    void LoadData(string filePath);
    void SaveData(string filePath);
}
