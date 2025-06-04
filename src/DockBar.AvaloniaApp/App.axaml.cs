using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DockBar.AvaloniaApp.Views;
using DockBar.Core.Contacts;
using DockBar.Core.Helpers;
using DockBar.Core.Structs;
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

    internal MainWindow MainWindow
        => ((ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)!.MainWindow as MainWindow) !;

    public const string SettingFile = ".settings";
    public const string StorageFile = ".dockItems";

    public App() : this(Log.Logger, IAppSettingWrapper.Empty)
    {
    }

    public App(ILogger logger, IAppSettingWrapper appSettingWrapper)
    {
        DataContext = this;
        Logger = logger;

        appSettingWrapper.Data.PropertyChanged += OnDataPropertyChanged;

        颜色主题改变(appSettingWrapper.Data);
    }

    private void OnDataPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not AppSetting setting)
            return;

        switch (e.PropertyName)
        {
            case nameof(AppSetting.颜色主题):
                颜色主题改变(setting);
                break;
        }
    }

    private void 颜色主题改变(AppSetting setting)
    {
        switch (setting.颜色主题)
        {
            case 颜色主题.跟随系统:
                RequestedThemeVariant = ThemeVariant.Default;
                break;
            case 颜色主题.深色:
                RequestedThemeVariant = ThemeVariant.Dark;
                break;
            case 颜色主题.浅色:
                RequestedThemeVariant = ThemeVariant.Light;
                break;
        }
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
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove
            = BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
            BindingPlugins.DataValidators.Remove(plugin);
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