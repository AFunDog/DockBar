using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DockBar.Core.Internals;
using MessagePack;
using MessagePack.Formatters;

namespace DockBar.Core.DockItems;

[MessagePackObject(AllowPrivate = true)]
public partial class WrappedDockItem : DockItemBase
{
    [IgnoreMember]
    internal override DockItemService? OwnerService
    {
        get => base.OwnerService;
        set
        {
            base.OwnerService = value;
            if (DockItem is not null)
                DockItem.OwnerService = value;
        }
    }

    [ObservableProperty]
    [Key(3)]
    public partial DockItemBase? DockItem { get; set; }

    [ObservableProperty]
    [Key(4)]
    public partial int Index { get; set; }

    partial void OnDockItemChanged(DockItemBase? oldValue, DockItemBase? newValue)
    {
        if (oldValue is not null)
            oldValue.PropertyChanged -= DockItem_PropertyChanged;
        if (newValue is not null)
            newValue.PropertyChanged += DockItem_PropertyChanged;
        ShowName = DockItem?.ShowName;
        IconData = DockItem?.IconData;
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        // 同步 DockItem 的属性
        switch (e.PropertyName)
        {
            case nameof(ShowName):
                if (DockItem is not null)
                    DockItem.ShowName = ShowName;
                break;
            case nameof(IconData):
                if (DockItem is not null)
                    DockItem.IconData = IconData;
                break;
            default:
                break;
        }
    }

    private void DockItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // 同步 DockItem 的属性
        switch (e.PropertyName)
        {
            case nameof(ShowName):
                ShowName = DockItem?.ShowName;
                break;
            case nameof(IconData):
                IconData = DockItem?.IconData;
                break;
            default:
                OnPropertyChanged(e.PropertyName);
                break;
        }
    }

    protected override void StartCore() => DockItem?.Start();
}
