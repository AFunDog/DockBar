using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform;
using DockBar.Avalonia.Controls;
using DockBar.Avalonia.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Vanara.PInvoke;

namespace DockBar.Avalonia.Views;

public partial class MainWindow : Window
{
    internal MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext!;

    public MainWindow()
    {
        InitializeComponent();
        DockItemList.AddHandler(DragDrop.DropEvent, OnDrop);
        DockItemList.AddHandler(DragDrop.DragEnterEvent, OnDragEnter);
        DockItemList.AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
        this.Closing += (s, e) =>
        {
            UnregisterHotKey();
            ViewModel.SaveDockItemDatas();
            ViewModel.Global.SaveSettings();
        };

        Win32Properties.AddWndProcHookCallback(this, WndProcHook);
        RegisterHotKey();
    }

    // 通过注册按键来实现全局按键响应
    // 响应打开面板或关闭面板

    private void RegisterHotKey()
    {
        var hWnd = this.TryGetPlatformHandle()!.Handle;
        var res = User32.RegisterHotKey(hWnd, this.GetHashCode(), User32.HotKeyModifiers.MOD_WIN, (int)User32.VK.VK_OEM_3);
        if (!res)
        {
            Log.Error("RegisterHotKey failed");
        }
    }

    private void UnregisterHotKey()
    {
        var hWnd = this.TryGetPlatformHandle()!.Handle;
        User32.UnregisterHotKey(hWnd, this.GetHashCode());
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
        ViewModel.Global.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(GlobalViewModel.AutoPositionBottom))
            {
                TryMoveWindowToCenter();
            }
        };
    }

    private void OnHotKeyPressed()
    {
        ViewModel.IsMouseEntered = !ViewModel.IsMouseEntered;
    }

    private void OnDragLeave(object? sender, DragEventArgs e)
    {
        ViewModel.IsDragMode = false;
    }

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        ViewModel.IsDragMode = true;
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        var index = PosXToIndex(e.GetPosition(DockItemList).X);
        foreach (var data in e.Data.GetFiles() ?? [])
        {
            ViewModel.InsertDockLinkItem(index, data.Path.LocalPath);
        }
        ViewModel.IsDragMode = false;
        ViewModel.Logger.Debug("OnDrop {Index}", index);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (ViewModel.IsMoveMode)
        {
            if (e.Pointer.Type is PointerType.Mouse && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                BeginMoveDrag(e);
                e.Handled = true;
            }
        }

        ViewModel.SelectedDockItem = null;
        ViewModel.Logger.Debug("Window.OnPointerPressed");
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
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
        var settingWindow = App.Instance.ServiceProvider.GetRequiredService<SettingWindow>();
        _ = settingWindow.ShowDialog(this);
        e.Handled = true;
        //if (_settingDialog is not null)
        //    return;
        //_settingDialog = new SettingDialog();
        //_settingDialog.Closed += (s, e) =>
        //{
        //    _settingDialog = null;
        //};
        //_settingDialog.ShowDialog(this);
        //e.Handled = true;
    }

    private void MoveMenuItem_Clicked(object? sender, RoutedEventArgs e)
    {
        Cursor = new Cursor(StandardCursorType.SizeAll);
        ViewModel.IsMoveMode = true;
    }

    private async void AddLinkMenuItem_Clicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            var addDockItemWindow = App.Instance.ServiceProvider.GetRequiredService<AddDockItemWindow>();
            await addDockItemWindow.ShowDialog(this);
            e.Handled = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    private void DockItem_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not DockItemControl source)
            return;

        ViewModel.SelectedDockItem = source.DockItem;
        ViewModel.Logger.Debug("DockItem.OnPointerPressed");
        e.Handled = true;
    }

    private void DeleteLinkMenuItem_Clicked(object? sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedDockItem is null)
            return;
        ViewModel.RemoveDockItem(ViewModel.SelectedDockItem.Key);
        e.Handled = true;
    }

    int PosXToIndex(double x)
    {
        var curRight = GlobalViewModel.Instance.DockItemSize / 2;
        for (int i = 0; i < ViewModel.DockItems.Count; i++)
        {
            if (x <= curRight)
                return i;
            curRight += GlobalViewModel.Instance.DockItemSize + GlobalViewModel.Instance.DockItemSpacing;
        }
        return ViewModel.DockItems.Count;
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        switch (GlobalViewModel.Instance.DockPanelPosition)
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
        if (ViewModel.Global.IsAutoPosition)
        {
            if (Screens.ScreenFromWindow(this) is Screen screen)
            {
                // 自动靠下居中
                var realWidth = Width * screen.Scaling;
                var realHeight = Height * screen.Scaling;

                var x = screen.Bounds.X + (screen.Bounds.Width - realWidth) / 2;
                var y = screen.Bounds.Y + screen.Bounds.Height - ViewModel.Global.AutoPositionBottom - realHeight;

                Position = new PixelPoint((int)x, (int)y);
            }
        }
    }

    private void TryMoveWindowToLeft()
    {
        if (ViewModel.Global.IsAutoPosition)
        {
            if (Screens.ScreenFromWindow(this) is Screen screen)
            {
                var realWidth = Width * screen.Scaling;
                var realHeight = Height * screen.Scaling;

                var x = screen.Bounds.X;
                var y = screen.Bounds.Y + screen.Bounds.Height - ViewModel.Global.AutoPositionBottom - realHeight;

                Position = new PixelPoint((int)x, (int)y);
            }
        }
    }

    private void TryMoveWindowToRight()
    {
        if (ViewModel.Global.IsAutoPosition)
        {
            if (Screens.ScreenFromWindow(this) is Screen screen)
            {
                var realWidth = Width * screen.Scaling;
                var realHeight = Height * screen.Scaling;

                var x = screen.Bounds.X + screen.Bounds.Width - realWidth;
                var y = screen.Bounds.Y + screen.Bounds.Height - ViewModel.Global.AutoPositionBottom - realHeight;

                Position = new PixelPoint((int)x, (int)y);
            }
        }
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        ViewModel.IsMouseEntered = true;
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        ViewModel.IsMouseEntered = false;
    }

    private void MainWindowContextMenuOpened(object? sender, RoutedEventArgs e)
    {
        ViewModel.IsContextMenuShow = true;
    }

    private void MainWindowContextMenuClosed(object? sender, RoutedEventArgs e)
    {
        ViewModel.IsContextMenuShow = false;
    }
}
