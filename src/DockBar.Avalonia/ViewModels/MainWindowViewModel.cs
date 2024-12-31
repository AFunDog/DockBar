using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using DockBar.Avalonia.Structs;
using DockBar.Core;
using DockBar.Core.DockItems;
using DockBar.Core.Structs;
using DockBar.SystemMonitor;
using Serilog;

namespace DockBar.Avalonia.ViewModels;

internal sealed partial class MainWindowViewModel : ViewModelBase
{
    const string StorageFile = "dockItems.bin";

    public IDockItemService? DockItemService { get; set; }
    public ILogger? Logger { get; set; }
    public GlobalSetting GlobalSetting { get; set; } = new();

    [ObservableProperty]
    public partial PerformanceMonitor? PerformanceMonitor { get; set; }

    partial void OnPerformanceMonitorChanged(PerformanceMonitor? oldValue, PerformanceMonitor? newValue)
    {
        oldValue?.Dispose();
        newValue?.StartMonitor();
    }

    public ObservableCollection<DockItemBase> DockItems { get; } = [];

    [ObservableProperty]
    public partial int SelectedIndex { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSelectedDockItem))]
    public partial DockItemBase? SelectedDockItem { get; set; } = null;

    //[ObservableProperty]
    //private bool _isDockItemListPointerPressed;

    public bool IsSelectedDockItem => SelectedDockItem is not null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DockPanelWidth))]
    public partial bool IsMoveMode { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DockPanelWidth))]
    public partial bool IsDragMode { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DockPanelWidth))]
    public partial bool IsMouseEntered { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DockPanelWidth))]
    public partial bool IsContextMenuShow { get; set; } = false;

    public double DockPanelWidth =>
        (IsMouseEntered || IsContextMenuShow || IsDragMode || IsMoveMode) && GlobalSetting is not null
            ? (GlobalSetting.DockItemSize + GlobalSetting.DockItemSpacing) * DockItems.Count
                + GlobalSetting.DockItemExtendRate * GlobalSetting.DockItemSize
                + GlobalSetting.DockItemSpacing
                + 8
            : 0;

    //public double DockPanelWidth =>
    //    IsMoveMode || IsDragMode || IsMouseEntered || IsContextMenuShow && GlobalSetting is not null
    //        ? DockItemListPanelWidth + GlobalSetting.DockItemSpacing * 2
    //        : 0;

    public double DockPanelHeight =>
        GlobalSetting is not null
            ? GlobalSetting.DockItemSize * (1 + GlobalSetting.DockItemExtendRate)
                + 8
                + (GlobalSetting.DockItemIsShowName ? GlobalSetting.DockItemNameFontSize + 8 : 0)
            : 108;
    public Thickness DockItemListMargin => new(GlobalSetting?.DockItemSpacing ?? 0, 0, GlobalSetting?.DockItemSpacing ?? 0, 0);

    public MainWindowViewModel() { }

    public MainWindowViewModel(
        ILogger logger,
        IDockItemService dockItemService,
        GlobalSetting globalSetting,
        PerformanceMonitor performanceMonitor
    )
    {
        Logger = logger;
        DockItemService = dockItemService;
        GlobalSetting = globalSetting;
        PerformanceMonitor = performanceMonitor;

        void ChangeHandler(IDockItemService service, DockItemChangedEventArgs e)
        {
            switch (e.ChangeType)
            {
                case DockItemChangeType.Add:
                    switch (e.DockItem)
                    {
                        case WrappedDockItem { DockItem: not null } wrappedDockItem:
                            DockItems.Insert(wrappedDockItem.Index, wrappedDockItem);
                            break;
                        default:
                            DockItems.Add(e.DockItem);
                            break;
                    }
                    break;
                case DockItemChangeType.Remove:
                    DockItems.Remove(e.DockItem);
                    break;
                default:
                    break;
            }
            DockItemService.SaveData(StorageFile);
        }

        DockItemService.LoadData(StorageFile);
        foreach (var dockItem in DockItemService.DockItems)
        {
            DockItems.Add(dockItem);
        }
        DockItemService.DockItemChanged += ChangeHandler;
        GlobalSetting.PropertyChanged += (s, e) => NotifyPanelSize();
    }

    public void InsertDockLinkItem(int index, string fullPath)
    {
        DockItemService?.RegisterDockItem(
            new WrappedDockItem
            {
                DockItem = new DockLinkItem { LinkPath = fullPath },
                Index = index
            }
        );

        NotifyPanelSize();
    }

    public void RemoveDockItem(int key)
    {
        DockItemService?.UnregisterDockItem(key);
        NotifyPanelSize();
    }

    private void NotifyPanelSize()
    {
        OnPropertyChanged(nameof(DockPanelWidth));
        OnPropertyChanged(nameof(DockItemListMargin));
        //OnPropertyChanged(nameof(DockPanelHeight));
    }
}
