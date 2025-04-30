using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreLibrary.Toolkit.Contacts;
using DockBar.AvaloniaApp;
using DockBar.AvaloniaApp.Contacts;
using DockBar.AvaloniaApp.Structs;
using DockBar.AvaloniaApp.ViewModels;
using DockBar.DockItem;
using DockBar.DockItem.Structs;
using DockBar.Core.Helpers;
using DockBar.Core.Structs;
using DockBar.SystemMonitor;
using Serilog;

namespace DockBar.AvaloniaApp.ViewModels;

[Flags]
internal enum PanelState
{
    Hide = 0,
}

internal sealed partial class MainWindowViewModel : ViewModelBase
{
    public IDockItemService DockItemService { get; }
    public ILogger Logger { get; }
    public AppSetting GlobalSetting { get; set; }
    public IPerformanceMonitor PerformanceMonitor { get; set; }

    public IGlobalHotKeyManager  GlobalHotKeyManager { get; set; }
    
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
    //[ObservableProperty]
    //[NotifyPropertyChangedFor(nameof(IsPanelShow))]
    //public partial bool IsMoveMode { get; set; } = false;

    /// <summary>
    /// 是否处于文件拖入模式
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPanelShow))]
    public partial bool IsDragMode { get; set; } = false;

    /// <summary>
    /// 是否鼠标进入
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPanelShow))]
    public partial bool IsMouseEntered { get; set; } = false;

    /// <summary>
    /// 是否处于显示右键菜单
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPanelShow))]
    public partial bool IsMenuShow { get; set; } = false;

    /// <summary>
    /// 是否处于热键按下
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPanelShow))]
    public partial bool IsHotKeyPressed { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPanelShow))]
    public partial bool HasOwnedWindow { get; set; } = false;

    //[ObservableProperty]
    //[NotifyPropertyChangedFor(nameof(IsPanelShow))]
    //public partial bool IsMainWindowActivated { get; set; }
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPanelShow))]
    public partial bool IsDockItemDraging { get; set; } = false;

    /// <summary>
    /// 面板是否显示
    /// </summary>
    public bool IsPanelShow => IsMouseEntered || IsMenuShow || IsDragMode || IsHotKeyPressed || HasOwnedWindow || IsDockItemDraging;

    /// <summary>
    /// 面板是否显示
    /// 延迟变化
    /// </summary>
    public bool IsPanelShowDelay => IsPanelShow;

    public double ExtendDockPanelHeight => GlobalSetting.DockItemSize * 2;
    
    public MainWindowViewModel()
        : this(Log.Logger, IDockItemService.Empty, IDataProvider<AppSetting>.Empty, IPerformanceMonitor.Empty, IGlobalHotKeyManager.Empty)
    {
    }

    public MainWindowViewModel(
        ILogger logger,
        IDockItemService dockItemService,
        IDataProvider<AppSetting> appSettingProvider,
        IPerformanceMonitor performanceMonitor,
        IGlobalHotKeyManager globalHotKeyManager
    )
    {
        Logger = logger;
        DockItemService = dockItemService;

        // 读取应用设置，在读取前数据就应该要被加载
        GlobalSetting = appSettingProvider.Datas.FirstOrDefault() ?? new();
        PerformanceMonitor = performanceMonitor;

        GlobalHotKeyManager = globalHotKeyManager;

        InitDockItemService(DockItemService);

        // GlobalSetting.PropertyChanged += (s, e) =>
        // {
        //     //NotifyPanelSize();
        //     if (e.PropertyName == nameof(GlobalSetting.DockItemSize))
        //     {
        //         //OnPropertyChanged(nameof(DockPanelHeight));
        //         //OnPropertyChanged(nameof(ExtendDockPanelHeight));
        //     }
        // };
    }

    private void InitDockItemService(IDockItemService dockItemService)
    {
        void OnDockItemAdded(IDockItemService service, DockItemBase dockItem)
        {
            DockItems.Insert(dockItem.Index, dockItem);
            dockItemService.SaveData(App.StorageFile);
            //NotifyPanelSize();
        }

        void OnDockItemRemoved(IDockItemService service, DockItemBase dockItem)
        {
            DockItems.Remove(dockItem);
            dockItemService.SaveData(App.StorageFile);
            //NotifyPanelSize();
        }

        void OnDockItemMoved(IDockItemService service, (int oldIndex, int newIndex) args)
        {
            DockItems.Move(args.oldIndex, args.newIndex);
            // 暂时用这个来解决问题
            IsDockItemDraging = false;
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

    /// <summary>
    /// 打开任务管理器
    /// </summary>
    [RelayCommand]
    private void OpenTaskmgr()
    {
        try
        {
            Process.Start(new ProcessStartInfo() { FileName = "taskmgr", UseShellExecute = true });
        }
        catch (Exception e)
        {
            Logger.Error(e, "打开 任务管理器 时发生错误");
        }
    }

    /// <summary>
    /// 执行 <paramref name="dockItemKey"/> 对应的 <see cref="DockItemBase.Execute"/> 函数
    /// </summary>
    [RelayCommand]
    private void ExecuteDockItem(int dockItemKey)
    {
        DockItemService.ExecuteDockItem(dockItemKey);
    }

    public void InsertDockLinkItem(int index, string fullPath)
    {
        DockItemService.RegisterDockItem(index, new DockLinkItem { LinkPath = fullPath });

        //NotifyPanelSize();
    }

    public void RemoveDockItem(int key)
    {
        DockItemService.UnregisterDockItem(key);
        //NotifyPanelSize();
    }

    /// <summary>
    /// 用来延迟属性变化的 Timer
    /// </summary>
    private IDisposable? DelayTimer { get; set; }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        //Logger.Verbose("MainViewModel 属性改变 {Property}", e.PropertyName);

        // IsPanelShow 发生变化时，如果变为 false 则延迟发送变化信号
        if (e.PropertyName == nameof(IsPanelShow))
        {
            DelayTimer?.Dispose();
            if (IsPanelShow)
            {
                OnPropertyChanged(nameof(IsPanelShowDelay));
            }
            else
            {
                DelayTimer = DispatcherTimer.RunOnce(
                    () =>
                    {
                        OnPropertyChanged(nameof(IsPanelShowDelay));
                        Logger.Verbose("IsPanelShowDelay 属性改变 {Property}", IsPanelShowDelay);
                    },
                    TimeSpan.FromSeconds(0.33)
                );
                Logger.Verbose("IsPanelShow 属性改变 {Property}", IsPanelShow);
            }
        }
    }

    //private void NotifyPanelSize()
    //{
    //    OnPropertyChanged(nameof(DockItemListMargin));
    //    OnPropertyChanged(nameof(DockPanelHeight));
    //}
}