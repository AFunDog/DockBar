using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DockBar.AvaloniaApp.Structs;
using DockBar.AvaloniaApp.Views;
using DockBar.AvaloniaApp.Windows;
using DockBar.DockItem.Contacts;
using DockBar.DockItem.Structs;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace DockBar.AvaloniaApp.ViewModels;

internal sealed partial class MenuWindowViewModel : ViewModelBase
{
    private IServiceProvider ServiceProvider { get; }
    private ILogger Logger { get; }
    private IDockItemService DockItemService { get; }
    private IRepository<IconData> IconDataRepo { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddDockItemCommand))]
    public partial bool CanAddItem { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditMenuItemCommand), nameof(RemoveDockItemCommand))]
    public partial DockItemData? SelectedDockItem { get; set; }

    [ObservableProperty]
    public partial int SelectedIndex { get; set; }

    public MenuWindowViewModel() { }

    public MenuWindowViewModel(
        IServiceProvider provider,
        ILogger logger,
        IDockItemService dockItemService,
        IRepository<IconData> iconDataRepo)
    {
        ServiceProvider = provider;
        Logger = logger.ForContext<MenuWindowViewModel>();
        DockItemService = dockItemService;
        IconDataRepo = iconDataRepo;

        WeakReferenceMessenger.Default.Register<MenuWindowViewModel, OpenMenuWindowMessage, string>(
            this,
            "MenuWindow.OpenMenu",
            (s, e) =>
            {
                SelectedDockItem = e.SelectedDockItem;
                SelectedIndex = e.SelectedIndex;
                CanAddItem = e.CanAddItem;
            }
        );
    }

    private void CloseMenu()
    {
        WeakReferenceMessenger.Default.Send(EventArgs.Empty, "MenuWindow.CloseMenu");
    }

    private bool CanAddDockItem() => CanAddItem;

    [RelayCommand(CanExecute = nameof(CanAddDockItem))]
    private async Task AddDockItem()
    {
        async Task OpenEditDockItemWindow()
        {
            try
            {
                var reply = await WeakReferenceMessenger.Default.Send(
                    new OpenEditDockItemWindowMessage() { IsAddMode = true, BaseDockItem = null, BaseIcon = null },
                    "WindowManager.OpenEditDockItemWindow"
                );
                if (reply is null || reply.DockItemData is null)
                    return;
                await DockItemService.AddOrUpdateDockItem(reply.DockItemData, reply.IconData);
                // var addDockItemWindow = ServiceProvider.GetRequiredKeyedService<Window>(nameof(EditDockItemWindow));
                // addDockItemWindow.ViewModel.IsAddMode = true;
                // addDockItemWindow.ViewModel.Index = SelectedIndex;
                // addDockItemWindow.Show(App.Instance.MainWindow);
                // addDockItemWindow.Closing += (s, e) =>
                // {
                //     MainWindow.ViewModel.HasOwnedWindow = false;
                //     MainWindow.ViewModel.SelectedDockItem = null;
                // };
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "打开 EditDockItemWindow 异常");
            }
        }

        await OpenEditDockItemWindow();
        CloseMenu();
    }

    public bool CanEditDockItem() => SelectedDockItem is not null;

    [RelayCommand(CanExecute = nameof(CanEditDockItem))]
    private async Task EditMenuItem()
    {
        async Task OpenEditDockItemWindow()
        {
            // if (GetSelectedDockItem(this) is null)
            //     return;
            try
            {
                var reply = await WeakReferenceMessenger.Default.Send(
                    new OpenEditDockItemWindowMessage()
                    {
                        IsAddMode = false,
                        BaseDockItem = SelectedDockItem,
                        BaseIcon = SelectedDockItem is null
                            ? []
                            : (await IconDataRepo.Select(SelectedDockItem.IconId))?.Data ?? []
                    },
                    "WindowManager.OpenEditDockItemWindow"
                );
                if (reply is null || reply.DockItemData is null)
                    return;
                await DockItemService.AddOrUpdateDockItem(reply.DockItemData, reply.IconData);
                // var editDockItemWindow = ServiceProvider.GetRequiredKeyedService<Window>(nameof(EditDockItemWindow));
                // editDockItemWindow.ViewModel.IsAddMode = false;
                // editDockItemWindow.ViewModel.Index = SelectedIndex;
                // editDockItemWindow.ViewModel.CurrentDockItem = SelectedDockItem;
                // MainWindow.ViewModel.HasOwnedWindow = true;
                // editDockItemWindow.Show(App.Instance.MainWindow);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "打开 EditDockItemWindow 异常");
            }

            // MainWindow.ViewModel.HasOwnedWindow = false;
        }

        await OpenEditDockItemWindow();
        CloseMenu();
    }

    public bool CanRemoveDockItem() => SelectedDockItem is not null;

    [RelayCommand(CanExecute = nameof(CanRemoveDockItem))]
    private void RemoveDockItem()
    {
        if (SelectedDockItem is not { } item)
            return;
        DockItemService.RemoveDockItem(item.Id);
        CloseMenu();
    }

    public bool CanMoveToFolder() => SelectedDockItem is not null;

    // [RelayCommand(CanExecute = nameof(CanMoveToFolder))]
    // private void MoveToFolder()
    // {
    //     if (SelectedDockItem is null) return;
    //     
    // }

    [RelayCommand]
    private void Exit()
    {
        if (App.Instance.ApplicationLifetime is IControlledApplicationLifetime lifetime)
            lifetime.Shutdown();

        CloseMenu();
    }

    [RelayCommand]
    private void OpenSettingWindow()
    {
        try
        {
            // var settingWindow = Program.ServiceProvider.GetRequiredService<ControlPanelWindow>();
            // settingWindow.Show(Program.ServiceProvider.GetRequiredKeyedService<Window>(nameof(MainWindow)));
            // settingWindow.Closed += (s, e) => { App.Instance.MainWindow. };
            WeakReferenceMessenger.Default.Send("/", "WindowManager.OpenControlPanelWindow");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "打开设置窗口失败");
        }

        CloseMenu();
    }
}