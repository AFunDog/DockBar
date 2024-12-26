using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using CoreLibrary.Toolkit.Services.Setting;
using DockBar.Avalonia.ViewModels;
using DockBar.Avalonia.Views;
using DockBar.Core;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace DockBar.Avalonia;

public partial class App : Application
{
    public IServiceProvider ServiceProvider { get; } = BuildServices();

    public static App Instance => (App)Current!;

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
            .AddSingleton<ILogger>(new LoggerConfiguration().WriteTo.Debug().MinimumLevel.Debug().CreateLogger())
            .AddSingleton<ISettingService>(ISettingService.Implement)
            // 注册全局视图模型
            .AddSingleton<GlobalViewModel>()
            // 注册视图模型
            .AddTransient<MainWindowViewModel>()
            .AddTransient<SettingWindowViewModel>()
            .AddTransient<AddDockItemWindowViewModel>()
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
            .AddTransient<AddDockItemWindow>(static provider => new AddDockItemWindow
            {
                DataContext = provider.GetRequiredService<AddDockItemWindowViewModel>()
            });
        return services.BuildServiceProvider();
    }
}
