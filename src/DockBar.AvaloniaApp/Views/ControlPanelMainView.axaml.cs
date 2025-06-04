using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using DockBar.AvaloniaApp.ViewModels;
using DockBar.Core.Helpers;

namespace DockBar.AvaloniaApp.Views;

internal partial class ControlPanelMainView : UserControl
{
    public MainViewModel ViewModel => (DataContext as MainViewModel)!;

    public ControlPanelMainView() : this(new())
    {
    }

    public ControlPanelMainView(MainViewModel mainViewModel)
    {
        using var _ = LogHelper.Trace();

        DataContext = mainViewModel;
        InitializeComponent();

        ViewModel.Logger.Information("主页页面加载完毕");
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        using var _ = LogHelper.Trace();
        base.OnUnloaded(e);

        ViewModel.Logger.Debug("主页页面卸载");
    }
}