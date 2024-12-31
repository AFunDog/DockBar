using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DockBar.Avalonia.Extensions;
using DockBar.Avalonia.Structs;
using DockBar.Core;
using DockBar.Core.DockItems;
using Dumpify;

namespace DockBar.Avalonia.ViewModels;

internal sealed partial class EditDockItemWindowViewModel : ViewModelBase
{
    internal IDockItemService? DockItemService { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowTitle), nameof(ConfirmButtonText))]
    public partial bool IsAddMode { get; set; }

    public string WindowTitle => IsAddMode ? "添加停靠项目" : "编辑停靠项目";
    public string ConfirmButtonText => IsAddMode ? "确认添加" : "确认修改";

    [ObservableProperty]
    public partial int Index { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DockIcon))]
    [NotifyCanExecuteChangedFor(nameof(AddDockItemCommand))]
    public partial WrappedDockItem? CurrentDockItem { get; set; }
    public IImage? DockIcon
    {
        get => CurrentDockItem?.IconDataStream.ToIImage();
    }

    //partial void OnCurrentDockItemChanged(WrappedDockItem? oldValue, WrappedDockItem? newValue)
    //{
    //    oldValue?.Dump("oldValue");
    //    newValue?.Dump("newValue");
    //}

    // 设计时使用
    public EditDockItemWindowViewModel() { }

    public EditDockItemWindowViewModel(IDockItemService dockItemService)
    {
        DockItemService = dockItemService;
    }

    public bool CanAddDockItem => CurrentDockItem is not null;

    [RelayCommand(CanExecute = nameof(CanAddDockItem))]
    private void AddDockItem()
    {
        if (CurrentDockItem is not null)
            DockItemService?.RegisterDockItem(CurrentDockItem);
    }
}
