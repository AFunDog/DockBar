using System;
using System.Collections.Generic;
using System.Reflection;
using Avalonia;
using DockBar.AvaloniaApp.Contacts;
using DockBar.AvaloniaApp.Extensions;
using DockBar.AvaloniaApp.Services;
using DockBar.AvaloniaApp.ViewModels;
using DockBar.AvaloniaApp.Views;
using DockBar.Core.Extensions;
using DockBar.Core.Structs;
using DockBar.DockItem.Extensions;
using DockBar.SystemMonitor.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using Avalonia.Controls;
using DockBar.AvaloniaApp.ViewModels.ControlPanel;
using DockBar.AvaloniaApp.Windows;
using Serilog.Formatting.Compact;
using Zeng.CoreLibrary.Toolkit.Avalonia.Extensions;
using Zeng.CoreLibrary.Toolkit.Contacts;
using Zeng.CoreLibrary.Toolkit.Extensions;
using Zeng.CoreLibrary.Toolkit.Logging;
using Zeng.CoreLibrary.Toolkit.Services.Localization;
using Zeng.CoreLibrary.Toolkit.Services.Navigate;
using Zeng.CoreLibrary.Toolkit.Structs;
using ControlPanelWindowViewModel = DockBar.AvaloniaApp.ViewModels.ControlPanel.ControlPanelWindowViewModel;

namespace DockBar.AvaloniaApp;

internal sealed class Program
{
    public static IServiceProvider ServiceProvider { get; } = BuildServices();

    public static Version AppVersion { get; } = Assembly.GetExecutingAssembly().GetName().Version is { } v
        ? new(v.Major, v.Minor, v.Build)
        : new();

    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            ServiceProvider
                .GetRequiredService<ILogger>()
                .Information("程序启动 {Version} {WorkDir}", AppVersion, Environment.CurrentDirectory);
            BeforeSetup();
            var exitCode = BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

            ServiceProvider.GetRequiredService<ILogger>().Information("程序正常退出 {exitCode}", exitCode);
            ServiceProvider.TryDispose();
        }
        catch (Exception e)
        {
            ServiceProvider.GetRequiredService<ILogger>().Fatal(e, "未处理的崩溃");
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp() => AppBuilder
        .Configure(ServiceProvider.GetRequiredService<App>)
        .AfterSetup(AfterSetup)
        .UsePlatformDetect()
        .WithInterFont()
        .LogToTrace();

    private static ServiceProvider BuildServices()
    {
        return new ServiceCollection()
            //注册 App
            .AddSingleton<App>()
            // 注册外部服务
            .AddSingleton<IDataProvider<AppSetting>, AppSettingProvider>()
            .AddSingleton<IDataProvider<IEnumerable<LocalizationData>>, LocalizationDataProvider>()
            .UseAppSettingWrapper()
            .UseLocalizeService()
            .UseNavigateService()
            .AddSingleton<ILogger>(_ => BuildLogger())
            .UseDockItem()
            .UseSystemMonitor()
            .AddSingleton<IGlobalHotKeyManager, GlobalHotKeyManager>()
            .AddSingleton<WindowManager>()
            //.AddSingleton(typeof(IPerformanceMonitor), IPerformanceMonitor.ImplementationType)
            // 注册视图模型
            .AddTransient<MainWindowViewModel>()
            .AddTransient<MenuWindowViewModel>()
            .AddTransient<PerformanceMonitorViewModel>()
            .AddTransient<ControlPanelWindowViewModel>()
            .AddTransient<EditDockItemWindowViewModel>()
            // .AddTransient<SettingViewModel>()
            .AddTransient<ControlPanelMainViewModel>()
            // 注册主窗口
            .AddKeyedSingleton<Window>(
                nameof(MainWindow),
                (s, _) => new MainWindow() { DataContext = s.GetRequiredService<MainWindowViewModel>() }
            )
            // 注册模态弹窗（可能以后做优化）
            .AddSingleton<DockItemFolderWindow>()
            .AddKeyedTransient<Window>(
                nameof(MenuWindow),
                (s, _) => new MenuWindow() { DataContext = s.GetRequiredService<MenuWindowViewModel>() }
            )
            .AddKeyedTransient<Window>(
                nameof(EditDockItemWindow),
                (s, _) => new EditDockItemWindow() { DataContext = s.GetRequiredService<EditDockItemWindowViewModel>() }
            )
            .AddKeyedTransient<Window>(
                nameof(ControlPanelWindow),
                (s, _) => new ControlPanelWindow() { DataContext = s.GetRequiredService<ControlPanelWindowViewModel>() }
            )
            .UseControlPanelViews()
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
    }

    private static ILogger BuildLogger()
    {
        //AnsiConsoleTheme.Code.
        return Log.Logger = new LoggerConfiguration()
            // .Enrich.FromLogContext()
            .Enrich.FromTraceInfo()
            .Enrich.ShortSourceContext()
            .MinimumLevel.Verbose()
#if DEBUG
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss fff} {Level:u3}] {TraceInfo}{Message:lj}{NewLine}{Exception}"
            )
#endif
            // .WriteTo.File(
            //     "Log/log.txt",
            //     outputTemplate:
            //     "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Caller}{Message:lj}{NewLine}{Exception}",
            //     restrictedToMinimumLevel: LogEventLevel.Verbose,
            //     rollingInterval: RollingInterval.Day,
            //     rollOnFileSizeLimit: true
            // )
            .WriteTo.File(
                new CompactJsonFormatter(),
                "logs/log-.json",
                restrictedToMinimumLevel: LogEventLevel.Verbose,
                rollingInterval: RollingInterval.Day,
                rollOnFileSizeLimit: true
            )
            // .WriteTo.File(
            //     path: "Log/log.json",
            //     formatter: new JsonFormatter()
            //     )
            .CreateLogger();
    }

    private static void BeforeSetup()
    {
        // AppSetting LoadData 只加载一次
        ServiceProvider.GetRequiredService<IDataProvider<AppSetting>>().LoadData();

        LocalizeExtension.SetLocalizeService(ServiceProvider.GetRequiredService<ILocalizeService>());
        // 初始化全局热键服务
        _ = ServiceProvider.GetRequiredService<IGlobalHotKeyManager>();
        _ = ServiceProvider.GetRequiredService<WindowManager>();
    }

    private static void AfterSetup(AppBuilder builder)
    {
        var navigateService = ServiceProvider.GetRequiredService<INavigateService>();
        navigateService
            .RegisterViewRoute(
                "/",
                () => new ControlPanelMainView()
                {
                    DataContext = ServiceProvider.GetRequiredService<ControlPanelMainViewModel>()
                }
            )
            .RegisterViewRoute("/DockItems", () => ServiceProvider.GetRequiredService<ControlPanelDockItemsView>())
            .RegisterViewRoute("/Settings", () => ServiceProvider.GetRequiredService<ControlPanelSettingView>());
        // 通过预加载 MenuWindow 可以解决第一个打开的 MenuWindow 展示位置不对的问题
        // var menuWindow = ServiceProvider.GetRequiredService<MenuWindow>();
        // if (menuWindow.Screens.ScreenFromWindow(menuWindow) is { } screen)
        // {
        // menuWindow.Position =
        //     new(screen.Bounds.Size.Width + 12, screen.Bounds.Size.Height + 12);
        // menuWindow.Show();
        // menuWindow.Hide();
        // menuWindow.RootShowMenu.Loaded += (_, _) =>
        // {
        //     menuWindow.Hide();
        // };

        // menuWindow.RootShowMenu.Loaded += (_, _) =>
        // {
        //     menuWindow.Hide();
        // };
        // var timer = new DispatcherTimer() { Interval = TimeSpan.FromSeconds(1) };
        // timer.Tick += (s, e) => { timer.Stop();menuWindow.Hide(); };
        // timer.Start();
        // }

        var folderWindow = ServiceProvider.GetRequiredService<DockItemFolderWindow>();
        folderWindow.Show();
        folderWindow.Classes.Add("ToHide");
        folderWindow.Hide();
    }
}