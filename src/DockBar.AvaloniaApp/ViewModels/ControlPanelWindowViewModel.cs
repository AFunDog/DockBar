using System.Collections.Generic;
using System.Linq;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Zeng.CoreLibrary.Toolkit.Avalonia.Structs;
using Zeng.CoreLibrary.Toolkit.Contacts;
using Zeng.CoreLibrary.Toolkit.Services.Navigate;
using DockBar.AvaloniaApp.Contacts;
using DockBar.AvaloniaApp.Views;
using DockBar.Core.Contacts;
using DockBar.Core.Helpers;
using DockBar.Core.Structs;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace DockBar.AvaloniaApp.ViewModels;

internal sealed partial class ControlPanelWindowViewModel : ViewModelBase
{
    public ILogger Logger { get; }
    public IAppSettingWrapper AppSettingWrapper { get; }
    public INavigateService NavigateService { get; }

    public IEnumerable<MenuItemData> MenuItems { get; } = [new("主页", '\uE80F', "/"), new("设置", '\uE713', "/Settings")];

    [ObservableProperty]
    public partial object? Content { get; set; }

    [ObservableProperty]
    public partial MenuItemData? SelectedMenuItem { get; set; }

    partial void OnSelectedMenuItemChanged(MenuItemData? value)
    {
        Logger.Information("SelectedMenuItem changed to {Value}", value?.Title);
    }

    public ControlPanelWindowViewModel()
        : this(Log.Logger, IAppSettingWrapper.Empty, INavigateService.Empty) { }

    public ControlPanelWindowViewModel(ILogger logger, IAppSettingWrapper appSettingWrapper, INavigateService navigateService)
    {
        Logger = logger;
        AppSettingWrapper = appSettingWrapper;
        NavigateService = navigateService
            .RegisterViewRoute("/", Program.ServiceProvider.GetRequiredService<MainView>)
            .RegisterViewRoute("/Settings", Program.ServiceProvider.GetRequiredService<SettingView>);
        NavigateService.OnNavigated += (s, e) =>
        {
            using var _ = LogHelper.Trace();
            Content = e;
            //SelectedMenuItem = MenuItems.FirstOrDefault(x => x.Tag is string route && route == s.CurrentRoute);
            Logger.Information("Navigated to {Route} {View}", s.CurrentRoute, e);
        };
        //NavigateService.Navigate("/");
        NavigateService.ForceRefresh();
    }

    [RelayCommand]
    private void Navigate(MenuItemData data)
    {
        if (data.Tag is string route)
        {
            NavigateService.Navigate(route);
        }
    }
}
