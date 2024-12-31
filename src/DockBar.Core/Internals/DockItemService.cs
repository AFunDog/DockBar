using System.ComponentModel;
using DockBar.Core.DockItems;
using DockBar.Core.Structs;
using MessagePack;
using Serilog;

namespace DockBar.Core.Internals;

internal sealed class DockItemService : IDockItemService
{
    private ILogger Logger { get; set; } = Log.Logger;
    private Dictionary<int, DockItemBase> DockItemTable { get; } = [];

    public IEnumerable<DockItemBase> DockItems => DockItemTable.Values;

    public event Action<IDockItemService, DockItemChangedEventArgs>? DockItemChanged;

    public DockItemService() { }

    // 如果有外部的 Logger 则会使用外部的 Logger
    public DockItemService(ILogger logger)
    {
        Logger = logger;
    }

    public DockItemBase? GetDockItem(int key)
    {
        if (DockItemTable.TryGetValue(key, out var dockItem))
        {
            return dockItem;
        }
        return null;
    }

    public void RegisterDockItem(DockItemBase dockItem)
    {
        dockItem.Key = AllocNewKey();
        DockItemTable[dockItem.Key] = dockItem;
        DockItemChanged?.Invoke(this, new DockItemChangedEventArgs { DockItem = dockItem, ChangeType = DockItemChangeType.Add });
        Logger.Debug("注册 DockItem 成功 {Key} {DockItem}", dockItem.Key, dockItem);
    }

    public void UnregisterDockItem(int key)
    {
        if (DockItemTable.TryGetValue(key, out var dockItem) && DockItemTable.Remove(key))
        {
            DockItemChanged?.Invoke(this, new DockItemChangedEventArgs { DockItem = dockItem, ChangeType = DockItemChangeType.Remove });
            dockItem.Dispose();
            Logger.Debug("删除 DockItem 成功 {Key} {DockItem}", key, dockItem);
        }
        else
        {
            Logger.Warning("{key} 对应的 DockItem 不存在 跳过删除", key);
        }
    }

    public void ClearDockItems()
    {
        var keys = DockItemTable.Keys.ToArray();
        foreach (var key in keys)
        {
            UnregisterDockItem(key);
        }
        Logger.Information("清空 DockItem 完毕");
    }

    public void LoadData(string filePath)
    {
        Logger.Information("开始从 {FilePath} 加载 DockItem 数据", filePath);
        if (!File.Exists(filePath))
        {
            Logger.Warning("本地文件 {FilePath} 不存在 跳过加载", filePath);
            return;
        }
        try
        {
            var data = MessagePackSerializer.Typeless.Deserialize(File.ReadAllBytes(filePath));
            if (data is not IEnumerable<DockItemBase> dockItems)
                return;
            foreach (var dockItem in dockItems)
            {
                var res = DockItemTable.TryAdd(dockItem.Key, dockItem);
                if (res is false)
                    Logger.Warning("{Key} 已经被注册 跳过", dockItem.Key);
                else
                    DockItemChanged?.Invoke(
                        this,
                        new DockItemChangedEventArgs { DockItem = dockItem, ChangeType = DockItemChangeType.Add }
                    );
            }
            Logger.Information("从 {FilePath} 加载 DockItem 数据成功", filePath);
        }
        catch (Exception e)
        {
            Logger.Error(e, "从 {FilePath} 加载 DockItem 数据失败", filePath);
        }
    }

    public void SaveData(string filePath)
    {
        Logger.Information("开始保存 DockItem 数据到 {filePath}", filePath);
        try
        {
            using var fs = File.OpenWrite(filePath);
            // 不转为 Array 会报错
            MessagePackSerializer.Typeless.Serialize(fs, DockItemTable.Values.ToArray());
            Logger.Information("保存 DockItem 数据到 {FilePath} 成功", filePath);
        }
        catch (Exception e)
        {
            Logger.Error(e, "保存 DockItem 数据到 {FilePath} 失败", filePath);
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
