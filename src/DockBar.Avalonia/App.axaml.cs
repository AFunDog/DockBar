using System;
using System.IO;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using DockBar.Avalonia.ViewModels;
using DockBar.Avalonia.Views;
using DockBar.Core;
using DockBar.Shared.Helpers;
using DockBar.SystemMonitor;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Context;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace DockBar.Avalonia;

public partial class App : Application
{
    public static App Instance => (App)Current!;
    public IServiceProvider ServiceProvider { get; } = BuildServices();
    public ILogger Logger => ServiceProvider.GetRequiredService<ILogger>();

    public const string SettingFile = "settings.bin";
    public const string StorageFile = "dockItems.bin";

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            BindingPlugins.DataValidators.RemoveAt(0);
            desktop.MainWindow = ServiceProvider.GetRequiredService<MainWindow>();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static IServiceProvider BuildServices()
    {
        var services = new ServiceCollection()
            // 注册外部服务
            .UseDockItemService()
            .AddSingleton<ILogger>(BuildLogger())
            .AddSingleton<AppSetting>()
            .AddSingleton<PerformanceMonitor>()
            // 注册视图模型
            .AddTransient<MainWindowViewModel>()
            .AddTransient<SettingWindowViewModel>()
            .AddTransient<EditDockItemWindowViewModel>()
            // 注册窗口
            .AddSingleton<MainWindow>(static provider => new MainWindow
            {
                DataContext = provider.GetRequiredService<MainWindowViewModel>()
            })
            // 注册模态弹窗（可能以后做优化）
            .AddTransient<SettingWindow>(static provider => new SettingWindow
            {
                DataContext = provider.GetRequiredService<SettingWindowViewModel>()
            })
            .AddTransient<EditDockItemWindow>(static provider => new EditDockItemWindow
            {
                DataContext = provider.GetRequiredService<EditDockItemWindowViewModel>()
            });
        return services.BuildServiceProvider();
    }

    private static Logger BuildLogger()
    {
        //AnsiConsoleTheme.Code.
        return new LoggerConfiguration()
            .Enrich.FromLogContext()
            .MinimumLevel.Verbose()
#if DEBUG
            .WriteTo.Console(
                outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Caller} {Message:lj}{NewLine}{Exception}",
                restrictedToMinimumLevel: LogEventLevel.Debug
            )
#endif
            .WriteTo.File(
                "Log/log.txt",
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Caller} {Message:lj}{NewLine}{Exception}",
                restrictedToMinimumLevel: LogEventLevel.Verbose,
                rollingInterval: RollingInterval.Day,
                rollOnFileSizeLimit: true
            )
            .CreateLogger();
    }
}
