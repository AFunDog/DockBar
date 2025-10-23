using DockBar.DockItem.Items;
using DockBar.DockItem.Structs;

namespace DockBar.DockItem.Contacts;

public partial class DockItemExecutedEventArgs : EventArgs
{
    public DockItemData DockItemData { get; }
    public bool Result { get; }

    public DockItemExecutedEventArgs(DockItemData dockItemData, bool result)
    {
        DockItemData = dockItemData;
        Result = result;
    }
}

public interface IDockItemService
{
    event EventHandler<DockItemExecutedEventArgs>? DockItemExecuted;
    void RegisterExecute(string type, Func<DockItemData, Task<bool>> execute);
    void RegisterValidMetadata(string type, IEnumerable<MetadataDescription> metadata);

    Task<DockItemData?> AddDockItem(
        string name,
        byte[] iconData,
        string parentPath,
        int index,
        string type,
        IReadOnlyDictionary<string, string> metadata);
    
    Task<DockItemData?> AddOrUpdateDockItem(DockItemData data, byte[]? iconData);
    
    IEnumerable<MetadataDescription> GetValidMetadata(string type);

    Task<bool> RemoveDockItem(Guid id);

    Task<bool> MoveDockItem(Guid id, string parentPath, int index);

    /// <summary>
    /// 执行 <see cref="DockItemData.Type"/> 对应的注册回调
    /// </summary>
    /// <param name="key">
    /// </param>
    /// <remarks>
    /// 如果执行成功，则会触发 <see cref="DockItemExecuted"/> 事件
    /// </remarks>
    /// <returns>
    /// 如果 <see cref="DockItemData"/> 不存在，那么返回 <see cref="bool">false</see>
    /// <br/>
    /// 如果 <see cref="DockItemData"/>
    /// </returns>
    Task<bool> ExecuteDockItem(Guid key);

}