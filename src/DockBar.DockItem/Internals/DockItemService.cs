// using System.Collections.ObjectModel;
// using DockBar.Core.Helpers;
// using DockBar.DockItem.Contacts;
// using DockBar.DockItem.Items;
// using DockBar.DockItem.Structs;
// using MessagePack;
// using Microsoft.Extensions.DependencyInjection;
// using Serilog;
// using Zeng.CoreLibrary.Toolkit.Logging;
// using DockItemBase = DockBar.DockItem.Items.DockItemBase;
//

using System.ComponentModel.DataAnnotations;
using System.Drawing;
using DockBar.DockItem.Contacts;
using DockBar.DockItem.Items;
using DockBar.DockItem.Structs;
using Serilog;
using Zeng.CoreLibrary.Toolkit.Logging;

namespace DockBar.DockItem.Internals;

internal sealed partial class DockItemService : IDockItemService
{
    private ILogger Logger { get; }
    private DockItemCoreExecutor CoreExecutor { get; }
    private IRepository<DockItemData> DockItemDataRepo { get; }
    private IRepository<IconData> IconDataRepo { get; }


    public event EventHandler<DockItemExecutedEventArgs>? DockItemExecuted;

    private Dictionary<string, Func<DockItemData, Task<bool>>> ExecuteTable { get; } = [];
    private Dictionary<string, IEnumerable<MetadataDescription>> MetadataDescTable { get; } = [];

    public DockItemService(
        ILogger logger,
        DockItemCoreExecutor coreExecutor,
        IRepository<DockItemData> dockItemDataRepo,
        IRepository<IconData> iconDataRepo)
    {
        Logger = logger.ForContext<DockItemService>();
        CoreExecutor = coreExecutor;
        DockItemDataRepo = dockItemDataRepo;
        IconDataRepo = iconDataRepo;

        // 注册 Link 类型
        RegisterExecute("Link", CoreExecutor.ExecuteLink);
        RegisterValidMetadata(
            "Link",
            [
                new MetadataDescription()
                {
                    Key = "LinkType",
                    Type = MetadataTypeEnum.Enum,
                    Validations = [new RequiredAttribute(), new AllowedValuesAttribute(["Lnk", "File"])]
                },
                new MetadataDescription()
                {
                    Key = "LinkPath", Type = MetadataTypeEnum.String, Validations = [new RequiredAttribute()]
                }
            ]
        );
    }

    public void RegisterExecute(string type, Func<DockItemData, Task<bool>> execute)
    {
        ExecuteTable[type] = execute;
    }

    public void RegisterValidMetadata(string type, IEnumerable<MetadataDescription> metadata)
    {
        // TODO metadata Key 查重
        MetadataDescTable[type] = metadata;
    }

    public async Task<DockItemData?> AddDockItem(
        string name,
        byte[] iconData,
        string parentPath,
        int index,
        string type,
        IReadOnlyDictionary<string, string> metadata)
    {
        var iconDataRec = new IconData() { Id = Ulid.NewUlid().ToGuid(), Data = iconData };
        var dockItemData = new DockItemData()
        {
            Id = Ulid.NewUlid().ToGuid(),
            IconId = iconDataRec.Id,
            Name = name,
            Type = type,
            ParentPath = parentPath,
            Index = index,
            Metadata = new(metadata)
        };

        // var dockItemPathData = new DockItemPathData()
        // {
        //     Id = Ulid.NewUlid().ToGuid(),
        //     ItemId = dockItemData.Id,
        //     ParentPath = parentPath,
        //     Index = index
        // };
        var res = await DockItemDataRepo.Insert(dockItemData) && await IconDataRepo.Insert(iconDataRec);
        var o = (await DockItemDataRepo.Select(dockItemData.Id));
        if (o is null)
            return null;
        var nLevel = (await DockItemDataRepo.SelectAll())
            .Where(x => x.ParentPath == parentPath && x.Id != dockItemData.Id)
            .OrderBy(x => x.Index == -1 ? int.MaxValue : x.Index)
            .Select(x => x.Id)
            .ToList();
        nLevel.Insert(index == -1 ? nLevel.Count : index, dockItemData.Id);
        await DockItemDataRepo.Update(
            x => x.ParentPath == parentPath && nLevel.Contains(x.Id),
            x => x with { Index = nLevel.IndexOf(x.Id) }
        );
        return dockItemData;
    }

    public async Task<DockItemData?> AddOrUpdateDockItem(DockItemData data, byte[]? iconData)
    {
        if (iconData is not null)
        {
            var iconDataRec = new IconData() { Id = Ulid.NewUlid().ToGuid(), Data = iconData };
            var res = await IconDataRepo.Insert(iconDataRec);
            if (res)
            {
                data = data with { IconId = iconDataRec.Id };
            }
        }

        return await DockItemDataRepo.InsertOrUpdate(data) ? data : null;
    }

    public IEnumerable<MetadataDescription> GetValidMetadata(string type)
    {
        if (MetadataDescTable.TryGetValue(type, out var value))
        {
            return value;
        }

        return [];
    }

    public Task<bool> RemoveDockItem(Guid id)
    {
        return DockItemDataRepo.Delete(id);
    }

    public async Task<bool> MoveDockItem(Guid id, string parentPath = "/", int index = -1)
    {
        // if (await DockItemPathDataRepo.Select(key) is {} pathData)
        // {
        //     
        // }
        var o = (await DockItemDataRepo.Select(id));
        if (o is null)
            return false;
        var nLevel = (await DockItemDataRepo.SelectAll())
            .Where(x => x.ParentPath == parentPath && x.Id != id)
            .OrderBy(x => x.Index == -1 ? int.MaxValue : x.Index)
            .Select(x => x.Id)
            .ToList();
        nLevel.Insert(index == -1 ? nLevel.Count : index, id);
        var res = await DockItemDataRepo.Update(
            x => x.ParentPath == parentPath && nLevel.Contains(x.Id),
            x => x with { Index = nLevel.IndexOf(x.Id) }
        );
        if (res != 0 && string.IsNullOrEmpty(o.ParentPath) is false)
        {
            await ReIndex(o.ParentPath);

            // if (oParentPath != parentPath)
            // {
            //     await ReIndex(parentPath);
            // }
        }

        return res != 0;
    }

    private async Task ReIndex(string path)
    {
        var level = new OrderedDictionary<Guid, DockItemData>(
            (await DockItemDataRepo.SelectAll())
            .Where(x => x.ParentPath == path)
            .OrderBy(x => x.Index == -1 ? int.MaxValue : x.Index)
            .Select(x => new KeyValuePair<Guid, DockItemData>(x.Id, x))
            .ToArray()
        );

        for (var i = 0; i < level.Count; i++)
        {
            await DockItemDataRepo.Update(x => level.ContainsKey(x.Id), x => x with { Index = level.IndexOf(x.Id) });
        }
    }

    public async Task<bool> ExecuteDockItem(Guid key)
    {
        try
        {
            if (await DockItemDataRepo.Select(key) is { } dockItem)
            {
                if (ExecuteTable.TryGetValue(dockItem.Type, out var func))
                {
                    var res = await func.Invoke(dockItem);
                    DockItemExecuted?.Invoke(this, new DockItemExecutedEventArgs(dockItem, res));
                    return res;
                }
            }
        }
        catch (Exception e)
        {
            Logger.Trace().Error(e, "");
        }


        return false;
    }
}

// internal sealed class DockItemService : IDockItemService
// {
//     private IServiceProvider ServiceProvider { get; }
//     private ILogger Logger { get; }
//     // private IDockItemFactory DockItemFactory { get; } = dockItemFactory;
//
//     /// <summary>
//     /// 所有注册过的 <see cref="DockItemBase"/>
//     /// </summary>
//     private Dictionary<int, DockItemBase> RegisteredDockItemDict { get; } = [];
//
//     public RootGroup Root { get; } = new();
//     public IReadOnlyCollection<DockItemBase> RegisteredDockItems => RegisteredDockItemDict.Values;
//
//     public event Action<IDockItemService, DockItemBase>? DockItemCreated;
//     public event Action<IDockItemService, DockItemBase>? DockItemExecuted;
//     public event Action<IDockItemService, DockItemBase>? DockItemRemoved;
//     // public event Action<IDockItemService, (int oldIndex, int newIndex)>? DockItemMoved;
//
//     public DockItemService()
//     {
//     }
//
//     public DockItemService(IServiceProvider provider,ILogger logger)
//     {
//         ServiceProvider = provider;
//         Logger = logger;
//     }
//
//     internal void RaiseDockItemStarted(DockItemBase dockItem)
//     {
//         using var _ = LogHelper.Trace();
//         Logger.Debug("DockItem 启动 {Type} {Key} {ShowName}", dockItem.GetType(), dockItem.Key, dockItem.ShowName);
//         DockItemExecuted?.Invoke(this, dockItem);
//     }
//
//     public DockItemBase? GetDockItem(int key) => RegisteredDockItemDict.GetValueOrDefault(key);
//
//
//     public int RegisterDockItem(DockItemBase dockItem)
//     {
//         //using var _ = LogHelper.Trace();
//         dockItem.Key = AllocNewKey();
//         var res = RegisterDockItemCore(dockItem);
//         if (res)
//             return dockItem.Key;
//
//         return -1;
//         //Logger.Debug("注册 DockItem 成功 {Key} {DockItem}", dockItem.Key, dockItem);
//     }
//
//     private bool RegisterDockItemCore(DockItemBase dockItem)
//     {
//         using var _ = LogHelper.Trace();
//         // DockItemFactory.AttachService(dockItem);
//
//         // dockItem.Logger = Logger;
//         // dockItem.DockItemService = this;
//
//         var res = RegisteredDockItemDict.TryAdd(dockItem.Key, dockItem);
//         if (res)
//         {
//             Logger.Verbose("注册 DockItem 成功 {DockItem} {Key}", dockItem, dockItem.Key);
//             DockItemCreated?.Invoke(this, dockItem);
//         }
//         else
//         {
//             Logger.Warning("{DockItem} {Key} 已经被注册 跳过注册", dockItem, dockItem.Key);
//         }
//
//         return res;
//     }
//
//     public bool RemoveDockItem(int key)
//     {
//         using var _ = LogHelper.Trace();
//         if (RegisteredDockItemDict.TryGetValue(key, out var dockItem) && RegisteredDockItemDict.Remove(key))
//         {
//             Root.Remove(dockItem.Key);
//             // DockItemList.RemoveAt(dockItem.Index);
//             // Root.Remove(key);
//             // AllReIndexed();
//             Logger.Debug("注销 DockItem 成功 {Key} {DockItem}", key, dockItem);
//             DockItemRemoved?.Invoke(this, dockItem);
//             return true;
//         }
//         else
//         {
//             Logger.Warning("{key} 对应的 DockItem 不存在 跳过删除", key);
//         }
//
//         return false;
//     }
//
//     // public void MoveDockItemTo(int key, int index)
//     // {
//     //     using var _ = LogHelper.Trace();
//     //     // 防止超出索引范围
//     //     index = Math.Max(0, Math.Min(index, RegisteredDockItemDict.Count - 1));
//     //     if (RegisteredDockItemDict.TryGetValue(key, out var dockItem))
//     //     {
//     //         if (dockItem.Index == index)
//     //         {
//     //             Logger.Warning("DockItem {Key} 已经在 {Index} 位置 跳过移动", key, index);
//     //             return;
//     //         }
//     //
//     //         var oldIndex = dockItem.Index;
//     //         Root.Move(key, index);
//     //         // DockItemList.RemoveAt(dockItem.Index);
//     //         //index = Math.Max(0, Math.Min(index, DockItemTable.Keys.Count));
//     //         // DockItemList.Insert(index, dockItem);
//     //         // AllReIndexed();
//     //         Logger.Verbose("DockItem {Key} 移动到 {Index} 位置", key, index);
//     //         DockItemMoved?.Invoke(this, (oldIndex, dockItem.Index));
//     //     }
//     //     else
//     //     {
//     //         Logger.Warning("尝试移动 {key} 对应的 DockItem 不存在 跳过移动", key);
//     //     }
//     // }
//
//     public bool ExecuteDockItem(int key)
//     {
//         if (RegisteredDockItemDict.TryGetValue(key, out var dockItem))
//         {
//             var res = dockItem.Execute();
//             if (res)
//                 RaiseDockItemStarted(dockItem);
//             return res;
//         }
//
//         return false;
//     }
//
//     public void ClearDockItems()
//     {
//         using var _ = LogHelper.Trace();
//         var keys = RegisteredDockItemDict.Keys.ToArray();
//         foreach (var key in keys)
//             RemoveDockItem(key);
//
//         Logger.Trace().Information("清空 DockItem 完毕");
//     }
//
//     public void LoadData(string filePath)
//     {
//         using var _ = LogHelper.Trace();
//         if (!File.Exists(filePath))
//         {
//             Logger.Trace().Warning("本地文件 {FilePath} 不存在 跳过加载", filePath);
//             return;
//         }
//
//         try
//         {
//             using var fs = File.OpenRead(filePath);
//             // var dockItems = MessagePackSerializer.Deserialize<DockItemBase[]>(fs);
//             // foreach (var dockItem in dockItems)
//             // {
//             //     var res = RegisterDockItemCore(dockItem.Index, dockItem);
//             //     if (res)
//             //         DockItemAdded?.Invoke(this, dockItem);
//             // }
//             var data = MessagePackSerializer.Deserialize<DockItemData>(fs);
//             foreach (var item in data.DockItems)
//             {
//                 var res = RegisterDockItemCore(item);
//                 if (res)
//                     DockItemCreated?.Invoke(this, item);
//             }
//
//             foreach (var rootItem in data.RootItemKeys.Select(GetDockItem).OfType<DockItemBase>())
//                 // var item = GetDockItem(rootItemKey);
//                 // if (item is not null)
//                 // {
//                 //     Root.Insert(item);
//                 // }
//                 // Root.Add(rootItemKey);
//                 Root.Add(rootItem.Key);
//
//
//             Logger.Trace().Information("从 {FilePath} 加载 DockItem 数据成功", filePath);
//             Logger.Trace().Information(
//                 "加载 {Count} 条数据：{DockItems}",
//                 RegisteredDockItemDict.Count,
//                 RegisteredDockItemDict.Values.Select(d => d.GetType().Name)
//             );
//         }
//         catch (Exception e)
//         {
//             Logger.Trace().Error(e, "从 {FilePath} 加载 DockItem 数据失败", filePath);
//         }
//     }
//
//     public void SaveData(string filePath)
//     {
//         using var _ = LogHelper.Trace();
//         try
//         {
//             var file = new FileInfo(filePath);
//             if (file.Directory is not null && file.Directory.Exists is false)
//                 file.Directory.Create();
//
//             using var fs = File.OpenWrite(filePath);
//             // 不转为 Array 会报错
//             // MessagePack 在 AOT 下只能序列化数组，其他List、IEnumerable类型好像不可行
//             // var items = DockItemTable.Values.OrderBy(d => d.Index).ToArray();
//             // MessagePackSerializer.Serialize<DockItemBase[]>(fs, items);
//             var data = new DockItemData(Root.ToArray(), RegisteredDockItemDict.Values.ToArray());
//             MessagePackSerializer.Serialize<DockItemData>(fs, data);
//             Logger.Information("保存 DockItem 数据到 {FilePath} 成功 共保存 {Count} 条数据", filePath, RegisteredDockItemDict.Count);
//         }
//         catch (Exception e)
//         {
//             Logger.Error(e, "保存 DockItem 数据到 {FilePath} 失败", filePath);
//         }
//     }
//
//     // /// <summary>
//     // /// 将列表中的所有元素的 <see cref="DockItemBase.Index"/> 重新编号
//     // /// </summary>
//     // private void AllReIndexed()
//     // {
//     //     for (int i = 0; i < DockItemList.Count; i++)
//     //     {
//     //         DockItemList[i].Index = i;
//     //     }
//     // }
//
//     private int AllocNewKey()
//     {
//         var key = Random.Shared.Next();
//         while (RegisteredDockItemDict.ContainsKey(key))
//             key = Random.Shared.Next();
//
//         return key;
//     }
// }