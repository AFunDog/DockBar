using DockBar.Core.Helpers;
using Serilog;
using DockItemBase = DockBar.DockItem.Structs.DockItemBase;
using DockLinkItem = DockBar.DockItem.Structs.DockLinkItem;

namespace DockBar.DockItem.Internals;

internal sealed partial class DockItemFactory : IDockItemFactory
{
    private ILogger Logger { get; }

    public DockItemFactory() : this(Log.Logger)
    {
    }

    public DockItemFactory(ILogger logger)
    {
        Logger = logger;
    }

    public DockItemBase Create(DockItemType type, string? showName = null, byte[]? iconData = null)
        => AttachServiceCore(type switch
        {
            DockItemType.LinkItem => new DockLinkItem() { ShowName = showName, IconData = iconData },
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        });

    public void AttachService(DockItemBase dockItem)
    {
        using var _ = LogHelper.Trace();
        Logger.Verbose("为 {Key} {ShowName} 附加服务",dockItem.Key,dockItem.ShowName);
        AttachServiceCore(dockItem);
    }


    private DockItemBase AttachServiceCore(DockItemBase dockItem)
    {
        dockItem.Logger = Logger;
        return dockItem;
    }
}