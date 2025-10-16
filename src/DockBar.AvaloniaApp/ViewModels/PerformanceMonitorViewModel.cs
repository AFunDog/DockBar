using CommunityToolkit.Mvvm.ComponentModel;
using DockBar.SystemMonitor;
using Serilog;

namespace DockBar.AvaloniaApp.ViewModels;

internal sealed partial class PerformanceMonitorViewModel : ViewModelBase
{
    private ILogger Logger { get; }
    private IPerformanceMonitor PerformanceMonitor { get; }

    [ObservableProperty]
    public partial double CpuUsage { get; set; }
    
    [ObservableProperty]
    public partial double MemoryUsage { get; set; }
    [ObservableProperty]
    public partial double TotalPhysicalMemoryMB { get; set; }
    [ObservableProperty]
    public partial string NetworkSentBytesString { get; set; }
    [ObservableProperty]
    public partial string NetworkReceivedBytesString { get; set; }

    public PerformanceMonitorViewModel() { }

    public PerformanceMonitorViewModel(ILogger logger, IPerformanceMonitor performanceMonitor)
    {
        Logger = logger;
        PerformanceMonitor = performanceMonitor;

        PerformanceMonitor.PerformanceDataChanged += (s, e) =>
        {
            CpuUsage = PerformanceMonitor.CpuUsage;
            MemoryUsage = PerformanceMonitor.MemoryUsage;
            TotalPhysicalMemoryMB = PerformanceMonitor.TotalPhysicalMemoryMB;
            NetworkSentBytesString = PerformanceMonitor.NetworkSentBytesString;
            NetworkReceivedBytesString = PerformanceMonitor.NetworkReceivedBytesString;
        };
    }
}