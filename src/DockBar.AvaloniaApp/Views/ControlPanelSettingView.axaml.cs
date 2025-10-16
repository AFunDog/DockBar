using Avalonia.Controls;
using Avalonia.Interactivity;
using DockBar.Core.Contacts;
using Serilog;
using Zeng.CoreLibrary.Toolkit.Logging;

namespace DockBar.AvaloniaApp.Views;

internal partial class ControlPanelSettingView : UserControl
{
    // public SettingViewModel ViewModel => (DataContext as SettingViewModel)!;

    // private ILogger Logger { get; }
    //
    public IAppSettingWrapper AppSettingWrapper { get; }

    public ControlPanelSettingView(IAppSettingWrapper appSettingWrapper)
    {
        AppSettingWrapper = appSettingWrapper;
        DataContext = this;
        InitializeComponent();

        Log.Logger.Trace().Information("设置页面加载完毕");
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        Log.Logger.Trace().Debug("设置页面卸载");
    }
}