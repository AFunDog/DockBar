using DockBar.Core.Internals;
using Microsoft.Extensions.DependencyInjection;

namespace DockBar.Core;

public static class DockItemServiceExtensions
{
    public static IServiceCollection UseDockItemService(this IServiceCollection collection)
    {
        collection.AddSingleton<IDockItemService, DockItemService>();
        return collection;
    }
}
