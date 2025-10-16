using System;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using DockBar.AvaloniaApp.Structs;
using DockBar.AvaloniaApp.Views;
using DockBar.AvaloniaApp.Windows;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Zeng.CoreLibrary.Toolkit.Logging;

namespace DockBar.AvaloniaApp.Services;

// TODO
internal sealed partial class WindowManager
{
    private IServiceProvider ServiceProvider { get; }
    private ILogger Logger { get; }

    public WindowManager() { }

    public WindowManager(IServiceProvider provider, ILogger logger)
    {
        ServiceProvider = provider;
        Logger = logger.ForContext<WindowManager>();

        WeakReferenceMessenger.Default.Register<WindowManager, OpenMenuWindowMessage, string>(
            this,
            "WindowManager.OpenMenuWindow",
            (s, e) =>
            {
                try
                {
                    var menuWindow = ServiceProvider.GetRequiredKeyedService<Window>(nameof(MenuWindow));
                    WeakReferenceMessenger.Default.Send(e, "MenuWindow.OpenMenu");
                }
                catch (Exception exception)
                {
                    Logger.Trace().Error(exception, "");
                }
            }
        );
        WeakReferenceMessenger.Default.Register<WindowManager, OpenEditDockItemWindowMessage, string>(
            this,
            "WindowManager.OpenEditDockItemWindow",
            (s, e) =>
            {
                try
                {
                    var editDockItemWindow
                        = ServiceProvider.GetRequiredKeyedService<Window>(nameof(EditDockItemWindow));
                    WeakReferenceMessenger.Default.Send(e, "EditDockItemWindow.OpenWindow");
                }
                catch (Exception exception)
                {
                    Logger.Trace().Error(exception, "");
                }
            }
        );
        WeakReferenceMessenger.Default.Register<WindowManager,string,string>(this,"WindowManager.OpenControlPanelWindow",
            (s, e) =>
            {
                try
                {
                    var controlPanelWindow = ServiceProvider.GetRequiredKeyedService<Window>(nameof(ControlPanelWindow));
                    WeakReferenceMessenger.Default.Send(e, "ControlPanelWindow.OpenWindow");
                }
                catch (Exception exception)
                {
                    Logger.Trace().Error(exception,"");
                }
            }
        );
    }
}