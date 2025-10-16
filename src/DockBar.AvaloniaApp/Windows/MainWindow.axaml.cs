using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using DockBar.AvaloniaApp.Controls;
using DockBar.AvaloniaApp.Helpers;
using DockBar.AvaloniaApp.Structs;
using DockBar.DockItem.Helpers;
using DockBar.DockItem.Items;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Zeng.CoreLibrary.Toolkit.Logging;
using static Windows.Win32.PInvoke;
using Color = Avalonia.Media.Color;

namespace DockBar.AvaloniaApp.Windows;

internal partial class MainWindow : Window
{
    // ReSharper disable InconsistentNaming
    private const uint WM_TRAYICON = WM_USER + 1;
    // ReSharper restore InconsistentNaming


    private CancellationTokenSource BackgroundTaskCts { get; } = new();

    public MainWindow()
    {
        InitializeComponent();

        DockPanelGrid.PropertyChanged += (s, e) =>
        {
            if (e.Property == HeightProperty)
                TryMoveWindowToTopCenter();
        };

        DockItemPanel.AddHandler(DragDrop.DropEvent, OnDockItemPanelDrop);
        DockItemPanel.AddHandler(DragDrop.DragEnterEvent, OnDockItemPanelDragEnter);
        DockItemPanel.AddHandler(DragDrop.DragLeaveEvent, OnDockItemPanelDragLeave);

        BottomBorder.AddHandler(DragDrop.DragEnterEvent, OnBottomBorderDragEnter);
        // AddHandler(DockItemControl.DockItemStartDragEvent, OnDockItemControlStartDrag);
        // AddHandler(DockItemControl.DockItemEndDragEvent, OnDockItemControlEndDrag);
        Win32Properties.AddWndProcHookCallback(this, WndProcHook);
        ChangeWindowStyle();
        RegisterDockItemKeyAction();
        CreateTrayIcon();

        OnActualThemeVariantPropertyChanged(ActualThemeVariant, ActualThemeVariant);

        Closed += (s, e) => { BackgroundTaskCts.Cancel(); };

        Dispatcher.UIThread.InvokeAsync(async () =>
            {
                while (BackgroundTaskCts.IsCancellationRequested is false)
                {
                    await Task.Delay(1);
                    // Log.Logger.Trace().Information("OwnedWindows : {Count}", OwnedWindows.Count);
                    WeakReferenceMessenger.Default.Send(
                        new ValueChangedMessage<bool>(OwnedWindows.Any(x => x.IsVisible)),
                        "MainWindow.HasOwnedWindow"
                    );
                }

                Log.Logger.Verbose("MainWindow 后台任务 退出");
            }
        );
    }


    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ActualThemeVariantProperty)
            OnActualThemeVariantPropertyChanged(change.GetOldValue<ThemeVariant>(), change.GetNewValue<ThemeVariant>());
    }

    private void OnActualThemeVariantPropertyChanged(ThemeVariant oldValue, ThemeVariant newValue)
    {
        if (this.TryFindResource("SystemAltHighColor", newValue, out var resource) && resource is Color color)
            AcrylicHelper.EnableAcrylic(this, new(128, color.R, color.G, color.B));
    }

    private void OnBottomBorderDragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data.Contains(DataFormats.Files))
        {
            WeakReferenceMessenger.Default.Send(new ValueChangedMessage<bool>(true), "MainWindow.IsDragMode");
            WeakReferenceMessenger.Default.Send(new ValueChangedMessage<bool>(true), "MainWindow.IsMouseEntered");
            // ViewModel.Logger.Debug("BottomBorderDragEnter");
        }
    }

    /// <summary>
    /// 创建托盘菜单图标
    /// </summary>
    private void CreateTrayIcon()
    {
        if (TryGetPlatformHandle() is not { } handle)
        {
            Log.Logger.Trace().Error("TryGetPlatformHandle() 的返回结果是 null 无法注册托盘图标");
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
                nid.uFlags = NOTIFY_ICON_DATA_FLAGS.NIF_ICON
                             | NOTIFY_ICON_DATA_FLAGS.NIF_MESSAGE
                             | NOTIFY_ICON_DATA_FLAGS.NIF_TIP;
                nid.uCallbackMessage = WM_TRAYICON;
                nid.hIcon = (HICON)new Bitmap(AssetLoader.Open(IconResource.AppIconUri)).GetHicon();
                nid.szTip = "工具栏";

                Shell_NotifyIcon(NOTIFY_ICON_MESSAGE.NIM_ADD, &nid);
            }

            Log.Logger.Trace().Information("创建托盘图标");
        }
        catch (Exception e)
        {
            Log.Logger.Trace().Error(e, "创建托盘图标");
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


    private void RegisterDockItemKeyAction()
    {
        KeyActionDockItems.KeyActions["Setting"] = OpenSettingWindow;
    }

    /// <summary>
    /// 覆盖处理窗口进程事件
    /// </summary>
    private nint WndProcHook(
        nint hWnd,
        uint msg,
        nint wParam,
        nint lParam,
        ref bool handled)
    {
        try
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
                        OpenRightMenu(point.X, point.Y, false);
                        // var menuWindow = Program.ServiceProvider.GetRequiredService<MenuWindow>();
                        // menuWindow.SelectedIndex = ViewModel.SelectedIndex;
                        // menuWindow.SelectedDockItem = ViewModel.SelectedDockItem;
                        // menuWindow.OpenMenu(point.X, point.Y);
                        handled = true;
                    }

                    break;
            }
        }
        catch (Exception e)
        {
            Log.Logger.Trace().Error(e, "");
        }


        return nint.Zero;
    }

    private void KeyMainWindow()
    {
        if (IsActive)
        {
            // ViewModel.IsHotKeyPressed = !ViewModel.IsHotKeyPressed;
        }
        else
        {
            // ViewModel.IsHotKeyPressed = true;
            Activate();
        }
    }

    private void OnDockItemPanelDragLeave(object? sender, DragEventArgs e)
    {
        WeakReferenceMessenger.Default.Send(new ValueChangedMessage<bool>(false), "MainWindow.IsDragMode");
    }

    private void OnDockItemPanelDragEnter(object? sender, DragEventArgs e)
    {
        WeakReferenceMessenger.Default.Send(new ValueChangedMessage<bool>(true), "MainWindow.IsDragMode");
    }

    private async void OnDockItemPanelDrop(object? sender, DragEventArgs e)
    {
        var targetIndex = -1;
        if (e.Data.Contains(DataFormats.Files))
        {
            targetIndex = PosXToIndex(e.GetPosition(DockItemPanel).X, true);
            Log.Logger.Trace().Verbose("Drop 文件数据 在 {Index}", targetIndex);
            foreach (var data in e.Data.GetFiles() ?? [])
            {
                WeakReferenceMessenger.Default.Send(
                    new AddDockItemMessage(
                        data.Name,
                        await IconHelper.GetIconDataFromPath(data.Path.LocalPath, LinkType.Lnk),
                        "/",
                        targetIndex,
                        "Link",
                        new Dictionary<string, string>()
                        {
                            ["LinkType"] = nameof(LinkType.Lnk), ["LinkPath"] = data.Path.LocalPath
                        }
                    ),
                    "AddDockItem"
                );

                // if (e.Source is DockItemControl { DockItemId: { } key } dockItemControl
                //     && ViewModel.DockItemService.GetDockItem(key) is DockItemFolder folder)
                //     folder.Add(item.Key);
                // else
                //     ViewModel.DockItemService.Root.Insert(targetIndex, item.Key);

                e.Handled = true;
            }
        }
        else if (e.Data.Contains("Id"))
        {
            // 由于不是插入所以按整个 DockItem 进行位置索引转化
            targetIndex = PosXToIndex(e.GetPosition(DockItemPanel).X, false);
            WeakReferenceMessenger.Default.Send(
                new MoveDockItemMessage((Guid)(e.Data.Get("Id") ?? Guid.Empty), targetIndex),
                "MoveDockItem"
            );
            // ViewModel.Logger.Verbose("Drop 字符串数据 在 {Index}", targetIndex);
            // var data = e.Data.Get("key");
            // if (data is int key && ViewModel.DockItemService.GetDockItem(key) is { } item)
            // {
            //     // ViewModel.DockItemService.MoveDockItemTo(key, targetIndex);
            //     // ViewModel.DockItemService.Root.Move(key, targetIndex);
            //     ViewModel.DockItemService.Root.Remove(item.Key);
            //     ViewModel.DockItemService.Root.Insert(targetIndex, item.Key);
            //     ViewModel.DockItemService.SaveData(App.StorageFile);
            //     e.Handled = true;
            // }
        }

        WeakReferenceMessenger.Default.Send(new ValueChangedMessage<bool>(false), "MainWindow.IsDragMode");
        // ViewModel.IsDragMode = false;
        // ViewModel.Logger.Debug("OnDrop {Index}", targetIndex);
    }

    private void OpenRightMenu(int x, int y, bool canAddItem)
    {
        try
        {
            // ViewModel.HasOwnedWindow = true;

            WeakReferenceMessenger.Default.Send(
                new OpenMenuWindowMessage(null, -1, new(x, y), canAddItem),
                "OpenMenuWindow"
            );
            // var menuWindow = Program.ServiceProvider.GetRequiredService<MenuWindow>();
            // menuWindow.PropertyChanged += OnMenuWindowPropertyChanged;
            // menuWindow.SelectedIndex = ViewModel.SelectedIndex;
            // menuWindow.SelectedDockItem = ViewModel.SelectedDockItem;
            // menuWindow.ShowMenu(x, y);
        }
        catch (Exception ex)
        {
            // ViewModel.Logger.Error(ex, "打开右键菜单失败");
        }
    }

    // private void OnMenuWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    // {
    //     // 当菜单可见时，保持面板打开
    //     // 前提时 atWindow
    //     if (e.Property == MenuWindow.IsVisibleProperty)
    //     {
    //         ViewModel.HasOwnedWindow = e.GetNewValue<bool>();
    //         if (!ViewModel.HasOwnedWindow)
    //         {
    //             if (sender is MenuWindow menu)
    //                 menu.PropertyChanged -= OnMenuWindowPropertyChanged;
    //         }
    //     }
    // }

    // private bool TryHandleRightMouseButton(PointerPressedEventArgs e)
    // {
    //     // if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
    //     // {
    //     //     IsRightPressed = true;
    //     //     e.Handled = true;
    //     // }
    //     // else
    //     // {
    //     //     IsRightPressed = false;
    //     // }
    //
    //     return e.Handled;
    // }

    private void TryOpenMenuWindow(PointerReleasedEventArgs e)
    {
        if (
            // IsRightPressed
            e.GetCurrentPoint(this).Properties.PointerUpdateKind is PointerUpdateKind.RightButtonReleased
            && e.Source is Visual source
            && source.GetVisualsAt(e.GetPosition(source)).Any(c => c == source || source.IsVisualAncestorOf(c)))
        {
            var point = e.GetPosition(this);
            var t = this.PointToScreen(point);
            OpenRightMenu(t.X, t.Y, true);
            e.Handled = true;

            // var animation = new Animation();
            // animation.Children.Add(new() { Cue = new(0) });
            // animation.RunAsync(this);
        }
    }

    private double SelectDockPanel(PointerEventArgs e)
    {
        WeakReferenceMessenger.Default.Send(new ValueChangedMessage<Guid>(Guid.Empty), "MainWindow.SelectDockItem");
        // if (e.Source is not DockItemControl)
        //     // 重置 SelectedDockItem
        //     ViewModel.SelectedDockItem = null;
        //
        var x = e.GetPosition(DockItemList).X;
        WeakReferenceMessenger.Default.Send(
            new ValueChangedMessage<int>(PosXToIndex(x, false)),
            "MainWindow.SelectIndex"
        );
        Log.Logger.Trace().Debug("其他项都未被选择 默认选中 DockPanel");
        return x;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        // ViewModel.Logger.Debug("MainWindow OnPointerRelease");


        // 处理当前选择的项是什么
        // 会影响打开菜单的模式
        switch (e.Source)
        {
            case DockItemControl { DockItemId: { } itemKey }:
                SelectDockItem(itemKey);
                break;
            default:
                SelectDockPanel(e);
                break;
        }

        TryOpenMenuWindow(e);
    }

    private void SelectDockItem(Guid itemId)
    {
        WeakReferenceMessenger.Default.Send(new ValueChangedMessage<Guid>(itemId), "MainWindow.SelectDockItem");
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
            // ViewModel.HasOwnedWindow = true;
            await settingWindow.ShowDialog(this);
        }
        catch (Exception ex)
        {
            // ViewModel.Logger.Error(ex, "打开设置窗口失败");
        }

        // ViewModel.HasOwnedWindow = false;
    }

    private int PosXToIndex(double x, bool half)
    {
        var s = DockItemList.ItemsPanelRoot as StackPanel;
        if (s is null)
            return -1;
        for (var i = 0; i < s.Children.Count; i++)
        {
            var control = s.Children[i];
            if (x <= (half ? control.Bounds.Width / 2 : control.Bounds.Width))
                return i;
            x -= (control.Bounds.Width + s.Spacing);
        }

        return s.Children.Count;
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
            var y = -DockPanelGrid.Bounds.Height * screen.Scaling;
            Position = new PixelPoint((int)x, (int)y);
        }
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        WeakReferenceMessenger.Default.Send(new ValueChangedMessage<bool>(true), "MainWindow.IsMouseEntered");
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        WeakReferenceMessenger.Default.Send(new ValueChangedMessage<bool>(false), "MainWindow.IsMouseEntered");
    }
}