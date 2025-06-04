using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DockBar.SystemMonitor.Internals;
using Microsoft.Extensions.DependencyInjection;

namespace DockBar.SystemMonitor.Extensions;

public static class ServiceExtension
{
    public static IServiceCollection UseSystemMonitor(this IServiceCollection services)
    {
        services.AddTransient<IPerformanceMonitor, PerformanceMonitor>();
        return services;
    }
}