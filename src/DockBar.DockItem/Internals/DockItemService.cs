using DockBar.Core.Helpers;
using MessagePack;
using Serilog;
using DockItemBase = DockBar.DockItem.Structs.DockItemBase;

namespace DockBar.DockItem.Internals;

internal sealed class DockItemService(ILogger logger, IDockItemFactory dockItemFactory) : IDockItemService
{
    private ILogger Logger { get; } = logger;
    private IDockItemFactory DockItemFactory { get; } = dockItemFactory;

    private Dictionary<int, DockItemBase> DockItemTable { get; } = [];
    private List<DockItemBase> DockItemList { get; } = [];

    public IReadOnlyList<DockItemBase> DockItems => DockItemList;

    public event Action<IDockItemService, DockItemBase>? DockItemAdded;
    public event Action<IDockItemService, DockItemBase>? DockItemStarted;
    public event Action<IDockItemService, DockItemBase>? DockItemRemoved;
    public event Action<IDockItemService, (int oldIndex, int newIndex)>? DockItemMoved;

    public DockItemService()
        : this(Log.Logger,IDockItemFactory.Empty)
    {
    }

    internal void RaiseDockItemStarted(DockItemBase dockItem)
    {
        using var _ = LogHelper.Trace();
        Logger.Debug("DockItem 启动 {Type} {Key} {ShowName}", dockItem.GetType(), dockItem.Key, dockItem.ShowName);
        DockItemStarted?.Invoke(this, dockItem);
    }

    public DockItemBase? GetDockItem(int key)
    {
        return DockItemTable.GetValueOrDefault(key);
    }

    public int RegisterDockItem(int index, DockItemBase dockItem)
    {
        //using var _ = LogHelper.Trace();
        dockItem.Key = AllocNewKey();
        RegisterDockItemCore(index, dockItem);
        return dockItem.Key;
        //Logger.Debug("注册 DockItem 成功 {Key} {DockItem}", dockItem.Key, dockItem);
    }

    private bool RegisterDockItemCore(int index, DockItemBase dockItem)
    {
        using var _ = LogHelper.Trace();
        DockItemFactory.AttachService(dockItem);
        var res = DockItemTable.TryAdd(dockItem.Key, dockItem);
        DockItemList.Insert(Math.Max(0, Math.Min(index, DockItemList.Count)), dockItem);
        AllReIndexed();
        DockItemAdded?.Invoke(this, dockItem);
        if (res)
        {
            Logger.Verbose("注册 DockItem 成功 {DockItem} {Key}", dockItem, dockItem.Key);
        }
        else
        {
            Logger.Warning("{DockItem} {Key} 已经被注册 跳过注册", dockItem, dockItem.Key);
        }

        return res;
    }

    public bool UnregisterDockItem(int key)
    {
        using var _ = LogHelper.Trace();
        if (DockItemTable.TryGetValue(key, out var dockItem) && DockItemTable.Remove(key))
        {
            DockItemList.RemoveAt(dockItem.Index);
            DockItemRemoved?.Invoke(this, dockItem);
            AllReIndexed();
            Logger.Debug("删除 DockItem 成功 {Key} {DockItem}", key, dockItem);
            return true;
        }
        else
        {
            Logger.Warning("{key} 对应的 DockItem 不存在 跳过删除", key);
        }

        return false;
    }

    public void MoveDockItemTo(int key, int index)
    {
        using var _ = LogHelper.Trace();
        // 防止超出索引范围
        index = Math.Max(0, Math.Min(index, DockItemTable.Count - 1));
        if (DockItemTable.TryGetValue(key, out var dockItem))
        {
            if (dockItem.Index == index)
            {
                Logger.Warning("DockItem {Key} 已经在 {Index} 位置 跳过移动", key, index);
                return;
            }

            var oldIndex = dockItem.Index;
            DockItemList.RemoveAt(dockItem.Index);
            //index = Math.Max(0, Math.Min(index, DockItemTable.Keys.Count));
            DockItemList.Insert(index, dockItem);
            AllReIndexed();
            Logger.Verbose("DockItem {Key} 移动到 {Index} 位置", key, index);
            DockItemMoved?.Invoke(this, (oldIndex, dockItem.Index));
        }
        else
        {
            Logger.Warning("尝试移动 {key} 对应的 DockItem 不存在 跳过移动", key);
        }
    }

    public bool ExecuteDockItem(int key)
    {
        if (DockItemTable.TryGetValue(key, out var dockItem))
        {
            return dockItem.Execute();;
        }

        return false;
    }

    public void ClearDockItems()
    {
        using var _ = LogHelper.Trace();
        var keys = DockItemTable.Keys.ToArray();
        foreach (var key in keys)
        {
            UnregisterDockItem(key);
        }

        Logger.Information("清空 DockItem 完毕");
    }

    public void LoadData(string filePath)
    {
        using var _ = LogHelper.Trace();
        if (!File.Exists(filePath))
        {
            Logger.Warning("本地文件 {FilePath} 不存在 跳过加载", filePath);
            return;
        }

        try
        {
            using var fs = File.OpenRead(filePath);
            var dockItems = MessagePackSerializer.Deserialize<DockItemBase[]>(fs);
            //if (data is not IEnumerable<DockItemBase> dockItems)
            //    return;
            foreach (var dockItem in dockItems)
            {
                var res = RegisterDockItemCore(dockItem.Index, dockItem);
                if (res)
                    DockItemAdded?.Invoke(this, dockItem);
            }

            Logger.Information("从 {FilePath} 加载 DockItem 数据成功", filePath);
            Logger.Information("加载 {Count} 条数据：{DockItems}", DockItemTable.Count, DockItemTable.Values.Select(d => d.GetType().Name));
        }
        catch (Exception e)
        {
            Logger.Error(e, "从 {FilePath} 加载 DockItem 数据失败", filePath);
        }
    }

    public void SaveData(string filePath)
    {
        using var _ = LogHelper.Trace();
        try
        {
            var file = new FileInfo(filePath);
            if (file.Directory is not null && file.Directory.Exists is false)
            {
                file.Directory.Create();
            }

            using var fs = File.OpenWrite(filePath);
            // 不转为 Array 会报错
            // MessagePack 在 AOT 下只能序列化数组，其他List、IEnumerable类型好像不可行
            var items = DockItemTable.Values.OrderBy(d => d.Index).ToArray();
            MessagePackSerializer.Serialize<DockItemBase[]>(fs, items);
            Logger.Information("保存 DockItem 数据到 {FilePath} 成功 共保存 {Count} 条数据", filePath, DockItemTable.Count);
        }
        catch (Exception e)
        {
            Logger.Error(e, "保存 DockItem 数据到 {FilePath} 失败", filePath);
        }
    }

    /// <summary>
    /// 将列表中的所有元素的 <see cref="DockItemBase.Index"/> 重新编号
    /// </summary>
    private void AllReIndexed()
    {
        for (int i = 0; i < DockItemList.Count; i++)
        {
            DockItemList[i].Index = i;
        }
    }

    private int AllocNewKey()
    {
        int key = Random.Shared.Next();
        while (DockItemTable.ContainsKey(key))
        {
            key = Random.Shared.Next();
        }

        return key;
    }
}