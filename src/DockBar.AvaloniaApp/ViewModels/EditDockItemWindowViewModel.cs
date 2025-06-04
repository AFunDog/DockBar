using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Zeng.CoreLibrary.Toolkit.Contacts;
using DockBar.AvaloniaApp.Extensions;
using DockBar.AvaloniaApp.Structs;
using DockBar.Core.Contacts;
using DockBar.Core.Structs;
using DockBar.DockItem;
using DockBar.DockItem.Items;
using Serilog;

namespace DockBar.AvaloniaApp.ViewModels;

internal sealed partial class EditDockItemWindowViewModel : ViewModelBase
{
    internal ILogger Logger { get; }
    internal IDockItemService DockItemService { get; }

    internal IDockItemFactory DockItemFactory { get; }

    internal IStorageProvider? StorageProvider { get; set; }


    public event Action? Confirmed;

    // public IDataProvider<AppSetting> LocalSettingProvider { get; }

    public AppSetting AppSetting { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowTitle), nameof(ConfirmButtonText))]
    public partial bool IsAddMode { get; set; }

    public string WindowTitle => IsAddMode ? "添加停靠项目" : "编辑停靠项目";
    public string ConfirmButtonText => IsAddMode ? "确认添加" : "确认修改";

    [ObservableProperty]
    public partial int Index { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDockItemCommand), nameof(OpenIconFileCommand))]
    [NotifyPropertyChangedFor(nameof(SelectedDockItemTypeIndex))]
    public partial DockItemBase? CurrentDockItem { get; set; }

    private IReadOnlyList<(Type ItemType, string Key, Func<DockItemBase> Builder)> DockItemTypeTable { get; }

    public IEnumerable<string> DockItemTypeStrings => DockItemTypeTable.Select(v => v.Key);

    public int SelectedDockItemTypeIndex
    {
        get => CurrentDockItem is null ? -1 :
            DockItemTypeTable.Index().FirstOrDefault(i => i.Item.ItemType == CurrentDockItem.GetType()) is { } item
            && item != default ? item.Index : -1;
        set => CurrentDockItem = value == -1 ? null : DockItemTypeTable[value].Builder();
    }

    public EditDockItemWindowViewModel() : this(
        Log.Logger,
        IDockItemService.Empty,
        IAppSettingWrapper.Empty,
        IDockItemFactory.Empty
    )
    {
    }

    public EditDockItemWindowViewModel(
        ILogger logger,
        IDockItemService dockItemService,
        IAppSettingWrapper appSettingWrapper,
        IDockItemFactory dockItemFactory)
    {
        Logger = logger;
        DockItemService = dockItemService;
        // LocalSettingProvider = globalSettingProvider;
        // AppSetting = appSettingProvider.Datas.First();
        AppSetting = appSettingWrapper.Data;
        DockItemFactory = dockItemFactory;

        DockItemTypeTable =
        [
            (typeof(DockLinkItem), "链接项目", () => DockItemFactory.Create<DockLinkItem>()),
            (typeof(KeyActionDockItem), "自定义行为项目", () => DockItemFactory.Create<KeyActionDockItem>()),
            (typeof(DockItemFolder), "文件夹项目", () => DockItemFactory.Create<DockItemFolder>())
        ];
    }

    public bool CanConfirmDockItem() => CurrentDockItem is not null;

    [RelayCommand(CanExecute = nameof(CanConfirmDockItem))]
    private void ConfirmDockItem()
    {
        if (CurrentDockItem is null)
            return;

        if (IsAddMode)
        {
            DockItemService.RegisterDockItem(CurrentDockItem);
            DockItemService.Root.Insert(Index, CurrentDockItem.Key);
        }

        // 如果是修改模式，实际上什么都不用做，因为 CurrentDockItem 已经是引用类型
        Confirmed?.Invoke();
    }

    // public bool CanOpenIconFile() => !CurrentDockItem?.UseGeneratedIcon ?? false;

    [RelayCommand]
    private async Task OpenIconFileAsync()
    {
        if (CurrentDockItem is null || StorageProvider is null)
            return;

        var files = await StorageProvider.OpenFilePickerAsync(new() { Title = "选择图标文件", AllowMultiple = false });

        var iconFile = files.FirstOrDefault();
        if (iconFile is null)
            return;

        await using var stream = await iconFile.OpenReadAsync();
        if (stream.Length >= 1024 * 1024 * 32)
        {
            Logger.Warning("选择图标文件过大 不使用");
            return;
        }

        using var ms = stream.ToMemoryStream();
        CurrentDockItem.IconData = ms.GetBuffer();
    }

    //protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    //{
    //    base.OnPropertyChanged(e);
    //    //if (e.PropertyName == nameof(CurrentDockItem)) { }
    //}
}