using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DockBar.AvaloniaApp.Structs;
using DockBar.Core.Contacts;
using DockBar.Core.Structs;
using DockBar.DockItem.Contacts;
using DockBar.DockItem.Helpers;
using DockBar.DockItem.Items;
using DockBar.DockItem.Structs;
using ObservableCollections;
using Serilog;
using Zeng.CoreLibrary.Toolkit.Logging;

namespace DockBar.AvaloniaApp.ViewModels;

public class EditMetadataChangedEventArgs : EventArgs
{
    public required string? OldValue { get; set; }
    public required string? NewValue { get; set; }
}

public sealed partial class EditMetadata : ObservableObject
{
    public event EventHandler<EditMetadataChangedEventArgs>? KeyChanged;
    public event EventHandler<EditMetadataChangedEventArgs>? ValueChanged;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanKeyEdit), nameof(CanRemove))]
    public partial MetadataDescription? Description { get; set; }


    public bool CanKeyEdit => Description is null;

    public bool CanRemove => Description is null;

    [ObservableProperty]
    public partial string? Key { get; set; }


    partial void OnKeyChanged(string? oldValue, string? newValue) => KeyChanged?.Invoke(
        this,
        new EditMetadataChangedEventArgs() { OldValue = oldValue, NewValue = newValue }
    );

    [ObservableProperty]
    public partial string? Value { get; set; }

    partial void OnValueChanged(string? oldValue, string? newValue) => ValueChanged?.Invoke(
        this,
        new EditMetadataChangedEventArgs() { OldValue = oldValue, NewValue = newValue }
    );
}

internal sealed partial class EditDockItemWindowViewModel : ViewModelBase
{
    private IServiceProvider ServiceProvider { get; }
    private ILogger Logger { get; }
    internal IDockItemService DockItemService { get; }
    private IRepository<DockItemData> DockItemDataRepo { get; }

    // internal IDockItemFactory DockItemFactory { get; }

    // internal IStorageProvider? StorageProvider { get; set; }


    // public event Action? Confirmed;

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
    public partial DockItemData? CurrentDockItem { get; set; }

    partial void OnCurrentDockItemChanged(DockItemData? oldValue, DockItemData? newValue)
    {
        if (oldValue is not null)
            oldValue.Metadata.MetadataChanged -= OnMetadataChanged;
        if (newValue is not null)
        {
            newValue.Metadata.MetadataChanged += OnMetadataChanged;
            Dispatcher.UIThread.Post(() => { RefreshMetadata(); });
        }
    }

    private ObservableDictionary<string, EditMetadata> MetadataTable { get; }
    public IEnumerable<EditMetadata> MetadataView { get; }

    [ObservableProperty]
    public partial bool UseGenerateName { get; set; }

    [ObservableProperty]
    public partial bool UseGenerateIcon { get; set; }

    [ObservableProperty]
    public partial byte[]? IconData { get; set; }

    private IReadOnlyList<(string Type, Func<DockItemData?> Builder)> DockItemTypeTable { get; }

    public IEnumerable<string> DockItemTypeStrings => DockItemTypeTable.Select(v => v.Type);

    public int SelectedDockItemTypeIndex
    {
        get => CurrentDockItem is null
            ? -1
            : DockItemTypeTable
                .Select((item, index) => (item, index))
                .FirstOrDefault(i => i.item.Type == CurrentDockItem.Type)
                .index;
        set => CurrentDockItem = value == -1 ? null : DockItemTypeTable[value].Builder();
    }

    public EditDockItemWindowViewModel() { }

    public EditDockItemWindowViewModel(
        IServiceProvider provider,
        ILogger logger,
        IDockItemService dockItemService,
        IAppSettingWrapper appSettingWrapper,
        IRepository<DockItemData> dockItemDataRepo)
    {
        ServiceProvider = provider;
        Logger = logger;
        DockItemService = dockItemService;
        AppSetting = appSettingWrapper.Data;
        DockItemDataRepo = dockItemDataRepo;


        MetadataTable = [];
        var view = MetadataTable.CreateView(x => x.Value);
        MetadataView = view.ToNotifyCollectionChanged();
        // view.AttachFilter(x => !(x.Value.value is null && x.Value.description is null));

        DockItemTypeTable =
        [
            ("Link",
                () => new DockItemData()
                {
                    Id = Ulid.NewUlid().ToGuid(),
                    IconId = Guid.Empty,
                    Index = Index,
                    Metadata = new() { ["LinkType"] = "Lnk", ["LinkPath"] = string.Empty },
                    Name = UseGenerateName ? $"链接项目{Index}" : "",
                    ParentPath = "/",
                    Type = "Link"
                })
            // (typeof(DockLinkItem), "链接项目", () => ServiceProvider.GetService<DockLinkItem>()),
            // (typeof(KeyActionDockItem), "自定义行为项目", () => ServiceProvider.GetService<KeyActionDockItem>()),
            // (typeof(DockItemFolder), "文件夹项目", () => ServiceProvider.GetService<DockItemFolder>())
        ];

        WeakReferenceMessenger.Default.Register<EditDockItemWindowViewModel, DragEventArgs, string>(
            this,
            "EditDockItemWindow.OnDrop",
            OnDrop
        );

        WeakReferenceMessenger.Default.Register<EditDockItemWindowViewModel, OpenEditDockItemWindowMessage, string>(
            this,
            "EditDockItemWindow.OpenWindow",
            (s, e) =>
            {
                IsAddMode = e.IsAddMode;
                CurrentDockItem = e.BaseDockItem;
                IconData = e.BaseIcon;

                // Dispatcher.UIThread.Post(() =>
                //     {
                //         MetadataTable.Clear();
                //         if (CurrentDockItem is not null)
                //         {
                //             foreach (var (k, v) in CurrentDockItem.MetadataKey)
                //             {
                //                 MetadataTable[k] = v;
                //             }
                //         }
                //     }
                // );
            }
        );
    }

    private void RefreshMetadata()
    {
        MetadataTable.Clear();
        if (CurrentDockItem is not null && DockItemService.GetValidMetadata(CurrentDockItem.Type) is { } descriptions)
        {
            var descTable = descriptions.ToDictionary(x => x.Key);
            foreach (var (k, v) in CurrentDockItem.Metadata)
            {
                MetadataTable[k] = ToEditMetadata(k, v, descTable.GetValueOrDefault(k));
            }

            foreach (var (k, v) in descTable)
            {
                if (MetadataTable.ContainsKey(k) is false)
                {
                    MetadataTable[k] = ToEditMetadata(k, null, v);
                }
            }
        }
    }

    private void OnMetadataChanged(object? s, DockItemMetadataChangedEventArgs e)
    {
        if (s is not DockItemMetadata data)
            return;
        if (e.DockItemMetadataChangedType is DockItemMetadataChangedTypeEnum.Update)
        {
            // MetadataTable[e.Key] = MetadataTable.GetValueOrDefault(e.Key) is { } o
            //     ? o with { value = data[e.Key] }
            //     : (data[e.Key], null);
            if (MetadataTable.TryGetValue(e.Key, out var editMetadata))
            {
                editMetadata.Value = data[e.Key];
            }
            else
            {
                // Dispatcher.UIThread.InvokeAsync(async () =>
                // {
                //     
                // });
                MetadataTable[e.Key] = new EditMetadata() { Key = e.Key, Value = data[e.Key], Description = null };
            }
        }
        else if (e.DockItemMetadataChangedType is DockItemMetadataChangedTypeEnum.Delete)
        {
            // MetadataTable[e.Key] = MetadataTable.GetValueOrDefault(e.Key) is { } o
            //     ? o with { value = null }
            //     : (null, null);
            MetadataTable.Remove(e.Key);
        }
    }

    private EditMetadata ToEditMetadata(string key, string? value, MetadataDescription? description)
    {
        var editMetadata = new EditMetadata() { Key = key, Value = value, Description = description };
        editMetadata.KeyChanged += (s, e) =>
        {
            if (CurrentDockItem is null)
                return;
            var oldValue = e.OldValue is null ? null : CurrentDockItem.Metadata[e.OldValue];
            if (e.OldValue is not null)
                CurrentDockItem.Metadata.Remove(e.OldValue);
            if (e.NewValue is not null)
                CurrentDockItem.Metadata[e.NewValue] = oldValue ?? string.Empty;
        };
        editMetadata.ValueChanged += (s, e) =>
        {
            // MetadataTable[((EditMetadata)s!).Key] = e.NewValue;
            if (CurrentDockItem is null || s is not EditMetadata metadata)
                return;
            if (metadata.Key is not null)
                CurrentDockItem.Metadata[metadata.Key] = e.NewValue ?? string.Empty;
        };
        return editMetadata;
    }

    private void OnDrop(object recipient, DragEventArgs message)
    {
        var data = message.Data.GetFiles();

        if (data is not null && data.FirstOrDefault() is { } item)
        {
            CurrentDockItem = new DockItemData()
            {
                Id = Ulid.NewUlid().ToGuid(),
                Name = item.Name,
                IconId = Guid.Empty,
                Index = Index,
                Type = "Link",
                ParentPath = "/",
                Metadata = new DockItemMetadata()
                {
                    ["LinkPath"] = item.Path.LocalPath, ["LinkType"] = nameof(LinkType.Lnk)
                }
            };
            Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    IconData = await IconHelper.GetIconDataFromPath(item.Path.LocalPath, LinkType.Lnk);
                }
            );
        }
        // CurrentDockItem = new DockLinkItem { LinkPath = item.Path.LocalPath, LinkType = LinkType.File };
    }


    public bool CanConfirmDockItem() => CurrentDockItem is not null;

    [RelayCommand(CanExecute = nameof(CanConfirmDockItem))]
    private void ConfirmDockItem()
    {
        if (CurrentDockItem is null)
            return;

        if (IsAddMode)
        {
            // DockItemDataRepo.Insert(new DockItemData()
            // {
            //     Id = Guid.NewGuid(),
            //     Name = CurrentDockItem.ShowName ?? string.Empty,
            //     Type = 
            //     Index = Index,
            //     UseGeneratedIcon = CurrentDockItem.UseGeneratedIcon,
            //     UseGeneratedShowName = CurrentDockItem.UseGeneratedShowName,
            //     ShowName = CurrentDockItem.ShowName,
            //     IconData = CurrentDockItem.IconData
            // });
            // DockItemService.RegisterDockItem(CurrentDockItem);
            // DockItemService.Root.Insert(Index, CurrentDockItem.Key);
        }

        // 如果是修改模式，实际上什么都不用做，因为 CurrentDockItem 已经是引用类型
        // Confirmed?.Invoke();

        WeakReferenceMessenger.Default.Send(
            new EditDockItemWindowReply(CurrentDockItem, IconData),
            "EditDockItemWindow.Confirm"
        );
    }

    // public bool CanOpenIconFile() => !CurrentDockItem?.UseGeneratedIcon ?? false;

    [RelayCommand]
    private async Task OpenIconFileAsync()
    {
        if (CurrentDockItem is null)
            return;

        try
        {
            var files = await WeakReferenceMessenger.Default.Send(
                new OpenFilePickerMessage() { Options = new() { Title = "选择图标文件", AllowMultiple = false } },
                "EditDockItemWindow.OpenFilePicker"
            );

            // var files = await StorageProvider.OpenFilePickerAsync(new() { Title = "选择图标文件", AllowMultiple = false });

            var iconFile = files.FirstOrDefault();
            if (iconFile is null)
                return;

            await using var stream = await iconFile.OpenReadAsync();
            if (stream.Length >= 1024 * 1024 * 32)
            {
                Logger.Trace().Warning("选择图标文件过大 不使用");
                return;
            }

            using var ms = stream.ToMemoryStream();
            IconData = ms.GetBuffer();
        }
        catch (Exception e)
        {
            Logger.Trace().Error(e, "");
        }
    }

    [RelayCommand]
    public void AddMetadata()
    {
        if (CurrentDockItem is null)
            return;
        int index = 0;
        while (CurrentDockItem.Metadata.ContainsKey($"Metadata{index}"))
        {
            index++;
        }

        CurrentDockItem.Metadata.Add($"Metadata{index}", "");
    }

    [RelayCommand]
    public void RemoveMetadata(string key)
    {
        CurrentDockItem?.Metadata.Remove(key);
    }

    //protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    //{
    //    base.OnPropertyChanged(e);
    //    //if (e.PropertyName == nameof(CurrentDockItem)) { }
    //}
}