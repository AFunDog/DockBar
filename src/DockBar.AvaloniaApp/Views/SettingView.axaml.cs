using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using DockBar.AvaloniaApp.ViewModels;
using DockBar.Core.Helpers;

namespace DockBar.AvaloniaApp.Views;

internal partial class SettingView : UserControl
{
    public SettingViewModel ViewModel => (DataContext as SettingViewModel)!;

    public SettingView()
        : this(new()) { }

    public SettingView(SettingViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();

        ViewModel.Logger.Information("设置页面加载完毕");
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        using var _ = LogHelper.Trace();
        base.OnUnloaded(e);

        ViewModel.Logger.Debug("设置页面卸载");
    }
}
