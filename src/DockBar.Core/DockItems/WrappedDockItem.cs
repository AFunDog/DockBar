using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DockBar.Core.Internals;
using MessagePack;
using MessagePack.Formatters;

namespace DockBar.Core.DockItems;

public partial class WrappedDockItem : DockItemBase
{
    [ObservableProperty]
    public partial DockItemBase? DockItem { get; set; }

    [ObservableProperty]
    public partial int Index { get; set; }
    public override MemoryStream? IconDataStream
    {
        get => DockItem?.IconDataStream;
    }

    partial void OnDockItemChanged(DockItemBase? oldValue, DockItemBase? newValue)
    {
        if (oldValue is not null)
            oldValue.PropertyChanged -= DockItem_PropertyChanged;
        if (newValue is not null)
        {
            if (string.IsNullOrEmpty(ShowName))
                ShowName = newValue.ShowName;
            newValue.PropertyChanged += DockItem_PropertyChanged;
        }
    }

    private void DockItem_PropertyChanging(object? sender, System.ComponentModel.PropertyChangingEventArgs e)
    {
        OnPropertyChanging(e.PropertyName);
    }

    private void DockItem_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ShowName):
                ShowName = DockItem?.ShowName;
                break;
            default:
                break;
        }
        OnPropertyChanged(e.PropertyName);
    }

    public override void Start() => DockItem?.Start();

    public override void Dispose()
    {
        base.Dispose();
        DockItem?.Dispose();
    }
}
