using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using DockBar.AvaloniaApp.Helpers;
using DockBar.DockItem.Contacts;
using DockBar.DockItem.Items;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Zeng.CoreLibrary.Toolkit.Logging;

namespace DockBar.AvaloniaApp.Windows;

public partial class DockItemFolderWindow : Window
{
    private ILogger Logger { get; }
    private IDockItemService DockItemService { get; }
    private PixelPoint TopPosition { get; set; }

    // public static readonly StyledProperty<bool> IsShowProperty =
    //     AvaloniaProperty.Register<DockItemFolderWindow, bool>(nameof(IsShow), defaultValue: false);
    //
    // public bool IsShow
    // {
    //     get => GetValue(IsShowProperty);
    //     set => SetValue(IsShowProperty, value);
    // }


    public DockItemFolderWindow() 
    {
    }

    public DockItemFolderWindow(ILogger logger, IDockItemService dockItemService)
    {
        Logger = logger;
        DockItemService = dockItemService;

        Deactivated += OnDeactivated;

        DataContext = this;
        InitializeComponent();

        // Logger.Verbose("DockItemFolderWindow 加载");
        OnActualThemeVariantPropertyChanged(ActualThemeVariant, ActualThemeVariant);
    }

    protected override void OnResized(WindowResizedEventArgs e)
    {
        if (Screens.ScreenFromWindow(this) is { } screen)
            Position = new((int)(TopPosition.X - Bounds.Width * screen.Scaling * 0.5), TopPosition.Y);

        base.OnResized(e);
    }

    private void OnActualThemeVariantPropertyChanged(ThemeVariant oldValue, ThemeVariant newValue)
    {
        if (this.TryFindResource("SystemAltHighColor", newValue, out var resource) && resource is Color color)
            AcrylicHelper.EnableAcrylic(this, new(128, color.R, color.G, color.B));
    }

    private void OnDeactivated(object? sender, EventArgs e)
    {
        HideFolder();
    }

    public void ShowFolder(DockItemFolder folder, int x, int y)
    {
        Logger.Trace().Verbose("打开停靠文件夹窗口 {FolderKey}", folder.Key);
        TopPosition = new(x, y);

        ItemGrid.Children.Clear();
        int dockItemX = 0, dockItemY = 0;
        // foreach (var dockItem in folder.Select(k => DockItemService.GetDockItem(k)).OfType<DockItemBase>())
        //     if (DockItemIconConverter.Instance.Convert(
        //             dockItem.IconData,
        //             typeof(IImage),
        //             null,
        //             CultureInfo.CurrentCulture
        //         ) is IImage icon)
        //     {
        //         var dockItemControl = new DockItemControl
        //         {
        //             DockItemId = dockItem.Key,
        //             DockIcon = icon,
        //             ShowName = dockItem.ShowName,
        //             Command = new RelayCommand<int>(k => DockItemService.ExecuteDockItem(k)),
        //             CommandParameter = dockItem.Key,
        //             Width = Program.ServiceProvider.GetRequiredService<IAppSettingWrapper>().Data.DockItemSize,
        //             Height = Program.ServiceProvider.GetRequiredService<IAppSettingWrapper>().Data.DockItemSize
        //         };
        //         Grid.SetColumn(dockItemControl, dockItemX);
        //         Grid.SetRow(dockItemControl, dockItemY);
        //         ItemGrid.Children.Add(dockItemControl);
        //         dockItemX++;
        //         if (dockItemX % ItemGrid.ColumnDefinitions.Count == 0)
        //         {
        //             dockItemX = 0;
        //             dockItemY++;
        //         }
        //     }

        Position = new((int)TopPosition.X, TopPosition.Y);

        Show();
        // IsShow = true;

        Classes.Remove("ToHide");

        if (Screens.ScreenFromWindow(this) is { } screen)
            Position = new((int)(TopPosition.X - Bounds.Width * screen.Scaling * 0.5), TopPosition.Y);


        Owner = Program.ServiceProvider.GetRequiredKeyedService<Window>(nameof(MainWindow));

        Activate();
    }

    private IDisposable? HideTimer { get; set; }

    public void HideFolder()
    {

        Classes.Add("ToHide");

        HideTimer?.Dispose();
        HideTimer = DispatcherTimer.RunOnce(() => { Hide(); }, TimeSpan.FromSeconds(0.063));
        // Hide();
    }

    public override void Hide()
    {
        // 恢复焦点
        var owner = Owner;
        base.Hide();
        owner?.Activate();
    }
}