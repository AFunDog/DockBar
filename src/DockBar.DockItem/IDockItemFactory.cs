using DockBar.DockItem.Items;
using DockItemBase = DockBar.DockItem.Items.DockItemBase;
using DockLinkItem = DockBar.DockItem.Items.DockLinkItem;

namespace DockBar.DockItem;

public interface IDockItemFactory
{
    /// <summary>
    /// 创建一个新的 <see cref="DockItemBase"/> 
    /// </summary>
    /// <remarks>
    /// 通过此方法创建的 <see cref="DockItemBase"/> 会附带其所需要的外部服务
    /// </remarks>
    DockItemBase Create<T>(string? showName = null, byte[]? iconData = null) where T : DockItemBase, new();

    /// <summary>
    /// 为 <paramref name="dockItem"/> 附加外部服务
    /// 这适用于自己创建的 <see cref="DockItemBase"/>
    /// </summary>
    void AttachService(DockItemBase dockItem);

    public static IDockItemFactory Empty { get; } = new EmptyFactory();

    sealed class EmptyFactory : IDockItemFactory
    {
        public DockItemBase Create<T>(string? showName = null, byte[]? iconData = null) where T : DockItemBase, new()
            => new EmptyDockItem();

        public void AttachService(DockItemBase dockItem)
        {
        }
    }
}