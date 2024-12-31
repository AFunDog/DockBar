using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using DockBar.Avalonia.ViewModels;
using DockBar.Avalonia.Views;
using DockBar.Core;
using DockBar.SystemMonitor;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace DockBar.Avalonia;

public partial class App : Application
{
    public static App Instance => (App)Current!;
    public IServiceProvider ServiceProvider { get; } = BuildServices();
    public ILogger Logger => ServiceProvider.GetRequiredService<ILogger>();

    public Stream DefaultIconStream { get; } = AssetLoader.Open(new Uri("avares://DockBar.Avalonia/Assets/icon.png"));

    public const string SettingFile = "settings.bin";

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
            .AddSingleton<ILogger>(new LoggerConfiguration().WriteTo.Console().MinimumLevel.Debug().CreateLogger())
            .AddSingleton<GlobalSetting>()
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
}
