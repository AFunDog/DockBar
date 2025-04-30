using DockItemBase = DockBar.DockItem.Structs.DockItemBase;

namespace DockBar.DockItem;

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

    /// <summary>
    /// 移动指定 <paramref name="key"/> 的 <see cref="DockItemBase"/> 到 <paramref name="index"/> 位置处
    /// </summary>
    /// <param name="key"></param>
    /// <param name="index"></param>
    void MoveDockItemTo(int key, int index);

    /// <summary>
    /// 执行 <see cref="DockItemBase"/>
    /// </summary>
    /// <param name="key">
    /// <see cref="DockItemBase"/> 的 <see cref="DockItemBase.Key"/> 属性
    /// </param>
    /// <returns>
    /// 如果 <see cref="DockItemBase"/> 不存在，那么返回 <see cref="bool">false</see>
    /// <br/>
    /// 如果 <see cref="DockItemBase"/>
    /// </returns>
    bool ExecuteDockItem(int key);
    
    /// <summary>
    /// 加载本地项目数据
    /// </summary>
    /// <param name="filePath"></param>
    void LoadData(string filePath);

    /// <summary>
    /// 保存项目数据到本地
    /// </summary>
    /// <param name="filePath"></param>
    void SaveData(string filePath);

    /// <summary>
    /// 默认空服务
    /// </summary>
    public static IDockItemService Empty { get; } = new EmptyService();

    sealed class EmptyService : IDockItemService
    {
        public IReadOnlyList<DockItemBase> DockItems => [];
        public event Action<IDockItemService, DockItemBase>? DockItemAdded;
        public event Action<IDockItemService, DockItemBase>? DockItemRemoved;
        public event Action<IDockItemService, (int oldIndex, int newIndex)>? DockItemMoved;
        public event Action<IDockItemService, DockItemBase>? DockItemStarted;

        public DockItemBase? GetDockItem(int key) => null;

        public int RegisterDockItem(int index, DockItemBase dockItem) => -1;

        public bool UnregisterDockItem(int key) => false;

        public void MoveDockItemTo(int key, int index) { }
        public bool ExecuteDockItem(int key)
        {
            return false;
        }

        public void LoadData(string filePath) { }

        public void SaveData(string filePath) { }
    }
}
