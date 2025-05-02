using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.VisualTree;
using CoreLibrary.Toolkit.Avalonia.Structs;
using DockBar.AvaloniaApp;
using DockBar.AvaloniaApp.Controls;
using DockBar.AvaloniaApp.Helpers;
using DockBar.AvaloniaApp.Structs;
using DockBar.AvaloniaApp.ViewModels;
using DockBar.AvaloniaApp.Views;
using DockBar.Core.Helpers;
using DockBar.Core.Structs;
using DockBar.DockItem.Structs;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;
using static Windows.Win32.PInvoke;
using Color = Avalonia.Media.Color;

namespace DockBar.AvaloniaApp.Views;

internal partial class MainWindow : Window
{
    private const uint WM_TRAYICON = WM_USER + 1;

    private bool IsRightPressed { get; set; }

    internal MainWindowViewModel ViewModel => (DataContext as MainWindowViewModel)!;

    public MainWindow()
        : this(new()) { }

    public MainWindow(MainWindowViewModel viewModel)
    {
        using var _ = LogHelper.Trace();
        DataContext = viewModel;
        InitializeComponent();

        ViewModel.GlobalHotKeyManager.Host = this;
        ViewModel.GlobalHotKeyManager.Bind(nameof(AppSetting.KeepMainWindowHotKey), KeyMainWindow);
        DockPanelGrid.PropertyChanged += (s, e) =>
        {
            if (e.Property == HeightProperty)
            {
                TryMoveWindowToTopCenter();
            }
        };

        DockItemPanel.AddHandler(DragDrop.DropEvent, OnDockItemPanelDrop);
        DockItemPanel.AddHandler(DragDrop.DragEnterEvent, OnDockItemPanelDragEnter);
        DockItemPanel.AddHandler(DragDrop.DragLeaveEvent, OnDockItemPanelDragLeave);

        BottomBorder.AddHandler(DragDrop.DragEnterEvent, OnBottomBorderDragEnter);
        //DockItemPanel.AddHandler(DragDrop.drag)
        AddHandler(DockItemControl.DockItemStartDragEvent, OnDockItemControlStartDrag);
        AddHandler(DockItemControl.DockItemEndDragEvent, OnDockItemControlEndDrag);
        Win32Properties.AddWndProcHookCallback(this, WndProcHook);
        ChangeWindowStyle();
        // RegisterAppHotKey();
        RegisterDockItemKeyAction();
        CreateTrayIcon();

        AcrylicHelper.EnableAcrylic(this, Colors.Transparent);

        ViewModel.Logger.Verbose("MainWindow 启动");
    }

    // 不一定能监听到
    private void OnDockItemControlEndDrag(object? sender, RoutedEventArgs e)
    {
        ViewModel.IsDockItemDraging = false;
        e.Handled = true;
        ViewModel.Logger.Verbose("OnDockItemControlEndDrag");
    }

    private void OnDockItemControlStartDrag(object? sender, RoutedEventArgs e)
    {
        ViewModel.IsDockItemDraging = true;
        e.Handled = true;
        ViewModel.Logger.Verbose("OnDockItemControlStartDrag");
    }

    private void OnBottomBorderDragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains(DataFormats.Files))
        {
            //ViewModel.IsDragMode = true;
            ViewModel.IsMouseEntered = true;
            ViewModel.Logger.Debug("BottomBorderDragEnter");
        }
    }

    /// <summary>
    /// 创建托盘菜单图标
    /// </summary>
    private void CreateTrayIcon()
    {
        using var _ = LogHelper.Trace();
        if (TryGetPlatformHandle() is not { } handle)
        {
            ViewModel.Logger.Error("TryGetPlatformHandle() 的返回结果是 null 无法注册托盘图标");
            return;
        }

        try
        {
            unsafe
            {
                NOTIFYICONDATAW nid;
                nid.cbSize = (uint)sizeof(NOTIFYICONDATAW);
                nid.hWnd = (HWND)handle.Handle;
                nid.uID = 0;
                nid.uFlags = NOTIFY_ICON_DATA_FLAGS.NIF_ICON | NOTIFY_ICON_DATA_FLAGS.NIF_MESSAGE | NOTIFY_ICON_DATA_FLAGS.NIF_TIP;
                nid.uCallbackMessage = WM_TRAYICON;
                nid.hIcon = (HICON)(new Bitmap(AssetLoader.Open(IconResource.AppIconUri)).GetHicon());
                nid.szTip = "工具栏";

                Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_ADD, &nid);
            }

            ViewModel.Logger.Information("创建托盘图标");
        }
        catch (Exception e)
        {
            ViewModel.Logger.Error(e, "创建托盘图标时错误");
        }
    }

    /// <summary>
    /// 覆盖窗口的类型
    /// </summary>
    private void ChangeWindowStyle()
    {
        // 让窗口不会显示在Alt+Tab的窗口管理器中
        static (uint style, uint exStyle) WindowStylesProcHook(uint style, uint exStyle)
        {
            return (style, exStyle | (uint)WINDOW_EX_STYLE.WS_EX_TOOLWINDOW);
        }

        Win32Properties.AddWindowStylesCallback(this, WindowStylesProcHook);
    }

    /// <summary>
    /// 通过注册按键来实现全局按键响应
    /// 响应打开面板或关闭面板
    /// </summary>
    private void RegisterAppHotKey()
    {
        using var _ = LogHelper.Trace();
        try
        {
            if (TryGetPlatformHandle()?.Handle is { } hWnd)
            {
                if (RegisterHotKey(new(hWnd), this.GetHashCode(), HOT_KEY_MODIFIERS.MOD_CONTROL, (uint)VIRTUAL_KEY.VK_SPACE))
                {
                    ViewModel.Logger.Information("注册全局热键成功");
                    return;
                }
            }
        }
        catch (Exception e)
        {
            ViewModel.Logger.Error(e, "注册全局热键失败");
            return;
        }

        ViewModel.Logger.Warning("未能注册全局热键");
    }

    /// <summary>
    /// 注销全局按键响应
    /// </summary>
    private void UnregisterAppHotKey()
    {
        using var _ = LogHelper.Trace();
        try
        {
            if (TryGetPlatformHandle()?.Handle is { } hWnd)
            {
                if (UnregisterHotKey(new(hWnd), this.GetHashCode()))
                {
                    ViewModel.Logger.Information("注销全局热键成功");
                    return;
                }
            }
        }
        catch (Exception e)
        {
            ViewModel.Logger.Error(e, "注销全局热键失败");
            return;
        }

        ViewModel.Logger.Warning("未能注销全局热键");
    }

    private void RegisterDockItemKeyAction()
    {
        KeyActionDockItems.KeyActions["Setting"] = OpenSettingWindow;
    }

    /// <summary>
    /// 覆盖处理窗口进程事件
    /// </summary>
    private nint WndProcHook(nint hWnd, uint msg, nint wParam, nint lParam, ref bool handled)
    {
        switch (msg)
        {
            // case WM_HOTKEY:
            //     if (wParam == this.GetHashCode())
            //     {
            //         OnHotKeyPressed();
            //         handled = true;
            //     }
            //
            //     break;
            case WM_TRAYICON:
                if (lParam == WM_RBUTTONUP)
                {
                    // 打开托盘图标的菜单
                    GetCursorPos(out var point);
                    Program.ServiceProvider.GetRequiredService<MenuWindow>().ShowMenu(point.X, point.Y);
                    handled = true;
                }

                break;
        }

        return nint.Zero;
    }

    private void KeyMainWindow()
    {
        if (IsActive)
        {
            ViewModel.IsHotKeyPressed = !ViewModel.IsHotKeyPressed;
        }
        else
        {
            ViewModel.IsHotKeyPressed = true;
            Activate();
        }
    }

    private void OnDockItemPanelDragLeave(object? sender, DragEventArgs e)
    {
        using var _ = LogHelper.Trace();
        ViewModel.IsDragMode = false;
        ViewModel.Logger.Verbose("{Name} 关闭拖拽模式", nameof(DockItemList));
    }

    private void OnDockItemPanelDragEnter(object? sender, DragEventArgs e)
    {
        using var _ = LogHelper.Trace();
        ViewModel.IsDragMode = true;
        ViewModel.Logger.Verbose("{Name} 启用拖拽模式", nameof(DockItemList));
    }

    private void OnDockItemPanelDrop(object? sender, DragEventArgs e)
    {
        using var _ = LogHelper.Trace();
        int targetIndex = -1;
        if (e.Data.Contains(DataFormats.Files))
        {
            targetIndex = PosXToIndex(e.GetPosition(DockItemPanel).X);
            ViewModel.Logger.Verbose("Drop 文件数据 在 {Index}", targetIndex);
            foreach (var data in e.Data.GetFiles() ?? [])
            {
                ViewModel.DockItemService.RegisterDockItem(targetIndex, new DockLinkItem { LinkPath = data.Path.LocalPath });
            }
        }
        else if (e.Data.Contains("key"))
        {
            // 由于不是插入所以按整个 DockItem 进行位置索引转化
            targetIndex = PosXToIndex(e.GetPosition(DockItemPanel).X - ViewModel.AppSetting.DockItemSize / 2);
            ViewModel.Logger.Verbose("Drop 字符串数据 在 {Index}", targetIndex);
            var data = e.Data.Get("key");
            if (data is int key)
            {
                ViewModel.DockItemService.MoveDockItemTo(key, targetIndex);
            }
        }

        ViewModel.IsDragMode = false;
        ViewModel.Logger.Debug("OnDrop {Index}", targetIndex);
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        // UnregisterAppHotKey();
        ViewModel.GlobalHotKeyManager.Host = null;
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        ViewModel.Logger.Verbose("MainWindow 关闭");
    }

    private void OpenRightMenu(int x, int y)
    {
        try
        {
            ViewModel.HasOwnedWindow = true;

            var menuWindow = Program.ServiceProvider.GetRequiredService<MenuWindow>();
            menuWindow.PropertyChanged += OnMenuWindowPropertyChanged;
            menuWindow.ShowMenu(x, y, true);
        }
        catch (Exception ex)
        {
            ViewModel.Logger.Error(ex, "打开右键菜单失败");
        }
    }

    private void OnMenuWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        // 当菜单可见时，保持面板打开
        // 前提时 atWindow
        if (e.Property == MenuWindow.IsVisibleProperty)
        {
            ViewModel.HasOwnedWindow = e.GetNewValue<bool>();
            if (!ViewModel.HasOwnedWindow)
            {
                if (sender is MenuWindow menu)
                    menu.PropertyChanged -= OnMenuWindowPropertyChanged;
            }
        }
    }

    private void OnRightClick(PointerReleasedEventArgs e)
    {
        var point = e.GetPosition(this);
        var t = this.PointToScreen(point);
        OpenRightMenu(t.X, t.Y);
    }

    private bool TryHandleRightMouseButton(PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            IsRightPressed = true;
            e.Handled = true;
        }
        else
        {
            IsRightPressed = false;
        }

        return e.Handled;
    }

    private void TryOpenMenuWindow(PointerReleasedEventArgs e)
    {
        if (
            IsRightPressed
            && e.GetCurrentPoint(this).Properties.PointerUpdateKind is PointerUpdateKind.RightButtonReleased
            && e.Source is Visual source
            && source.GetVisualsAt(e.GetPosition(source)).Any(c => c == source || source.IsVisualAncestorOf(c))
        )
        {
            // 不重置 IsRightPressed 是因为要能不停顿地再次打开菜单
            // IsRightPressed = false;
            OnRightClick(e);
            e.Handled = true;
        }
    }

    private double SelectDockPanel(PointerEventArgs e)
    {
        if (e.Source is not DockItemControl)
        {
            // 重置 SelectedDockItem
            ViewModel.SelectedDockItem = null;
        }

        double x = e.GetPosition(DockItemList).X;
        ViewModel.SelectedIndex = PosXToIndex(x);
        ViewModel.Logger.Debug("其他项都未被选择 默认选中 DockPanel");
        return x;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        using var _ = LogHelper.Trace();

        // 尝试处理右键事件
        TryHandleRightMouseButton(e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        // 处理当前选择的项是什么
        // 会影响打开菜单的模式
        switch (e.Source)
        {
            case DockItemControl { DockItemKey: { } itemKey }:
                SelectDockItem(itemKey);
                break;
            default:
                SelectDockPanel(e);
                break;
        }

        TryOpenMenuWindow(e);
    }

    private void SelectDockItem(int itemKey)
    {
        ViewModel.SelectedDockItem = ViewModel.DockItemService.GetDockItem(itemKey);
        ViewModel.Logger.Debug(
            "DockItem 被选中 {DockItemKey} {DockItemIndex}",
            ViewModel.SelectedDockItem?.Key,
            ViewModel.SelectedDockItem?.Index
        );
    }

    private void DockItem_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        // 重置 Source 为 DockItemControl
        e.Source = sender;
    }

    private async void OpenSettingWindow()
    {
        try
        {
            var settingWindow = Program.ServiceProvider.GetRequiredService<ControlPanelWindow>();
            ViewModel.HasOwnedWindow = true;
            await settingWindow.ShowDialog(this);
        }
        catch (Exception ex)
        {
            ViewModel.Logger.Error(ex, "打开设置窗口失败");
        }

        ViewModel.HasOwnedWindow = false;
    }

    int PosXToIndex(double x)
    {
        var curRight = ViewModel.AppSetting.DockItemSize / 2;
        for (int i = 0; i < ViewModel.DockItems.Count; i++)
        {
            if (x <= curRight)
                return i;
            curRight += ViewModel.AppSetting.DockItemSize + ViewModel.AppSetting.DockItemSpacing;
        }

        return ViewModel.DockItems.Count;
    }

    protected override Avalonia.Size MeasureOverride(Avalonia.Size availableSize)
    {
        TryMoveWindowToTopCenter();
        return base.MeasureOverride(availableSize);
    }

    private void TryMoveWindowToTopCenter()
    {
        if (Screens.ScreenFromWindow(this) is Screen screen)
        {
            var realWidth = Width * screen.Scaling;
            var realHeight = Height * screen.Scaling;

            var x = screen.Bounds.X + (screen.Bounds.Width - realWidth) / 2;
            var y = -(DockPanelGrid.Bounds.Height) * screen.Scaling;
            Position = new PixelPoint((int)x, (int)y);
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
        ViewModel.IsMenuShow = true;
    }

    private void MainWindowContextMenuClosed(object? sender, RoutedEventArgs e)
    {
        ViewModel.IsMenuShow = false;
    }
}
