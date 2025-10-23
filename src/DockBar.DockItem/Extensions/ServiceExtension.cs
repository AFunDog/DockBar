using DockBar.DockItem.Contacts;
using DockBar.DockItem.Internals;
using DockBar.DockItem.Items;
using DockBar.DockItem.Structs;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace DockBar.DockItem.Extensions;

public static class ServiceExtension
{
    /// <summary>
    /// 添加单例 <see cref="IDockItemService"/> 和单例 <see cref="IDockItemFactory"/> 服务
    /// </summary>
    public static IServiceCollection UseDockItem(this IServiceCollection collection)
    {
        collection
            // .AddKeyedTransient<DockItemBase, DockLinkItem>(nameof(DockLinkItem))
            // .AddKeyedTransient<DockItemBase, DockItemFolder>(nameof(DockItemFolder))
            // .AddKeyedTransient<DockItemBase, KeyActionDockItem>(nameof(KeyActionDockItem))
            // .AddTransient<DockLinkItem>()
            // .AddTransient<DockItemFolder>()
            // .AddTransient<KeyActionDockItem>()
            .AddSingleton<IRepository<DockItemData>>(s
                => new FileRepository<DockItemData>(s.GetRequiredService<ILogger>()) { FilePath = "data/dockItems.dat" }
            )
            .AddSingleton<IRepository<IconData>>(s
                => new FileRepository<IconData>(s.GetRequiredService<ILogger>()) { FilePath = "data/icons.dat" }
            )
            .AddSingleton<DockItemCoreExecutor>()
            .AddSingleton<IDockItemService, DockItemService>();
        return collection;
    }
}