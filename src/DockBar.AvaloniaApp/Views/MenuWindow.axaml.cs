using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Input;
using DockBar.AvaloniaApp.Contacts;
using DockBar.AvaloniaApp.Helpers;
using DockBar.AvaloniaApp.Structs;
using DockBar.AvaloniaApp.ViewModels;
using DockBar.Core.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using static Windows.Win32.PInvoke;

namespace DockBar.AvaloniaApp.Views;

internal partial class MenuWindow : Window
{
    //public MenuWindowViewModel ViewModel => (DataContext as MenuWindowViewModel)!;

    private ILogger Logger { get; }

    private MainWindow MainWindow { get; set; }

    public MenuWindow()
        : this(Log.Logger, new()) { }

    public MenuWindow(ILogger logger, MainWindow mainWindow)
    {
        Logger = logger;
        MainWindow = mainWindow;
        MainWindow.ViewModel.PropertyChanged += (_, _) =>
        {
            // Logger.Debug("MainWindowViewModelPropertyChange {Data}",MainWindow.ViewModel.SelectedDockItem);
            EditMenuItemCommand.NotifyCanExecuteChanged();
            RemoveDockItemCommand.NotifyCanExecuteChanged();
        };
        Owner = MainWindow;
        DataContext = this;
        Deactivated += OnDeactivated;
        InitializeComponent();

        ChangeWindowStyle();

        RenderOptions.SetTextRenderingMode(this, TextRenderingMode.Antialias);

        AcrylicHelper.EnableAcrylic(this, Colors.Transparent);
        // AcrylicHoster.ApplyWithTheme(this);
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

    protected override void OnLostFocus(RoutedEventArgs e)
    {
        base.OnLostFocus(e);
        CheckLostFocus();
    }

    private void CheckLostFocus()
    {
        //if (Owner is not null)
        //{
        //    if (Owner.IsFocused || IsFocused)
        //        return;
        //}
        //if (IsFocused is false)
        //    Hide();
    }

    public void ShowMenu(int x, int y, bool atWindow = false)
    {
        using var _ = LogHelper.Trace();
        // 调整菜单位置让其不会超出屏幕
        if (Screens.ScreenFromWindow(this) is { } screen)
        {
            if (x + Width * screen.Scaling > screen.WorkingArea.Right)
                x -= (int)(Width * screen.Scaling);
            if (y + Height * screen.Scaling > screen.WorkingArea.Bottom)
                y -= (int)(Height * screen.Scaling);
            Position = new(x, y);
        }

        Logger.Debug("展示菜单窗口 {X} {Y}", x, y);
        // Deactivated += OnDeactivated;
        Show();
        Activate();
    }

    void OnDeactivated(object? sender, EventArgs e)
    {
        //ContextMenu contextMenu = new();
        //if(sender is WindowBase)
        HideMenu(sender);
    }

    void HideMenu(object? sender)
    {
        Hide();
        // if (sender is MainWindow window)
        // {
        //     window.Deactivated -= OnDeactivated;
        // }
        // else
        // {
        //     Deactivated -= OnDeactivated;
        // }
    }

    //public void ShowMenu(int x, int y)
    //{
    //    void OnDeactivated(object? sender, EventArgs e)
    //    {
    //        Hide();
    //        Deactivated -= OnDeactivated;
    //    }
    //    if (Screens.ScreenFromWindow(this) is { } screen)
    //    {
    //        if (x + Width * screen.Scaling > screen.WorkingArea.Right)
    //            x -= (int)(Width * screen.Scaling);
    //        if (y + Height * screen.Scaling > screen.WorkingArea.Bottom)
    //            y -= (int)(Height * screen.Scaling);
    //        Position = new(x, y);
    //    }
    //    Deactivated += OnDeactivated;
    //    Show();
    //}

    [RelayCommand]
    private void AddDockItem()
    {
        void OpenEditDockItemWindow()
        {
            try
            {
                var addDockItemWindow = Program.ServiceProvider.GetRequiredService<EditDockItemWindow>();
                addDockItemWindow.ViewModel.IsAddMode = true;
                addDockItemWindow.ViewModel.Index = MainWindow.ViewModel.SelectedIndex;
                MainWindow.ViewModel.HasOwnedWindow = true;
                addDockItemWindow.Show(MainWindow);
                addDockItemWindow.Closing += (s, e) =>
                {
                    MainWindow.ViewModel.HasOwnedWindow = false;
                };
            }
            catch (Exception ex)
            {
                MainWindow.ViewModel.Logger.Error(ex, "打开 EditDockItemWindow 异常");
            }
        }

        OpenEditDockItemWindow();
        HideMenu(this);
    }

    public bool CanEditDockItem() => MainWindow.ViewModel.SelectedDockItem is not null;

    [RelayCommand(CanExecute = nameof(CanEditDockItem))]
    private void EditMenuItem()
    {
        void OpenEditDockItemWindow()
        {
            if (MainWindow.ViewModel.SelectedDockItem is null)
                return;
            try
            {
                var editDockItemWindow = Program.ServiceProvider.GetRequiredService<EditDockItemWindow>();
                editDockItemWindow.ViewModel.IsAddMode = false;
                editDockItemWindow.ViewModel.Index = MainWindow.ViewModel.SelectedIndex;
                editDockItemWindow.ViewModel.CurrentDockItem = MainWindow.ViewModel.SelectedDockItem;
                MainWindow.ViewModel.HasOwnedWindow = true;
                editDockItemWindow.Show(MainWindow);
            }
            catch (Exception ex)
            {
                MainWindow.ViewModel.Logger.Error(ex, "打开 EditDockItemWindow 异常");
            }

            MainWindow.ViewModel.HasOwnedWindow = false;
        }

        OpenEditDockItemWindow();
        HideMenu(this);
    }

    public bool CanRemoveDockItem() => MainWindow.ViewModel.SelectedDockItem is not null;

    [RelayCommand(CanExecute = nameof(CanRemoveDockItem))]
    private void RemoveDockItem()
    {
        if (MainWindow.ViewModel.SelectedDockItem is null)
            return;
        MainWindow.ViewModel.DockItemService?.UnregisterDockItem(MainWindow.ViewModel.SelectedDockItem.Key);
        HideMenu(this);
    }

    [RelayCommand]
    private void Exit()
    {
        if (App.Current?.ApplicationLifetime is IControlledApplicationLifetime lifetime)
        {
            lifetime.Shutdown();
        }

        HideMenu(this);
    }

    [RelayCommand]
    private void OpenSettingWindow()
    {
        try
        {
            var settingWindow = Program.ServiceProvider.GetRequiredService<ControlPanelWindow>();
            settingWindow.Show(MainWindow);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "打开设置窗口失败");
        }

        HideMenu(this);
    }
}
