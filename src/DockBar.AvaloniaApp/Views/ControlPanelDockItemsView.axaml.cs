using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using DockBar.AvaloniaApp.Windows;
using DockBar.Core.Contacts;
using DockBar.Core.Structs;
using DockBar.DockItem.Contacts;
using DockBar.DockItem.Items;
using DockBar.DockItem.Structs;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace DockBar.AvaloniaApp.Views;

public partial class ControlPanelDockItemsView : UserControl
{
    public ILogger Logger { get; }
    public IDockItemService DockItemService { get; }

    public AppSetting AppSetting { get; }

    public ObservableCollection<DockItemData> DockItems { get; }

    public DockItemData? SelectedDockItem { get; set; }


    public ControlPanelDockItemsView() 
    {
        InitializeComponent();
    }

    public ControlPanelDockItemsView(
        ILogger logger,
        IDockItemService dockItemService,
        
        IAppSettingWrapper appSettingWrapper)
    {
        Logger = logger;
        DockItemService = dockItemService;
        AppSetting = appSettingWrapper.Data;
    
        // DockItems = new(DockItemService.RegisteredDockItems);
        // DockItemService.DockItemCreated += (s, e) => { DockItems.Add(e); };
        // DockItemService.DockItemRemoved += (s, e) => { DockItems.Remove(e); };
    
        DataContext = this;
    
        InitializeComponent();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (e.GetCurrentPoint(this).Properties.PointerUpdateKind is PointerUpdateKind.RightButtonReleased
            && e.Source is Visual source
            && source.GetVisualsAt(e.GetPosition(source)).Any(c => c == source || source.IsVisualAncestorOf(c)))
        {
            var point = e.GetPosition(this);
            var t = this.PointToScreen(point);
            OpenRightMenu(t.X, t.Y);
            e.Handled = true;
        }
    }


    private void OpenRightMenu(int x, int y)
    {
        try
        {
            // ViewModel.HasOwnedWindow = true;

            var menuWindow = Program.ServiceProvider.GetRequiredService<MenuWindow>();
            // menuWindow.PropertyChanged += OnMenuWindowPropertyChanged;
            // menuWindow.SelectedIndex = SelectedIndex;
            // menuWindow.SelectedDockItem = SelectedDockItem;
            menuWindow.OpenMenu(x, y);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "打开右键菜单失败");
        }
    }


    private void OnDockItemControlPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control { DataContext: DockItemData item })
            SelectedDockItem = item;
    }
}