using DockBar.DockItem.Internals;
using Microsoft.Extensions.DependencyInjection;

namespace DockBar.DockItem.Extensions;

public static class ServiceExtension
{
    /// <summary>
    /// 添加单例 <see cref="IDockItemService"/> 和单例 <see cref="IDockItemFactory"/> 服务
    /// </summary>
    public static IServiceCollection UseDockItem(this IServiceCollection collection)
    {
        collection.AddSingleton<IDockItemService, DockItemService>().AddSingleton<IDockItemFactory, DockItemFactory>();
        return collection;
    }
}