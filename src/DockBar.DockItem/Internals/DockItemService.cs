using System.Collections.ObjectModel;
using DockBar.Core.Helpers;
using DockBar.DockItem.Items;
using DockBar.DockItem.Structs;
using MessagePack;
using Serilog;
using DockItemBase = DockBar.DockItem.Items.DockItemBase;

namespace DockBar.DockItem.Internals;

internal sealed class DockItemService(ILogger logger) : IDockItemService
{
    private ILogger Logger { get; } = logger;
    // private IDockItemFactory DockItemFactory { get; } = dockItemFactory;

    /// <summary>
    /// 所有注册过的 <see cref="DockItemBase"/>
    /// </summary>
    private Dictionary<int, DockItemBase> RegisteredDockItemDict { get; } = [];

    public RootGroup Root { get; } = new();
    public IReadOnlyCollection<DockItemBase> RegisteredDockItems => RegisteredDockItemDict.Values;

    public event Action<IDockItemService, DockItemBase>? DockItemRegistered;
    public event Action<IDockItemService, DockItemBase>? DockItemExecuted;
    public event Action<IDockItemService, DockItemBase>? DockItemUnregistered;
    // public event Action<IDockItemService, (int oldIndex, int newIndex)>? DockItemMoved;

    public DockItemService() : this(Log.Logger)
    {
    }

    internal void RaiseDockItemStarted(DockItemBase dockItem)
    {
        using var _ = LogHelper.Trace();
        Logger.Debug("DockItem 启动 {Type} {Key} {ShowName}", dockItem.GetType(), dockItem.Key, dockItem.ShowName);
        DockItemExecuted?.Invoke(this, dockItem);
    }

    public DockItemBase? GetDockItem(int key) => RegisteredDockItemDict.GetValueOrDefault(key);

    public int RegisterDockItem(DockItemBase dockItem)
    {
        //using var _ = LogHelper.Trace();
        dockItem.Key = AllocNewKey();
        var res = RegisterDockItemCore(dockItem);
        if (res)
            return dockItem.Key;

        return -1;
        //Logger.Debug("注册 DockItem 成功 {Key} {DockItem}", dockItem.Key, dockItem);
    }

    private bool RegisterDockItemCore(DockItemBase dockItem)
    {
        using var _ = LogHelper.Trace();
        // DockItemFactory.AttachService(dockItem);

        dockItem.Logger = Logger;
        dockItem.DockItemService = this;

        var res = RegisteredDockItemDict.TryAdd(dockItem.Key, dockItem);
        if (res)
        {
            Logger.Verbose("注册 DockItem 成功 {DockItem} {Key}", dockItem, dockItem.Key);
            DockItemRegistered?.Invoke(this, dockItem);
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
        if (RegisteredDockItemDict.TryGetValue(key, out var dockItem) && RegisteredDockItemDict.Remove(key))
        {
            Root.Remove(dockItem.Key);
            // DockItemList.RemoveAt(dockItem.Index);
            // Root.Remove(key);
            // AllReIndexed();
            Logger.Debug("注销 DockItem 成功 {Key} {DockItem}", key, dockItem);
            DockItemUnregistered?.Invoke(this, dockItem);
            return true;
        }
        else
        {
            Logger.Warning("{key} 对应的 DockItem 不存在 跳过删除", key);
        }

        return false;
    }

    // public void MoveDockItemTo(int key, int index)
    // {
    //     using var _ = LogHelper.Trace();
    //     // 防止超出索引范围
    //     index = Math.Max(0, Math.Min(index, RegisteredDockItemDict.Count - 1));
    //     if (RegisteredDockItemDict.TryGetValue(key, out var dockItem))
    //     {
    //         if (dockItem.Index == index)
    //         {
    //             Logger.Warning("DockItem {Key} 已经在 {Index} 位置 跳过移动", key, index);
    //             return;
    //         }
    //
    //         var oldIndex = dockItem.Index;
    //         Root.Move(key, index);
    //         // DockItemList.RemoveAt(dockItem.Index);
    //         //index = Math.Max(0, Math.Min(index, DockItemTable.Keys.Count));
    //         // DockItemList.Insert(index, dockItem);
    //         // AllReIndexed();
    //         Logger.Verbose("DockItem {Key} 移动到 {Index} 位置", key, index);
    //         DockItemMoved?.Invoke(this, (oldIndex, dockItem.Index));
    //     }
    //     else
    //     {
    //         Logger.Warning("尝试移动 {key} 对应的 DockItem 不存在 跳过移动", key);
    //     }
    // }

    public bool ExecuteDockItem(int key)
    {
        if (RegisteredDockItemDict.TryGetValue(key, out var dockItem))
        {
            var res = dockItem.Execute();
            if (res)
                RaiseDockItemStarted(dockItem);

            return res;
        }

        return false;
    }

    public void ClearDockItems()
    {
        using var _ = LogHelper.Trace();
        var keys = RegisteredDockItemDict.Keys.ToArray();
        foreach (var key in keys)
            UnregisterDockItem(key);

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
            // var dockItems = MessagePackSerializer.Deserialize<DockItemBase[]>(fs);
            // foreach (var dockItem in dockItems)
            // {
            //     var res = RegisterDockItemCore(dockItem.Index, dockItem);
            //     if (res)
            //         DockItemAdded?.Invoke(this, dockItem);
            // }
            var data = MessagePackSerializer.Deserialize<DockItemData>(fs);
            foreach (var item in data.DockItems)
            {
                var res = RegisterDockItemCore(item);
                if (res)
                    DockItemRegistered?.Invoke(this, item);
            }

            foreach (var rootItem in data.RootItemKeys.Select(GetDockItem).OfType<DockItemBase>())
                // var item = GetDockItem(rootItemKey);
                // if (item is not null)
                // {
                //     Root.Insert(item);
                // }
                // Root.Add(rootItemKey);
                Root.Add(rootItem.Key);


            Logger.Information("从 {FilePath} 加载 DockItem 数据成功", filePath);
            Logger.Information(
                "加载 {Count} 条数据：{DockItems}",
                RegisteredDockItemDict.Count,
                RegisteredDockItemDict.Values.Select(d => d.GetType().Name)
            );
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
                file.Directory.Create();

            using var fs = File.OpenWrite(filePath);
            // 不转为 Array 会报错
            // MessagePack 在 AOT 下只能序列化数组，其他List、IEnumerable类型好像不可行
            // var items = DockItemTable.Values.OrderBy(d => d.Index).ToArray();
            // MessagePackSerializer.Serialize<DockItemBase[]>(fs, items);
            var data = new DockItemData(Root.ToArray(), RegisteredDockItemDict.Values.ToArray());
            MessagePackSerializer.Serialize<DockItemData>(fs, data);
            Logger.Information("保存 DockItem 数据到 {FilePath} 成功 共保存 {Count} 条数据", filePath, RegisteredDockItemDict.Count);
        }
        catch (Exception e)
        {
            Logger.Error(e, "保存 DockItem 数据到 {FilePath} 失败", filePath);
        }
    }

    // /// <summary>
    // /// 将列表中的所有元素的 <see cref="DockItemBase.Index"/> 重新编号
    // /// </summary>
    // private void AllReIndexed()
    // {
    //     for (int i = 0; i < DockItemList.Count; i++)
    //     {
    //         DockItemList[i].Index = i;
    //     }
    // }

    private int AllocNewKey()
    {
        var key = Random.Shared.Next();
        while (RegisteredDockItemDict.ContainsKey(key))
            key = Random.Shared.Next();

        return key;
    }
}