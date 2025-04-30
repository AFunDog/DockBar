using DockBar.Core.Contacts;
using DockBar.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DockBar.Core.Extensions;

public static class ServiceExtension
{
    public static IServiceCollection UseAppSettingWrapper(this IServiceCollection serviceCollection)
        => serviceCollection.AddSingleton<IAppSettingWrapper,AppSettingWrapper>();
}