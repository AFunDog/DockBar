using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Zeng.CoreLibrary.Toolkit.Avalonia.Structs;
using Zeng.CoreLibrary.Toolkit.Services.Navigate;
using DockBar.AvaloniaApp.Views;
using DockBar.Core.Contacts;
using Lucide.Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Zeng.CoreLibrary.Toolkit.Logging;

namespace DockBar.AvaloniaApp.ViewModels;

internal sealed partial class ControlPanelWindowViewModel : ViewModelBase
{
    private ILogger Logger { get; }
    private IAppSettingWrapper AppSettingWrapper { get; }
    private INavigateService NavigateService { get; }

    public IEnumerable<MenuItemData> MenuItems { get; } =
    [
        new("主页", new LucideIcon() { Kind = LucideIconKind.House }, "/"),
        new("停靠项目", new LucideIcon() { Kind = LucideIconKind.LayoutGrid }, "/DockItems"),
        new("设置", new LucideIcon() { Kind = LucideIconKind.Settings }, "/Settings")
    ];

    [ObservableProperty]
    public partial object? Content { get; set; }

    [ObservableProperty]
    public partial MenuItemData? SelectedMenuItem { get; set; }

    partial void OnSelectedMenuItemChanged(MenuItemData? value)
    {
        Logger.Information("SelectedMenuItem changed to {Value}", value?.Title);
    }

    public ControlPanelWindowViewModel() { }

    public ControlPanelWindowViewModel(
        ILogger logger,
        IAppSettingWrapper appSettingWrapper,
        INavigateService navigateService)
    {
        Logger = logger;
        AppSettingWrapper = appSettingWrapper;
        NavigateService = navigateService
            .RegisterViewRoute("/", Program.ServiceProvider.GetRequiredService<ControlPanelMainView>)
            .RegisterViewRoute("/DockItems", Program.ServiceProvider.GetRequiredService<ControlPanelDockItemsView>)
            .RegisterViewRoute("/Settings", Program.ServiceProvider.GetRequiredService<ControlPanelSettingView>);
        NavigateService.OnNavigated += (s, e) =>
        {
            Content = e;
            //SelectedMenuItem = MenuItems.FirstOrDefault(x => x.Tag is string route && route == s.CurrentRoute);
            Logger.Trace().Information("Navigated to {Route} {View}", s.CurrentRoute, e);
        };
        //NavigateService.Navigate("/");
        // NavigateService.ForceRefresh();

        WeakReferenceMessenger.Default.Register<ControlPanelWindowViewModel, string, string>(
            this,
            "ControlPanelWindow.OpenWindow",
            (s, e) => { NavigateService.Navigate(e); }
        );
    }

    [RelayCommand]
    private void Navigate(MenuItemData data)
    {
        if (data.Tag is string route)
            NavigateService.Navigate(route);
    }
}