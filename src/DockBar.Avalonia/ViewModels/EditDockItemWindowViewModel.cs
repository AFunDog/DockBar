using System;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DockBar.Avalonia.Extensions;
using DockBar.Avalonia.Structs;
using DockBar.Core;
using DockBar.Core.DockItems;

namespace DockBar.Avalonia.ViewModels;

internal sealed partial class EditDockItemWindowViewModel : ViewModelBase
{
    internal IDockItemService? DockItemService { get; }

    public event Action? Confirmed;

    [ObservableProperty]
    public partial AppSetting LocalSetting { get; set; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WindowTitle), nameof(ConfirmButtonText))]
    public partial bool IsAddMode { get; set; }

    public string WindowTitle => IsAddMode ? "添加停靠项目" : "编辑停靠项目";
    public string ConfirmButtonText => IsAddMode ? "确认添加" : "确认修改";

    [ObservableProperty]
    public partial int Index { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConfirmDockItemCommand))]
    public partial DockItemBase CurrentDockItem { get; set; } = new DockLinkItem();

    // 设计时使用
    public EditDockItemWindowViewModel() { }

    public EditDockItemWindowViewModel(IDockItemService dockItemService, AppSetting globalSetting)
    {
        DockItemService = dockItemService;
        LocalSetting = globalSetting;
    }

    public bool CanConfirmDockItem => CurrentDockItem is not null;

    [RelayCommand(CanExecute = nameof(CanConfirmDockItem))]
    private void ConfirmDockItem()
    {
        if (CurrentDockItem is null)
            return;

        if (IsAddMode)
        {
            DockItemService?.RegisterDockItem(Index, CurrentDockItem);
        }
        // 如果是修改模式，实际上什么都不用做，因为 CurrentDockItem 已经是引用类型
        Confirmed?.Invoke();
    }
}
