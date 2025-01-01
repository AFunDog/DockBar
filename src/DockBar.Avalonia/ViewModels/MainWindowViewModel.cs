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
using DockBar.Shared.Helpers;
using DockBar.SystemMonitor;
using Serilog;

namespace DockBar.Avalonia.ViewModels;

internal sealed partial class MainWindowViewModel : ViewModelBase
{
    public IDockItemService? DockItemService { get; }
    public ILogger? Logger { get; }
    public AppSetting GlobalSetting { get; } = new();

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

    /// <summary>
    /// 是否选中了 DockItem
    /// </summary>
    public bool IsSelectedDockItem => SelectedDockItem is not null;

    /// <summary>
    /// 是否处于移动模式
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DockPanelWidth))]
    public partial bool IsMoveMode { get; set; } = false;

    /// <summary>
    /// 是否处于文件拖入模式
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DockPanelWidth))]
    public partial bool IsDragMode { get; set; } = false;

    /// <summary>
    /// 是否鼠标进入
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DockPanelWidth))]
    public partial bool IsMouseEntered { get; set; } = false;

    /// <summary>
    /// 是否处于显示右键菜单
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DockPanelWidth))]
    public partial bool IsContextMenuShow { get; set; } = false;

    /// <summary>
    /// 是否处于热键按下
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DockPanelWidth))]
    public partial bool IsHotKeyPressed { get; set; } = false;

    public double DockPanelWidth =>
        (IsMouseEntered || IsContextMenuShow || IsDragMode || IsMoveMode || IsHotKeyPressed) && GlobalSetting is not null
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
        AppSetting globalSetting,
        PerformanceMonitor performanceMonitor
    )
    {
        Logger = logger;
        DockItemService = dockItemService;
        GlobalSetting = globalSetting;
        PerformanceMonitor = performanceMonitor;

        InitDockItemService(DockItemService);

        GlobalSetting.PropertyChanged += (s, e) => NotifyPanelSize();
    }

    private void InitDockItemService(IDockItemService dockItemService)
    {
        void OnDockItemChanged(IDockItemService service, DockItemChangedEventArgs e)
        {
            switch (e.ChangeType)
            {
                case DockItemChangeType.Add:
                    switch (e.DockItem)
                    {
                        case WrappedDockItem { DockItem: not null } wrappedDockItem:
                            DockItems.Insert(wrappedDockItem.Index, wrappedDockItem);
                            for (int i = 0; i < DockItems.Count; i++)
                            {
                                if (DockItems[i] is WrappedDockItem w)
                                {
                                    w.Index = i;
                                }
                            }
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
            service.SaveData(App.StorageFile);
        }
        void OnDockItemStarted(IDockItemService service, DockItemBase dockItem)
        {
            using var _ = LogHelper.Trace();
            if (dockItem is KeyActionDockItem { ActionKey: not null } keyActionDockItem)
            {
                if (KeyActionDockItems.KeyActions.TryGetValue(keyActionDockItem.ActionKey, out var action))
                {
                    try
                    {
                        action();
                    }
                    catch (System.Exception e)
                    {
                        Logger?.Error(e, "执行 KeyActionDockItem 的任务时发生错误");
                    }
                }
                else
                {
                    Logger?.Warning("KeyActionDockItem 的任务未找到");
                }
            }
        }

        dockItemService.LoadData(App.StorageFile);
        DockItems.Clear();

        foreach (
            var wrappedItem in dockItemService
                .DockItems.Where(item => item is WrappedDockItem)
                .Select(item => (WrappedDockItem)item)
                .OrderBy(item => item.Index)
        )
        {
            DockItems.Add(wrappedItem);
        }
        foreach (var dockItem in dockItemService.DockItems.Where(item => item is not WrappedDockItem))
        {
            DockItems.Add(dockItem);
        }
        dockItemService.DockItemChanged += OnDockItemChanged;
        dockItemService.DockItemStarted += OnDockItemStarted;
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
