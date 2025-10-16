using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using DockBar.AvaloniaApp.Contacts;
using DockBar.AvaloniaApp.Structs;
using DockBar.Core.Contacts;
using DockBar.Core.Structs;
using DockBar.DockItem.Contacts;
using DockBar.DockItem.Items;
using DockBar.DockItem.Structs;
using ObservableCollections;
using Serilog;
using Zeng.CoreLibrary.Toolkit.Logging;

namespace DockBar.AvaloniaApp.ViewModels;

[Flags]
internal enum PanelState { Hide = 0 }

internal sealed record SelectMessage();

internal sealed partial class MainWindowViewModel : ViewModelBase
{
    private IServiceProvider ServiceProvider { get; }
    private ILogger Logger { get; }
    private IDockItemService DockItemService { get; }
    public AppSetting AppSetting { get; set; }
    public IGlobalHotKeyManager GlobalHotKeyManager { get; }
    private IRepository<DockItemData> DockItemDataRepo { get; }
    // private IRepository<DockItemPathData> DockItemPathDataRepo { get; }
    private IRepository<IconData> IconDataRepo { get; }
    public PerformanceMonitorViewModel PerformanceMonitorViewModel { get; set; }

    private ObservableDictionary<Guid, (DockItemData? itemData, IconData? iconData)> DockItemViewDataTable { get; }
    private ObservableList<(Guid id, bool changeState)> DockItemViewDataList { get; }

    // private ObservableList<(DockItemData? itemData, IconData? iconData)> DockItemViewDataList { get; }

    public IEnumerable<DockItemViewData> DockItemViewDataView { get; }

    [ObservableProperty]
    public partial int SelectedIndex { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSelectedDockItem))]
    public partial Guid SelectedDockItemId { get; set; } = Guid.Empty;

    //[ObservableProperty]
    //private bool _isDockItemListPointerPressed;

    /// <summary>
    /// 是否选中了 DockItem
    /// </summary>
    public bool IsSelectedDockItem => SelectedDockItemId != Guid.Empty;

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

    // /// <summary>
    // /// 是否处于显示右键菜单
    // /// </summary>
    // [ObservableProperty]
    // [NotifyPropertyChangedFor(nameof(IsPanelShow))]
    // public partial bool IsMenuShow { get; set; } = false;

    /// <summary>
    /// 是否处于热键按下
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPanelShow))]
    public partial bool IsHotKeyPressed { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPanelShow))]
    public partial bool HasOwnedWindow { get; set; } = false;

    // private ObservableHashSet<Window> OwnedWindowSet { get; set; } = []; 

    /// <summary>
    /// 面板是否显示
    /// </summary>
    public bool IsPanelShow
        => IsMouseEntered || IsDragMode || IsHotKeyPressed || HasOwnedWindow
    // || IsDockItemDragging
    ;

    /// <summary>
    /// 面板是否显示
    /// 延迟变化
    /// </summary>
    public bool IsPanelShowDelay => IsPanelShow;


    public MainWindowViewModel() { }

    public MainWindowViewModel(
        IServiceProvider serviceProvider,
        ILogger logger,
        IDockItemService dockItemService,
        IAppSettingWrapper appSettingWrapper,
        IGlobalHotKeyManager globalHotKeyManager,
        IRepository<DockItemData> dockItemDataRepo,
        // IRepository<DockItemPathData> dockItemPathDataRepo,
        IRepository<IconData> iconDataRepo,
        PerformanceMonitorViewModel performanceMonitorViewModel)
    {
        ServiceProvider = serviceProvider;
        Logger = logger;
        DockItemService = dockItemService;
        // 读取应用设置，在读取前数据就应该要被加载
        AppSetting = appSettingWrapper.Data;
        GlobalHotKeyManager = globalHotKeyManager;
        DockItemDataRepo = dockItemDataRepo;
        // DockItemPathDataRepo = dockItemPathDataRepo;
        IconDataRepo = iconDataRepo;
        PerformanceMonitorViewModel = performanceMonitorViewModel;

        // OwnedWindowSet.CollectionChanged += (in e) =>
        // {
        //     HasOwnedWindow = OwnedWindowSet.Count > 0;
        // };

        DockItemViewDataTable = new();
        DockItemViewDataList = new();
        var view = DockItemViewDataList.CreateView(x
            => DockItemViewDataTable.GetValueOrDefault(x.id) is { } ii
                ? ToDockItemViewData(ii.itemData!, ii.iconData)
                : null
        );
        DockItemViewDataTable.CollectionChanged += (in e) =>
        {
            var ordered = DockItemViewDataTable
                .OrderBy(x => x.Value.itemData?.Index == -1 ? int.MaxValue : x.Value.itemData?.Index)
                .ToArray();
            Dispatcher.UIThread.Post(() =>
                {
                    DockItemViewDataList.Clear();
                    foreach (var (id, (itemData, iconData)) in ordered)
                    {
                        DockItemViewDataList.Add((id, false));
                    }
                }
            );
        };

        view.AttachFilter(x => DockItemViewDataTable.ContainsKey(x.id));
        DockItemViewDataView = view.ToNotifyCollectionChanged();

        if (SynchronizationContext.Current is null)
        {
            SynchronizationContext.SetSynchronizationContext(
                new AvaloniaSynchronizationContext(Dispatcher.UIThread, DispatcherPriority.Default)
            );
        }

        Task.Run(async () =>
            {
                var dockItemViewDataList = (await DockItemDataRepo.SelectAll())
                    .Where(x => x.ParentPath == "/")
                    .Join((await IconDataRepo.SelectAll()), x => x.IconId, y => y.Id, (x, y) => (x, y))
                    .ToArray();
                foreach (var (itemData, iconData) in dockItemViewDataList)
                {
                    DockItemViewDataTable[itemData.Id] = (itemData, iconData);
                }
            }
        );

        DockItemDataRepo.ItemsChanged += (s, e) =>
        {
            if (e.Item.ParentPath != "/")
                return;
            switch (e.Type)
            {
                case RepositoryItemChangedTypeEnum.Insert:
                case RepositoryItemChangedTypeEnum.Update:
                {
                    // if (DockItemViewDataTable.TryGetValue(e.Item.Id, out var item))
                    // {
                    //     // if (item.iconData?.Id != e.Item.IconId)
                    //     // {
                    //     //     DockItemViewDataTable[e.Item.Id] = item with { itemData = e.Item,iconData = await IconDataRepo.Select(e.Item.IconId)};
                    //     // }
                    //     // else
                    //     // {
                    //     // }
                    //     Dispatcher.UIThread.InvokeAsync(async () =>
                    //         {
                    //             DockItemViewDataTable[e.Item.Id] = (itemData: e.Item,
                    //                 iconData: await IconDataRepo.Select(e.Item.IconId));
                    //         }
                    //     );
                    // }
                    // else
                    // {
                    //     DockItemViewDataTable[e.Item.Id] = new(e.Item, null);
                    // }
                    Dispatcher.UIThread.InvokeAsync(async () =>
                        {
                            DockItemViewDataTable[e.Item.Id] = (itemData: e.Item,
                                iconData: await IconDataRepo.Select(e.Item.IconId));
                        }
                    );

                    break;
                }
                case RepositoryItemChangedTypeEnum.Delete:
                {
                    DockItemViewDataTable.Remove(e.Item.Id);

                    break;
                }
            }
        };
        IconDataRepo.ItemsChanged += (s, e) =>
        {
            switch (e.Type)
            {
                case RepositoryItemChangedTypeEnum.Insert:
                case RepositoryItemChangedTypeEnum.Update:
                {
                    foreach (var (id, item) in DockItemViewDataTable)
                    {
                        if (item.itemData is null)
                            continue;
                        if (item.itemData.IconId == e.Item.Id)
                        {
                            DockItemViewDataTable[id] = item with { iconData = e.Item };
                        }
                    }

                    break;
                }
                case RepositoryItemChangedTypeEnum.Delete:
                {
                    foreach (var (id, item) in DockItemViewDataTable)
                    {
                        if (item.itemData is null)
                            continue;
                        if (item.itemData.IconId == e.Item.Id)
                        {
                            DockItemViewDataTable[id] = item with { iconData = null };
                        }
                    }

                    break;
                }
            }
        };

        #region Messager注册

        // 鼠标进入和退出消息
        WeakReferenceMessenger.Default.Register<MainWindowViewModel, ValueChangedMessage<bool>, string>(
            this,
            "MainWindow.IsMouseEntered",
            (s, e) => IsMouseEntered = e.Value
        );
        WeakReferenceMessenger.Default.Register<MainWindowViewModel, ValueChangedMessage<Guid>, string>(
            this,
            "MainWindow.SelectDockItem",
            (s, e) => SelectedDockItemId = e.Value
        );
        WeakReferenceMessenger.Default.Register<MainWindowViewModel, ValueChangedMessage<int>, string>(
            this,
            "MainWindow.SelectIndex",
            (s, e) => SelectedIndex = e.Value
        );
        WeakReferenceMessenger.Default.Register<MainWindowViewModel, ValueChangedMessage<bool>, string>(
            this,
            "MainWindow.IsDragMode",
            (s, e) => IsDragMode = e.Value
        );
        WeakReferenceMessenger.Default.Register<MainWindowViewModel, ValueChangedMessage<bool>, string>(
            this,
            "MainWindow.HasOwnedWindow",
            (s, e) => HasOwnedWindow = e.Value
        );
        WeakReferenceMessenger.Default.Register<MainWindowViewModel, AddDockItemMessage, string>(
            this,
            "AddDockItem",
            (s, e) => { DockItemService.AddDockItem(e.Name, e.IconData, e.ParentPath, e.Index, e.Type, e.Metadata); }
        );
        WeakReferenceMessenger.Default.Register<MainWindowViewModel, MoveDockItemMessage, string>(
            this,
            "MoveDockItem",
            (s, e) => { DockItemService.MoveDockItem(e.Id, "/", e.TargetIndex); }
        );

        WeakReferenceMessenger.Default.Register<MainWindowViewModel, OpenMenuWindowMessage, string>(
            this,
            "OpenMenuWindow",
            (s, e) =>
            {
                Dispatcher.UIThread.InvokeAsync(async Task () =>
                    {
                        try
                        {
                            // var menuWindow = ServiceProvider.GetRequiredKeyedService<Window>(nameof(MenuWindow));
                            // WeakReferenceMessenger.Default.Send(
                            //     e with
                            //     {
                            //         SelectedDockItem = await DockItemDataRepo.Select(SelectedDockItemId),
                            //         SelectedIndex = SelectedIndex
                            //     },
                            //     "MenuWindow.OpenMenu"
                            // );
                            Logger.Trace().Information("打开菜单 {Index}", SelectedIndex);
                            WeakReferenceMessenger.Default.Send(
                                e with
                                {
                                    SelectedDockItem = await DockItemDataRepo.Select(SelectedDockItemId),
                                    SelectedIndex = SelectedIndex,
                                },
                                "WindowManager.OpenMenuWindow"
                            );
                        }
                        catch (Exception exception)
                        {
                            Logger.Trace().Error(exception, "");
                        }
                        // menuWindow.SelectedDockItem = await DockItemDataRepo.Select(SelectedDockItemId);
                        // menuWindow.SelectedIndex = SelectedIndex;
                        // menuWindow.OpenMenu(e.X, e.Y);
                    }
                );
            }
        );

        #endregion

        // InitDockItemService(DockItemService);
    }


    private static DockItemViewData ToDockItemViewData(DockItemData itemData, IconData? iconData)
    {
        return new DockItemViewData() { Id = itemData.Id, ShowName = itemData.Name, IconData = iconData?.Data ?? [] };
    }

    // private int PosXToIndex(double x)
    // {
    //     var curRight = AppSetting.DockItemSize / 2;
    //     for (var i = 0; i < DockItemViewDataTable.Count; i++)
    //     {
    //         if (x <= curRight)
    //             return i;
    //         curRight += AppSetting.DockItemSize + AppSetting.DockItemSpacing;
    //     }
    //
    //     return DockItemViewDataTable.Count;
    // }

    /// <summary>
    /// 打开任务管理器
    /// </summary>
    [RelayCommand]
    private async Task OpenTaskmgr()
    {
        // 防止启动时间过长卡住UI
        await Task.Run(() =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo { FileName = "taskmgr", UseShellExecute = true });
                }
                catch (Exception e)
                {
                    Logger.Error(e, "打开 任务管理器 时发生错误");
                }
            }
        );
    }

    /// <summary>
    /// 执行 <paramref name="id"/> 对应的 <see cref="DockItemBase.Execute"/> 函数
    /// </summary>
    [RelayCommand]
    private void ExecuteDockItem(Guid id)
    {
        DockItemService.ExecuteDockItem(id);
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
                OnPropertyChanged(nameof(IsPanelShowDelay));
            // IsPanelShowDelay = IsPanelShow;
            else
                DelayTimer = DispatcherTimer.RunOnce(
                    () =>
                    {
                        // IsPanelShowDelay = IsPanelShow;
                        OnPropertyChanged(nameof(IsPanelShowDelay));
                        Logger.Verbose("IsPanelShowDelay 属性改变 {Property}", IsPanelShowDelay);
                    },
                    TimeSpan.FromSeconds(0.33)
                );

            Logger.Verbose(
                "IsPanelShow 属性改变 {Property} {Others}",
                IsPanelShow,
                new Dictionary<string, bool>
                {
                    [nameof(IsMouseEntered)] = IsMouseEntered,
                    [nameof(IsDragMode)] = IsDragMode,
                    [nameof(IsHotKeyPressed)] = IsHotKeyPressed,
                    [nameof(HasOwnedWindow)] = HasOwnedWindow
                    // [nameof(IsDockItemDraging)] = IsDockItemDraging
                }
            );
        }
    }
}