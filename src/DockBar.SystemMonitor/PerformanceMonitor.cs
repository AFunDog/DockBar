using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;
using DockBar.Shared.Helpers;
using DockBar.SystemMonitor.Internals;
using Serilog;

namespace DockBar.SystemMonitor;

public sealed partial class PerformanceMonitor : ObservableObject, IDisposable
{
    private ILogger Logger { get; set; } = Log.Logger;
    private Task? MonitorTask { get; set; }

    private PerformanceCounter? CpuUsageCounter { get; set; }
    private PerformanceCounter? MemoryUsageCounter { get; set; }
    private PerformanceNetworkMonitor? NetworkMonitor { get; set; }

    [ObservableProperty]
    public partial float CpuUsage { get; set; }

    [ObservableProperty]
    public partial float MemoryUsage { get; set; }

    [ObservableProperty]
    public partial float TotalPhysicalMemoryMB { get; set; } = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NetworkSentBytesString))]
    public partial float NetworkSentBytes { get; set; }

    public string NetworkSentBytesString =>
        NetworkSentBytes switch
        {
            <= 1024 => $"{NetworkSentBytes:F2} B/S",
            > 1024 and < 1024 * 1024 => $"{(NetworkSentBytes / 1024):F2} KB/S",
            > 1024 * 1024 and < 1024 * 1024 * 1024 => $"{(NetworkSentBytes / 1024 / 1024):F2} MB/S",
            > 1024 * 1024 * 1024 => $"{(NetworkSentBytes / 1024 / 1024 / 1024):F2} GB/S",
            _ => "",
        };

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NetworkReceivedBytesString))]
    public partial float NetworkReceivedBytes { get; set; }

    public string NetworkReceivedBytesString =>
        NetworkReceivedBytes switch
        {
            <= 1024 => $"{NetworkReceivedBytes:F2} B/S",
            > 1024 and < 1024 * 1024 => $"{(NetworkReceivedBytes / 1024):F2} KB/S",
            > 1024 * 1024 and < 1024 * 1024 * 1024 => $"{(NetworkReceivedBytes / 1024 / 1024):F2} MB/S",
            > 1024 * 1024 * 1024 => $"{(NetworkReceivedBytes / 1024 / 1024 / 1024):F2} GB/S",
            _ => "",
        };

    public PerformanceMonitor() { }

    public PerformanceMonitor(ILogger logger)
    {
        Logger = logger;
    }

    public void StartMonitor()
    {
        using var _ = LogHelper.Trace();
        // 先清空上一次的数据
        Dispose();
        CpuUsageCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        MemoryUsageCounter = new PerformanceCounter("Memory", "Available MBytes");
        NetworkMonitor = new PerformanceNetworkMonitor(Logger);

        MonitorTask = Task.Run(Monitor);
        Logger.Information("启动性能监视器");
    }

    private async Task Monitor()
    {
        const int TraceTimeMs = 1000;

        const int LogTimeMs = 10000;
        var logCount = 0;

        while (true)
        {
            try
            {
                //TotalPhysicalMemoryMB = Vanara.PInvoke.Kernel32.GlobalMemoryStatusEx().TotalPhysicalMB;
                TotalPhysicalMemoryMB = GetTotalPhysicalMemoryBytes() / (1024f * 1024);

                CpuUsage = CpuUsageCounter?.NextValue() ?? CpuUsage;
                MemoryUsage = (1 - MemoryUsageCounter?.NextValue() / TotalPhysicalMemoryMB) * 100 ?? MemoryUsage;
                NetworkSentBytes = NetworkMonitor?.TotalNetworkSent ?? NetworkSentBytes;
                NetworkReceivedBytes = NetworkMonitor?.TotalNetworkReceived ?? NetworkReceivedBytes;
            }
            catch (Exception e)
            {
                Logger.Error(e, "监视器出错");
                break;
            }
            if (logCount == LogTimeMs / TraceTimeMs)
            {
                Logger.Verbose(
                    "记录一次性能数据 CPU:{CpuUsage} MEM:{MemoryUsage} UPLOAD:{NetworkSentBytes} DOWNLOAD:{NetworkReceivedBytes}",
                    CpuUsage,
                    MemoryUsage,
                    NetworkSentBytes,
                    NetworkReceivedBytes
                );
                logCount = 0;
            }
            logCount++;
            await Task.Delay(TraceTimeMs);
        }
    }

    private static ulong GetTotalPhysicalMemoryBytes()
    {
        Vanara.PInvoke.Kernel32.MEMORYSTATUSEX memStatus = new();
        memStatus.dwLength = (uint)Marshal.SizeOf(memStatus);
        if (Vanara.PInvoke.Kernel32.GlobalMemoryStatusEx(ref memStatus))
        {
            return memStatus.ullTotalPhys;
        }
        return 1;
    }

    public void Dispose()
    {
        using var _ = LogHelper.Trace();
        MonitorTask?.Wait(0);
        MonitorTask?.Dispose();
        MonitorTask = null;
        CpuUsageCounter?.Dispose();
        CpuUsageCounter = null;
        MemoryUsageCounter?.Dispose();
        MemoryUsageCounter = null;
        NetworkMonitor?.Dispose();
        NetworkMonitor = null;
        Logger.Debug("性能监视器关闭并销毁");
    }
}
