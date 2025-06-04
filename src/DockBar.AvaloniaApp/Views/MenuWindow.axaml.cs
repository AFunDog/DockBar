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
using Avalonia.Input;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using DockBar.DockItem;
using DockBar.DockItem.Items;
using static Windows.Win32.PInvoke;

namespace DockBar.AvaloniaApp.Views;

internal partial class MenuWindow : Window
{
    public static readonly StyledProperty<int> SelectedIndexProperty
        = AvaloniaProperty.Register<MenuWindow, int>(nameof(SelectedIndex));

    public int SelectedIndex
    {
        get => GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    public static readonly StyledProperty<DockItemBase?> SelectedDockItemProperty
        = AvaloniaProperty.Register<MenuWindow, DockItemBase?>(nameof(SelectedDockItem));

    public DockItemBase? SelectedDockItem
    {
        get => GetValue(SelectedDockItemProperty);
        set => SetValue(SelectedDockItemProperty, value);
    }

    public static readonly StyledProperty<bool> CanAddItemProperty
        = AvaloniaProperty.Register<MenuWindow, bool>(nameof(CanAddItem));

    public bool CanAddItem
    {
        get => GetValue(CanAddItemProperty);
        set => SetValue(CanAddItemProperty, value);
    }

    //public MenuWindowViewModel ViewModel => (DataContext as MenuWindowViewModel)!;

    private ILogger Logger { get; }
    private IDockItemService DockItemService { get; }

    // public partial int SelectedIndex { get; set; }


    // private MainWindow MainWindow { get; }

    public MenuWindow() : this(Log.Logger, IDockItemService.Empty)
    {
    }

    public MenuWindow(ILogger logger, IDockItemService dockItemService)
    {
        Logger = logger;
        DockItemService = dockItemService;
        // MainWindow = mainWindow;
        // MainWindow.ViewModel.PropertyChanged += (_, _) =>
        // {
        //     // Logger.Debug("MainWindowViewModelPropertyChange {Data}",MainWindow.ViewModel.SelectedDockItem);
        //     EditMenuItemCommand.NotifyCanExecuteChanged();
        //     RemoveDockItemCommand.NotifyCanExecuteChanged();
        // };

        DataContext = this;
        Deactivated += OnDeactivated;
        LayoutUpdated += OnLayoutUpdated;
        InitializeComponent();

        ChangeWindowStyle();

        // RenderOptions.SetTextRenderingMode(this, TextRenderingMode.Antialias);

        OnActualThemeVariantPropertyChanged(ActualThemeVariant, ActualThemeVariant);
        // AcrylicHoster.ApplyWithTheme(this);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        using var _ = LogHelper.Trace();
        Logger.Debug("MenuWindow OnPointerPressed");
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        using var _ = LogHelper.Trace();
        Logger.Debug("MenuWindow OnPointerReleased");
    }

    // TODO 这是用来展示解决打开右键菜单时，面板上下抽动的问题，之后改掉
    private void OnLayoutUpdated(object? sender, EventArgs e)
    {
        if (App.Instance.MainWindow is not null)
            App.Instance.MainWindow.NotifyIsPanelShow();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ActualThemeVariantProperty)
        {
            OnActualThemeVariantPropertyChanged(change.GetOldValue<ThemeVariant>(), change.GetNewValue<ThemeVariant>());
        }
        else if (change.Property == SelectedDockItemProperty || change.Property == SelectedIndexProperty)
        {
            EditMenuItemCommand.NotifyCanExecuteChanged();
            RemoveDockItemCommand.NotifyCanExecuteChanged();
        }
    }

    private void OnActualThemeVariantPropertyChanged(ThemeVariant oldValue, ThemeVariant newValue)
    {
        if (this.TryFindResource("SystemAltHighColor", newValue, out var resource) && resource is Color color)
            AcrylicHelper.EnableAcrylic(this, new(128, color.R, color.G, color.B));
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

    public void ShowMenu(int x, int y)
    {
        using var _ = LogHelper.Trace();

        const int ExtendBottom = 24;


        // 调整菜单位置让其不会超出屏幕
        if (Screens.ScreenFromWindow(this) is { } screen)
        {
            if (x + RootGrid.Bounds.Width * screen.Scaling + ExtendBottom > screen.WorkingArea.Right)
                x -= (int)(RootGrid.Bounds.Width * screen.Scaling);
            if (y + RootGrid.Bounds.Height * screen.Scaling + ExtendBottom > screen.WorkingArea.Bottom)
                y -= (int)(RootGrid.Bounds.Height * screen.Scaling);
            Position = new(x, y);
        }

        Logger.Debug("展示菜单窗口 {X} {Y}", x, y);
        // Deactivated += OnDeactivated;

        Show(App.Instance.MainWindow);

        // 并在之后将其设为焦点窗口，触发主窗口的 Deactivated 事件
        Activate();
    }

    private void OnDeactivated(object? sender, EventArgs e)
    {
        HideMenu();
    }

    private void HideMenu()
    {
        // Activate();
        // 恢复焦点
        var owner = Owner;
        Hide();
        owner?.Activate();
    }

    [RelayCommand(CanExecute = nameof(CanAddItem))]
    private void AddDockItem()
    {
        void OpenEditDockItemWindow()
        {
            try
            {
                var addDockItemWindow = Program.ServiceProvider.GetRequiredService<EditDockItemWindow>();
                addDockItemWindow.ViewModel.IsAddMode = true;
                addDockItemWindow.ViewModel.Index = SelectedIndex;
                addDockItemWindow.Show(App.Instance.MainWindow);
                // addDockItemWindow.Closing += (s, e) =>
                // {
                //     MainWindow.ViewModel.HasOwnedWindow = false;
                //     MainWindow.ViewModel.SelectedDockItem = null;
                // };
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "打开 EditDockItemWindow 异常");
            }
        }

        OpenEditDockItemWindow();
        HideMenu();
    }

    public bool CanEditDockItem() => SelectedDockItem is not null;

    [RelayCommand(CanExecute = nameof(CanEditDockItem))]
    private void EditMenuItem()
    {
        void OpenEditDockItemWindow()
        {
            if (SelectedDockItem is null)
                return;
            try
            {
                var editDockItemWindow = Program.ServiceProvider.GetRequiredService<EditDockItemWindow>();
                editDockItemWindow.ViewModel.IsAddMode = false;
                editDockItemWindow.ViewModel.Index = SelectedIndex;
                editDockItemWindow.ViewModel.CurrentDockItem = SelectedDockItem;
                // MainWindow.ViewModel.HasOwnedWindow = true;
                editDockItemWindow.Show(App.Instance.MainWindow);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "打开 EditDockItemWindow 异常");
            }

            // MainWindow.ViewModel.HasOwnedWindow = false;
        }

        OpenEditDockItemWindow();
        HideMenu();
    }

    public bool CanRemoveDockItem() => SelectedDockItem is not null;

    [RelayCommand(CanExecute = nameof(CanRemoveDockItem))]
    private void RemoveDockItem()
    {
        if (SelectedDockItem is null)
            return;
        DockItemService.UnregisterDockItem(SelectedDockItem.Key);
        HideMenu();
    }

    public bool CanMoveToFolder() => SelectedDockItem is not null;

    // [RelayCommand(CanExecute = nameof(CanMoveToFolder))]
    // private void MoveToFolder()
    // {
    //     if (SelectedDockItem is null) return;
    //     
    // }

    [RelayCommand]
    private void Exit()
    {
        if (App.Instance.ApplicationLifetime is IControlledApplicationLifetime lifetime)
            lifetime.Shutdown();

        HideMenu();
    }

    [RelayCommand]
    private void OpenSettingWindow()
    {
        try
        {
            var settingWindow = Program.ServiceProvider.GetRequiredService<ControlPanelWindow>();
            settingWindow.Show(App.Instance.MainWindow);
            // settingWindow.Closed += (s, e) => { App.Instance.MainWindow. };
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "打开设置窗口失败");
        }

        HideMenu();
    }
}