using System;
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
using DockBar.Shared.Helpers;
using DockBar.SystemMonitor;
using Serilog;

namespace DockBar.Avalonia.ViewModels;

internal sealed partial class MainWindowViewModel : ViewModelBase
{
    public IDockItemService? DockItemService { get; }
    public ILogger? Logger { get; }
    public AppSetting GlobalSetting { get; set; } = new();

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

    //public double DockBarBackgroundWidth => Math.Min(192, 192);

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
        void OnDockItemAdded(IDockItemService service, DockItemBase dockItem)
        {
            DockItems.Insert(dockItem.Index, dockItem);
            dockItemService.SaveData(App.StorageFile);
            NotifyPanelSize();
        }

        void OnDockItemRemoved(IDockItemService service, DockItemBase dockItem)
        {
            DockItems.Remove(dockItem);
            dockItemService.SaveData(App.StorageFile);
            NotifyPanelSize();
        }

        void OnDockItemMoved(IDockItemService service, (int oldIndex, int newIndex) args)
        {
            DockItems.Move(args.oldIndex, args.newIndex);
            dockItemService.SaveData(App.StorageFile);
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
                    catch (Exception e)
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
        foreach (var dockItem in dockItemService.DockItems)
        {
            DockItems.Add(dockItem);
        }
        //foreach (
        //    var wrappedItem in dockItemService
        //        .DockItems.Where(item => item is WrappedDockItem)
        //        .Select(item => (WrappedDockItem)item)
        //        .OrderBy(item => item.Index)
        //)
        //{
        //    DockItems.Add(wrappedItem);
        //}
        //foreach (var dockItem in dockItemService.DockItems.Where(item => item is not WrappedDockItem))
        //{
        //    DockItems.Add(dockItem);
        //}
        dockItemService.DockItemAdded += OnDockItemAdded;
        dockItemService.DockItemRemoved += OnDockItemRemoved;
        dockItemService.DockItemMoved += OnDockItemMoved;
        dockItemService.DockItemStarted += OnDockItemStarted;
    }

    public void InsertDockLinkItem(int index, string fullPath)
    {
        DockItemService?.RegisterDockItem(index, new DockLinkItem { LinkPath = fullPath });

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
        OnPropertyChanged(nameof(DockPanelHeight));
    }
}
