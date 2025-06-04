using System.Windows.Controls;
using DockBar.Core.Helpers;
using Serilog;
using DockItemBase = DockBar.DockItem.Items.DockItemBase;
using DockLinkItem = DockBar.DockItem.Items.DockLinkItem;

namespace DockBar.DockItem.Internals;

internal sealed partial class DockItemFactory : IDockItemFactory
{
    private ILogger Logger { get; }

    private IDockItemService DockItemService { get; }

    public DockItemFactory() : this(Log.Logger, IDockItemService.Empty)
    {
    }

    public DockItemFactory(ILogger logger, IDockItemService dockItemService)
    {
        Logger = logger;
        DockItemService = dockItemService;
    }

    public DockItemBase Create<T>(string? showName = null, byte[]? iconData = null) where T : DockItemBase, new()
        => AttachServiceCore(new T { ShowName = showName, IconData = iconData });

    public void AttachService(DockItemBase dockItem)
    {
        using var _ = LogHelper.Trace();
        Logger.Verbose("为 {Key} {ShowName} 附加服务", dockItem.Key, dockItem.ShowName);
        AttachServiceCore(dockItem);
    }


    private DockItemBase AttachServiceCore(DockItemBase dockItem)
    {
        dockItem.Logger = Logger;
        dockItem.DockItemService = DockItemService;
        return dockItem;
    }
}