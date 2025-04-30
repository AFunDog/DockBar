using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DockBar.AvaloniaApp.Views;
using DockBar.Core.Helpers;
using DockBar.SystemMonitor;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Context;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace DockBar.AvaloniaApp;

public partial class App : Application
{
    public static App Instance => (App)Current!;

    public ILogger Logger { get; }

    public const string SettingFile = ".settings";
    public const string StorageFile = ".dockItems";

    public App()
        : this(Log.Logger) { }

    public App(ILogger logger)
    {
        DataContext = this;
        Logger = logger;
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();
            desktop.MainWindow = Program.ServiceProvider.GetRequiredService<MainWindow>();
            //new Avalonia.Media.Imaging.Bitmap(AssetLoader.Open(new System.Uri("avares://DockBar.AvaloniaApp/Assets/icon.ico"))).Save(
            //    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "icon.png")
            //);
            //Logger.Debug();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove = BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }

    [RelayCommand]
    private void Shutdown()
    {
        if (ApplicationLifetime is IControlledApplicationLifetime desktop)
        {
            desktop.Shutdown();
            Logger.Debug("执行Shutdown命令");
        }
    }
}
