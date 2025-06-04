using DockBar.AvaloniaApp.Views;
using Microsoft.Extensions.DependencyInjection;

namespace DockBar.AvaloniaApp.Extensions;

public static class ControlPanelViewExtensions
{
    public static IServiceCollection UseControlPanelViews(this IServiceCollection services) => services
        .AddTransient<ControlPanelMainView>()
        .AddTransient<ControlPanelDockItemsView>()
        .AddTransient<ControlPanelSettingView>()
        .AddTransient<ControlPanelAboutView>();
}