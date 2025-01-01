using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;
using DockBar.Avalonia.Controls;
using DockBar.Avalonia.Structs;
using DockBar.Avalonia.ViewModels;
using DockBar.Core.DockItems;
using DockBar.Shared.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Vanara.PInvoke;

namespace DockBar.Avalonia.Views;

public partial class MainWindow : Window
{
    internal MainWindowViewModel? ViewModel => DataContext as MainWindowViewModel;

    public MainWindow()
    {
        InitializeComponent();
        DockItemList.AddHandler(DragDrop.DropEvent, OnDrop);
        DockItemList.AddHandler(DragDrop.DragEnterEvent, OnDragEnter);
        DockItemList.AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);

        Win32Properties.AddWndProcHookCallback(this, WndProcHook);
        RegisterHotKey();
        RegisterKeyAction();
    }

    // 通过注册按键来实现全局按键响应
    // 响应打开面板或关闭面板

    private void RegisterHotKey()
    {
        using var _ = LogHelper.Trace();
        if (this.TryGetPlatformHandle() is IPlatformHandle platformHandle)
        {
            var hWnd = platformHandle.Handle;
            var res = User32.RegisterHotKey(hWnd, this.GetHashCode(), User32.HotKeyModifiers.MOD_ALT, (int)User32.VK.VK_SPACE);
            if (res)
            {
                App.Instance.Logger.Information("注册全局热键成功");
                return;
            }
        }
        App.Instance.Logger.Error("注册全局热键失败");
    }

    private void UnregisterHotKey()
    {
        var hWnd = this.TryGetPlatformHandle()!.Handle;
        User32.UnregisterHotKey(hWnd, this.GetHashCode());
    }

    private void RegisterKeyAction()
    {
        KeyActionDockItems.KeyActions["Setting"] = OpenSettingWindow;
    }

    private nint WndProcHook(nint hWnd, uint msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg == (uint)User32.WindowMessage.WM_HOTKEY && wParam == this.GetHashCode())
        {
            OnHotKeyPressed();
            handled = true;
        }
        return nint.Zero;
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (ViewModel is null)
            return;
        if (ViewModel.GlobalSetting is not null)
        {
            ViewModel.GlobalSetting.LoadSetting(App.SettingFile);
            ViewModel.GlobalSetting.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(AppSetting.AutoPositionBottom))
                {
                    TryMoveWindowToCenter();
                }
            };
            //ViewModel.DockItemService?.RegisterDockItem(new WrappedDockItem() { DockItem = new SettingDockItem(), Index = 0 });
        }
    }

    private void OnHotKeyPressed()
    {
        if (this.IsActive)
        {
            if (ViewModel is null)
                return;
            ViewModel.IsHotKeyPressed = !ViewModel.IsHotKeyPressed;
        }
        else
        {
            if (ViewModel is not null)
                ViewModel.IsHotKeyPressed = true;
            this.Activate();
        }
    }

    private void OnDragLeave(object? sender, DragEventArgs e)
    {
        if (ViewModel is not null)
            ViewModel.IsDragMode = false;
    }

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        if (ViewModel is not null)
            ViewModel.IsDragMode = true;
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        using var _ = LogHelper.Trace();
        if (ViewModel is null)
            return;

        ViewModel.SelectedIndex = PosXToIndex(e.GetPosition(DockItemList).X);
        foreach (var data in e.Data.GetFiles() ?? [])
        {
            ViewModel.InsertDockLinkItem(ViewModel.SelectedIndex, data.Path.LocalPath);
        }

        ViewModel.IsDragMode = false;
        ViewModel.Logger?.Debug("OnDrop {Index}", ViewModel.SelectedIndex);
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        UnregisterHotKey();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (ViewModel is null)
            return;
        using var _ = LogHelper.Trace();
        if (ViewModel.IsMoveMode)
        {
            if (e.Pointer.Type is PointerType.Mouse && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                BeginMoveDrag(e);
                e.Handled = true;
            }
        }

        ViewModel.SelectedDockItem = null;
        //ViewModel.IsDockItemListPointerPressed = false;
        double x = e.GetPosition(DockItemList).X;
        ViewModel.SelectedIndex = PosXToIndex(x);
        ViewModel.Logger?.Debug("窗体被点击 点击X:{X} 选中索引:{Index}", x, ViewModel.SelectedIndex);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (ViewModel is null)
            return;

        if (ViewModel.IsMoveMode)
        {
            ViewModel.IsMoveMode = false;
            Cursor = Cursor.Default;
            e.Handled = true;
        }
    }

    private void CloseMenuItem_Clicked(object? sender, RoutedEventArgs e)
    {
        Close();
        e.Handled = true;
    }

    private void SettingMenuItem_Clicked(object? sender, RoutedEventArgs e)
    {
        OpenSettingWindow();
        e.Handled = true;
    }

    private void OpenSettingWindow()
    {
        var settingWindow = App.Instance.ServiceProvider.GetRequiredService<SettingWindow>();
        _ = settingWindow.ShowDialog(this);
    }

    private void MoveMenuItem_Clicked(object? sender, RoutedEventArgs e)
    {
        Cursor = new Cursor(StandardCursorType.SizeAll);
        if (ViewModel is not null)
            ViewModel.IsMoveMode = true;
    }

    private void DockItem_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not DockItemControl source)
            return;
        if (ViewModel is null)
            return;
        using var _ = LogHelper.Trace();
        ViewModel.SelectedDockItem = source.DockItem;
        ViewModel.SelectedIndex = PosXToIndex(e.GetPosition(DockItemList).X);
        //ViewModel.IsDockItemListPointerPressed = true;
        ViewModel.Logger?.Debug("DockItem 被点击 {DockItem} {Index}", ViewModel.SelectedDockItem, ViewModel.SelectedIndex);
        e.Handled = true;
    }

    private void DeleteDockItemMenuItem_Clicked(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is null || ViewModel.SelectedDockItem is null)
            return;
        ViewModel.RemoveDockItem(ViewModel.SelectedDockItem.Key);
        e.Handled = true;
    }

    int PosXToIndex(double x)
    {
        if (ViewModel is null || ViewModel.GlobalSetting is null)
            return 0;

        var curRight = ViewModel.GlobalSetting.DockItemSize / 2 + ViewModel.DockItemListMargin.Left;
        for (int i = 0; i < ViewModel.DockItems.Count; i++)
        {
            if (x <= curRight)
                return i;
            curRight += ViewModel.GlobalSetting.DockItemSize + ViewModel.GlobalSetting.DockItemSpacing;
        }
        return ViewModel.DockItems.Count;
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        if (ViewModel is null || ViewModel.GlobalSetting is null)
            return;

        switch (ViewModel.GlobalSetting.DockPanelPosition)
        {
            case DockPanelPositionType.Left:
                TryMoveWindowToLeft();
                break;
            case DockPanelPositionType.Right:
                TryMoveWindowToRight();
                break;
            case DockPanelPositionType.Center:
                TryMoveWindowToCenter();
                break;
            default:
                break;
        }
    }

    private void TryMoveWindowToCenter()
    {
        if (ViewModel?.GlobalSetting?.IsAutoPosition ?? false)
        {
            if (Screens.ScreenFromWindow(this) is Screen screen)
            {
                // 自动靠下居中
                var realWidth = Width * screen.Scaling;
                var realHeight = Height * screen.Scaling;

                var x = screen.Bounds.X + (screen.Bounds.Width - realWidth) / 2;
                var y = screen.Bounds.Y + screen.Bounds.Height - ViewModel.GlobalSetting.AutoPositionBottom - realHeight;

                Position = new PixelPoint((int)x, (int)y);
            }
        }
    }

    private void TryMoveWindowToLeft()
    {
        if (ViewModel?.GlobalSetting?.IsAutoPosition ?? false)
        {
            if (Screens.ScreenFromWindow(this) is Screen screen)
            {
                var realWidth = Width * screen.Scaling;
                var realHeight = Height * screen.Scaling;

                var x = screen.Bounds.X;
                var y = screen.Bounds.Y + screen.Bounds.Height - ViewModel.GlobalSetting.AutoPositionBottom - realHeight;

                Position = new PixelPoint((int)x, (int)y);
            }
        }
    }

    private void TryMoveWindowToRight()
    {
        if (ViewModel?.GlobalSetting?.IsAutoPosition ?? false)
        {
            if (Screens.ScreenFromWindow(this) is Screen screen)
            {
                var realWidth = Width * screen.Scaling;
                var realHeight = Height * screen.Scaling;

                var x = screen.Bounds.X + screen.Bounds.Width - realWidth;
                var y = screen.Bounds.Y + screen.Bounds.Height - ViewModel.GlobalSetting.AutoPositionBottom - realHeight;

                Position = new PixelPoint((int)x, (int)y);
            }
        }
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        if (ViewModel is not null)
            ViewModel.IsMouseEntered = true;
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        if (ViewModel is not null)
            ViewModel.IsMouseEntered = false;
    }

    private void MainWindowContextMenuOpened(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is not null)
            ViewModel.IsContextMenuShow = true;
    }

    private void MainWindowContextMenuClosed(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is not null)
            ViewModel.IsContextMenuShow = false;
    }

    private void DockItemList_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        //if (ViewModel is not null)
        //{
        //    //ViewModel.IsDockItemListPointerPressed = true;
        //    ViewModel.SelectedDockItem = null;
        //}
        //e.Handled = true;
    }

    private async void AddDockItemMenuItem_Clicked(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is null)
            return;

        try
        {
            var addDockItemWindow = App.Instance.ServiceProvider.GetRequiredService<EditDockItemWindow>();
            addDockItemWindow.ViewModel.IsAddMode = true;
            addDockItemWindow.ViewModel.Index = ViewModel.SelectedIndex;
            await addDockItemWindow.ShowDialog(this);
            e.Handled = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    private void EditDockItemMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is null || ViewModel.SelectedDockItem is null)
            return;
        var editDockItemWindow = App.Instance.ServiceProvider.GetRequiredService<EditDockItemWindow>();
        editDockItemWindow.ViewModel.IsAddMode = false;
        editDockItemWindow.ViewModel.Index = ViewModel.SelectedIndex;
        editDockItemWindow.ViewModel.CurrentDockItem = ViewModel.SelectedDockItem;
        editDockItemWindow.ShowDialog(this);
        e.Handled = true;
    }

    private void AddSettingDockItemMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        ViewModel?.DockItemService?.RegisterDockItem(
            new WrappedDockItem() { DockItem = KeyActionDockItems.SettingDockItem, Index = ViewModel.SelectedIndex }
        );
    }
}
