using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Win32.UI.WindowsAndMessaging;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using DockBar.AvaloniaApp.Helpers;
using DockBar.AvaloniaApp.Structs;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Zeng.CoreLibrary.Toolkit.Logging;

namespace DockBar.AvaloniaApp.Windows;

internal partial class MenuWindow : Window
{


    public MenuWindow()
    {
        Deactivated += OnDeactivated;
        InitializeComponent();

        ChangeWindowStyle();

        OnActualThemeVariantPropertyChanged(ActualThemeVariant, ActualThemeVariant);

        WeakReferenceMessenger.Default.Register<MenuWindow, EventArgs, string>(
            this,
            "MenuWindow.CloseMenu",
            (s, e) => { CloseMenu(); }
        );

        WeakReferenceMessenger.Default.Register<MenuWindow, OpenMenuWindowMessage, string>(
            this,
            "MenuWindow.OpenMenu",
            (s, e) => { OpenMenu(e.Pos.X, e.Pos.Y); }
        );
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Log.Logger.Trace().Debug("MenuWindow OnPointerPressed");
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        Log.Logger.Trace().Debug("MenuWindow OnPointerReleased");
    }

    private void OnActualThemeVariantPropertyChanged(ThemeVariant oldValue, ThemeVariant newValue)
    {
        if (this.TryFindResource("SystemAltHighColor", newValue, out var resource) && resource is Color color)
            AcrylicHelper.EnableAcrylic(this, new(144, color.R, color.G, color.B));
    }

    /// <summary>
    /// 覆盖改变窗口类型
    /// <br/>
    /// 让窗口不会显示在Alt+Tab的窗口管理器中
    /// </summary>
    private void ChangeWindowStyle()
    {
        static (uint style, uint exStyle) WindowStylesProcHook(uint style, uint exStyle)
        {
            return (style, exStyle | (uint)WINDOW_EX_STYLE.WS_EX_TOOLWINDOW);
        }

        Win32Properties.AddWindowStylesCallback(this, WindowStylesProcHook);
    }

    private bool IsOpened { get; set; }
    public void OpenMenu(int x, int y)
    {
        if (IsOpened)
            return;
        IsOpened = true;
        if (IsLoaded)
        {
            OpenMenuLoaded(this, new RoutedEventArgs());
        }
        else
        {
            Loaded += OpenMenuLoaded;
        }

        var rect = Screens.All.Select(s => s.Bounds).Aggregate((r, rr) => rr.Union(r));

        Show(Program.ServiceProvider.GetRequiredKeyedService<Window>(nameof(MainWindow)));
        Position = rect.BottomRight + new PixelPoint(32, 32);
        return;

        void OpenMenuLoaded(object? s, RoutedEventArgs e)
        {
            Loaded -= OpenMenuLoaded;
            Dispatcher.UIThread.Post(() =>
                {
                    const int ExtendBottom = 24;

                    // 调整菜单位置让其不会超出屏幕
                    var width = RootShowMenu.Bounds.Width;
                    var height = RootShowMenu.Bounds.Height;
                    if (Screens.ScreenFromPoint(new(x, y)) is { } screen)
                    {
                        if (x + width * screen.Scaling + ExtendBottom > screen.WorkingArea.Right)
                            x -= (int)(width * screen.Scaling + ExtendBottom);
                        if (y + height * screen.Scaling + ExtendBottom > screen.WorkingArea.Bottom)
                            y -= (int)(height * screen.Scaling + ExtendBottom);
                    }

                    Log.Logger.Trace().Information("{Pos} {Size}", (x, y), (width, height));

                    Position = new(x, y);

                    Log.Logger.Debug("展示菜单窗口 {X} {Y}", x, y);
                    // Deactivated += OnDeactivated;

                    Classes.Add("ShowMenu");
                    // 并在之后将其设为焦点窗口，触发主窗口的 Deactivated 事件
                    Activate();

                    // ShowMenu = true;
                }
            );
        }
    }

    private void OnDeactivated(object? sender, EventArgs e)
    {
        CloseMenu();
    }

    private void CloseMenu()
    {
        if (Classes.Contains("ShowMenu") is false)
            return;
        Classes.Remove("ShowMenu");
        WeakReferenceMessenger.Default.UnregisterAll(this);
        DispatcherTimer.RunOnce(() => Close(), TimeSpan.FromSeconds(0.1));
        // Dispatcher.UIThread.InvokeAsync(async () =>
        //     {
        //         await Task.Delay(TimeSpan.FromSeconds(0.1));
        //         Close();
        //     }
        // );
    }
}