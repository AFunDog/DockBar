using System;
using System.ComponentModel.Design.Serialization;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Threading;
using DockBar.AvaloniaApp.Contacts;
using DockBar.AvaloniaApp.Extensions;
using DockBar.AvaloniaApp.Services;
using DockBar.AvaloniaApp.ViewModels;
using DockBar.AvaloniaApp.Views;
using DockBar.Core.Contacts;
using DockBar.Core.Extensions;
using DockBar.Core.Structs;
using DockBar.DockItem.Extensions;
using DockBar.SystemMonitor;
using DockBar.SystemMonitor.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Zeng.CoreLibrary.Toolkit.Avalonia.Extensions;
using Zeng.CoreLibrary.Toolkit.Avalonia.Structs;
using Zeng.CoreLibrary.Toolkit.Contacts;
using Zeng.CoreLibrary.Toolkit.Extensions;
using Zeng.CoreLibrary.Toolkit.Services.Localization;
using Zeng.CoreLibrary.Toolkit.Services.Navigate;
using Zeng.CoreLibrary.Toolkit.Structs;

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
            ServiceProvider.GetRequiredService<ILogger>().Information("程序启动 {Version}", AppVersion);
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
            .AddSingleton<IDataProvider<LocalizationData>, LocalizationDataProvider>()
            .UseAppSettingWrapper()
            .UseLocalizeService()
            .UseNavigateService()
            .AddSingleton<ILogger>(_ => BuildLogger())
            .UseDockItem()
            .UseSystemMonitor()
            .AddSingleton<IGlobalHotKeyManager, GlobalHotKeyManager>()
            //.AddSingleton(typeof(IPerformanceMonitor), IPerformanceMonitor.ImplementationType)
            // 注册视图模型
            .AddTransient<MainWindowViewModel>()
            .AddTransient<ControlPanelWindowViewModel>()
            .AddTransient<EditDockItemWindowViewModel>()
            // .AddTransient<SettingViewModel>()
            .AddTransient<MainViewModel>()
            // 注册窗口
            .AddSingleton<MainWindow>()
            // 注册模态弹窗（可能以后做优化）
            .AddSingleton<DockItemFolderWindow>()
            .AddSingleton<MenuWindow>()
            .AddTransient<EditDockItemWindow>()
            .AddTransient<ControlPanelWindow>()
            .UseControlPanelViews()
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
    }

    private static Logger BuildLogger()
    {
        //AnsiConsoleTheme.Code.
        return new LoggerConfiguration()
            .Enrich.FromLogContext()
            .MinimumLevel.Verbose()
#if DEBUG
            .WriteTo.Console(
                outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Caller}{Message:lj}{NewLine}{Exception}",
                restrictedToMinimumLevel: LogEventLevel.Verbose
            )
#endif
            .WriteTo.File(
                "Log/log.txt",
                outputTemplate:
                "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Caller}{Message:lj}{NewLine}{Exception}",
                restrictedToMinimumLevel: LogEventLevel.Verbose,
                rollingInterval: RollingInterval.Day,
                rollOnFileSizeLimit: true
            )
            .CreateLogger();
    }

    private static void BeforeSetup()
    {
        // AppSetting LoadData 只加载一次
        ServiceProvider.GetRequiredService<IDataProvider<AppSetting>>().LoadData();

        LocalizeExtension.SetLocalizeService(ServiceProvider.GetRequiredService<ILocalizeService>());
        // 初始化全局热键服务
        _ = ServiceProvider.GetRequiredService<IGlobalHotKeyManager>();
    }

    private static void AfterSetup(AppBuilder builder)
    {
        // 通过预加载 MenuWindow 可以解决第一个打开的 MenuWindow 展示位置不对的问题
        var menuWindow = ServiceProvider.GetRequiredService<MenuWindow>();
        if (menuWindow.Screens.ScreenFromWindow(menuWindow) is { } screen)
        {
            // menuWindow.Position =
            //     new(screen.Bounds.Size.Width + 12, screen.Bounds.Size.Height + 12);
            menuWindow.Show();
            menuWindow.Hide();
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
        }

        var folderWindow = ServiceProvider.GetRequiredService<DockItemFolderWindow>();
        folderWindow.Show();
        folderWindow.Classes.Add("ToHide");
        folderWindow.Hide();
    }
}