using DockItemBase = DockBar.DockItem.Structs.DockItemBase;
using DockLinkItem = DockBar.DockItem.Structs.DockLinkItem;

namespace DockBar.DockItem;

public enum DockItemType
{
    LinkItem
}

public interface IDockItemFactory
{
    /// <summary>
    /// 创建一个新的 <see cref="DockItemBase"/> 
    /// </summary>
    /// <remarks>
    /// 通过此方法创建的 <see cref="DockItemBase"/> 会附带其所需要的外部服务
    /// </remarks>
    DockItemBase Create(DockItemType type, string? showName = null, byte[]? iconData = null);

    /// <summary>
    /// 为 <paramref name="dockItem"/> 附加外部服务
    /// 这适用于自己创建的 <see cref="DockItemBase"/>
    /// </summary>
    void AttachService(DockItemBase dockItem);
    
    public static IDockItemFactory Empty { get; } = new EmptyFactory();
    sealed class EmptyFactory : IDockItemFactory
    {
        public DockItemBase Create(DockItemType type, string? showName = null, byte[]? iconData = null)
        {
            return new DockLinkItem();
        }

        public void AttachService(DockItemBase dockItem)
        {
            
        }
    }
}