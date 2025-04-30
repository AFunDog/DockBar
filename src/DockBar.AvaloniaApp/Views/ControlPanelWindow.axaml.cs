using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using DockBar.AvaloniaApp;
using DockBar.AvaloniaApp.ViewModels;
using DockBar.Core.Helpers;

namespace DockBar.AvaloniaApp.Views;

internal partial class ControlPanelWindow : Window
{
    internal ControlPanelWindowViewModel ViewModel => (DataContext as ControlPanelWindowViewModel)!;

    public ControlPanelWindow()
        : this(new()) { }

    public ControlPanelWindow(ControlPanelWindowViewModel viewModel)
    {
        using var _ = LogHelper.Trace();
        DataContext = viewModel;
        InitializeComponent();

        ViewModel.Logger.Information("ControlPanelWindow 启动");
    }

    protected override void OnClosed(EventArgs e)
    {
        using var _ = LogHelper.Trace();
        base.OnClosed(e);
        ViewModel.NavigateService.Unbind();
        ViewModel.Logger.Information("ControlPanelWindow 关闭");
    }

    private void CloseButton_Clicked(object? sender, RoutedEventArgs e)
    {
        Close();
        e.Handled = true;
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        ViewModel.AppSettingWrapper.Save(App.SettingFile);
    }
}
