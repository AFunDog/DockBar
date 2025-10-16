using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Utilities;
using CommunityToolkit.Mvvm.Messaging;
using DockBar.AvaloniaApp.Helpers;
using DockBar.AvaloniaApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Zeng.CoreLibrary.Toolkit.Logging;

namespace DockBar.AvaloniaApp.Windows;

internal partial class ControlPanelWindow : Window
{

    public ControlPanelWindow()
    {

        InitializeComponent();

        RenderOptions.SetTextRenderingMode(this, TextRenderingMode.Antialias);

        AcrylicHelper.EnableAcrylic(this, Colors.Transparent);

        WeakReferenceMessenger.Default.Register<ControlPanelWindow,string,string>(this,"ControlPanelWindow.OpenWindow",
            (s, e) =>
            {
                Show(Program.ServiceProvider.GetRequiredKeyedService<Window>(nameof(MainWindow)));
            }
        );
    }


    protected override void OnClosed(EventArgs e)
    {
        // using var _ = LogHelper.Trace();
        base.OnClosed(e);
        WeakReferenceMessenger.Default.UnregisterAll(this);
        // ViewModel.NavigateService.Unbind();
        Log.Logger.Trace().Information("ControlPanelWindow 关闭");
    }

    private void CloseButton_Clicked(object? sender, RoutedEventArgs e)
    {
        Close();
        e.Handled = true;
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        // ViewModel.AppSettingWrapper.Save(App.SettingFile);
    }
}