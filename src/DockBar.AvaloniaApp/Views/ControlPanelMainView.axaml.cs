using Avalonia.Controls;
using Avalonia.Interactivity;
using DockBar.AvaloniaApp.ViewModels;
using Serilog;
using Zeng.CoreLibrary.Toolkit.Logging;

namespace DockBar.AvaloniaApp.Views;

internal partial class ControlPanelMainView : UserControl
{
    // public MainViewModel ViewModel => (DataContext as MainViewModel)!;



    public ControlPanelMainView()
    {
        InitializeComponent();

        Log.Logger.Trace().Information("主页页面加载完毕");
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        Log.Logger.Trace().Debug("主页页面卸载");
    }
}