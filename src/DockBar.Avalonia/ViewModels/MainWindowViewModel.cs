using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DockBar.Avalonia.ViewDatas;
using DockBar.DockItem;
using MessagePack;
using Serilog;

namespace DockBar.Avalonia.ViewModels;

internal sealed partial class MainWindowViewModel : ViewModelBase
{
    const string StorageFile = ".dockItems";

    public IDockItemService DockItemService { get; }
    public ILogger Logger { get; }

    public ObservableCollection<DockItemData> DockItems { get; } = [];

    public GlobalViewModel Global { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSelectedDockItem))]
    private DockItemData? _selectedDockItem = null;

    public bool IsSelectedDockItem => SelectedDockItem != null;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDockItemPanelEnabled))]
    private bool _isMoveMode = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDockItemPanelEnabled))]
    private bool _isDragMode = false;

    public bool IsDockItemPanelEnabled => !IsMoveMode && !IsDragMode;

    public double DockPanelWidth =>
        IsDockItemPanelShow || IsContextMenuShow
            ? (Global.DockItemSize + Global.DockItemSpacing) * DockItems.Count
                + Global.DockItemExtendRate * Global.DockItemSize
                + Global.DockItemSpacing
            : Global.DockItemListMargin.Left + Global.DockItemListMargin.Right;

    public double DockPanelHeight => Global.DockItemSize * (1 + Global.DockItemExtendRate) + 8;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DockPanelWidth))]
    private bool _isDockItemPanelShow = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DockPanelWidth))]
    private bool _isContextMenuShow = false;

    public MainWindowViewModel(GlobalViewModel global, ILogger logger, IDockItemService dockItemService)
    {
        Global = global;
        Logger = logger;
        DockItemService = dockItemService;
        void AddHandler(object? s, DockItemChangedEventArgs e)
        {
            if (e.IsAdd)
            {
                DockItems.Add(new DockItemData(e.DockItem));
            }
        }

        DockItemService.DockItemChanged += AddHandler;

        DockItemService.ReadData(StorageFile);
        DockItemService.DockItemChanged -= AddHandler;
        DockItemService.DockItemChanged += (s, e) =>
        {
            if (e.IsAdd is false)
            {
                DockItems.Remove(DockItems.First(item => item.Key == e.DockItem.Key));
            }
        };
        Global.PropertyChanged += (s, e) => NotifyPanelSize();
    }

    public void SaveDockItemDatas()
    {
        DockItemService.SaveData(StorageFile);
        Logger.Debug($"保存数据到 {StorageFile}");
    }

    public void InsertDockLinkItem(int index, string fullPath)
    {
        Logger.Debug($"AddDockLinkItem {fullPath}");
        var key = Path.GetFileNameWithoutExtension(fullPath);
        DockItemService.AddDockLinkItem(key, fullPath);
        DockItems.Insert(index, new DockItemData(DockItemService.GetDockItem(key)!));
        NotifyPanelSize();
    }

    public void RemoveDockItem(string key)
    {
        DockItemService.RemoveDockItem(key);
        NotifyPanelSize();
    }

    private void NotifyPanelSize()
    {
        OnPropertyChanged(nameof(DockPanelWidth));
        OnPropertyChanged(nameof(DockPanelHeight));
    }
}
