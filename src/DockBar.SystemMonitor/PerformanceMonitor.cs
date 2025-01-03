﻿using System.Diagnostics;
using System.Management;
using CommunityToolkit.Mvvm.ComponentModel;
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
    public partial float TotalPhysicalMemoryMB { get; set; }

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
        // 先清空上一次的数据
        Dispose();

        CpuUsageCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        MemoryUsageCounter = new PerformanceCounter("Memory", "Available MBytes");
        NetworkMonitor = new PerformanceNetworkMonitor();
        TotalPhysicalMemoryMB = GetTotalMemory() / (1024 * 1024);
        MonitorTask = Task.Run(Monitor);
        Logger.Information("启动性能监视器");
    }

    private async Task Monitor()
    {
        while (true)
        {
            CpuUsage = CpuUsageCounter?.NextValue() ?? CpuUsage;
            MemoryUsage = (1 - MemoryUsageCounter?.NextValue() / TotalPhysicalMemoryMB) * 100 ?? MemoryUsage;
            NetworkSentBytes = NetworkMonitor?.TotalNetworkSent ?? NetworkSentBytes;
            NetworkReceivedBytes = NetworkMonitor?.TotalNetworkReceived ?? NetworkReceivedBytes;
            Logger.Verbose(
                "获取一次性能数据 CPU:{CpuUsage} MEM:{MemoryUsage} UPLOAD:{NetworkSentBytes} DOWNLOAD:{NetworkReceivedBytes}",
                CpuUsage,
                MemoryUsage,
                NetworkSentBytes,
                NetworkReceivedBytes
            );
            await Task.Delay(1000);
        }
    }

    private static ulong GetTotalMemory()
    {
        // 不能放在异步中使用会卡死，不知道为什么！！！
        using var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
        foreach (var obj in searcher.Get())
        {
            return (ulong)obj["TotalPhysicalMemory"];
        }
        return 0;
    }

    public void Dispose()
    {
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
