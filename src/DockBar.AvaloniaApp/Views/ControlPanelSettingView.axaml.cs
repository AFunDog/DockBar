using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using DockBar.AvaloniaApp.ViewModels;
using DockBar.Core.Contacts;
using DockBar.Core.Helpers;
using DockBar.Core.Structs;
using Serilog;

namespace DockBar.AvaloniaApp.Views;

internal partial class ControlPanelSettingView : UserControl
{
    // public SettingViewModel ViewModel => (DataContext as SettingViewModel)!;

    public ILogger Logger { get; }

    public IAppSettingWrapper AppSettingWrapper { get; }

    public ControlPanelSettingView() : this(Log.Logger, IAppSettingWrapper.Empty)
    {
    }

    public ControlPanelSettingView(ILogger logger, IAppSettingWrapper appSettingWrapper)
    {
        Logger = logger;
        AppSettingWrapper = appSettingWrapper;

        DataContext = this;
        InitializeComponent();

        Logger.Information("设置页面加载完毕");
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        using var _ = LogHelper.Trace();
        base.OnUnloaded(e);

        Logger.Debug("设置页面卸载");
    }
}