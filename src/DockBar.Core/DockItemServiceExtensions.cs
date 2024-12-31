using DockBar.Core.Internals;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace DockBar.Core;

public static class DockItemServiceExtensions
{
    public static IServiceCollection UseDockItemService(this IServiceCollection collection)
    {
        collection.AddSingleton<IDockItemService, DockItemService>();
        return collection;
    }

    //public static IServiceProvider GetServiceForDockItemService(this IServiceProvider provider)
    //{
    //    if (provider.GetService<ILogger>() is ILogger logger)
    //    {
    //        GlobalService.Logger = logger;
    //    }
    //    return provider;
    //}
}
