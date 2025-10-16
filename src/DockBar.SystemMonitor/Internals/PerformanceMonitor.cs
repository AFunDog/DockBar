using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DockBar.SystemMonitor.Contacts;
using DockBar.SystemMonitor.Internals;
using Serilog;
using Windows.Win32.System.SystemInformation;
using Zeng.CoreLibrary.Toolkit.Logging;
using static Windows.Win32.PInvoke;

namespace DockBar.SystemMonitor.Internals;

internal sealed partial class PerformanceMonitor : IPerformanceMonitor, IDisposable
{
    private ILogger Logger { get; set; }
    private Task? MonitorTask { get; set; }

    private CancellationTokenSource MonitorCancellationTokenSource { get; set; } = new();
    private IPerformanceCounter CpuUsageCounter { get; set; }
    private IPerformanceCounter MemoryUsageCounter { get; set; }

    private IPerformanceCounter NetworkSentBytesCounter { get; set; }

    private IPerformanceCounter NetworkReceivedBytesCounter { get; set; }

    //private PerformanceNetworkMonitor? NetworkMonitor { get; set; }

    public event EventHandler<PerformanceChangedEventArgs>? PerformanceDataChanged;

    public double CpuUsage
    {
        get => field;
        private set
        {
            if (Math.Abs(field - value) < 1e-5)
                return;
            field = value;
            PerformanceDataChanged?.Invoke(this, new PerformanceChangedEventArgs() { PropertyName = nameof(CpuUsage) });
        }
    }

    public double MemoryUsage
    {
        get => field;
        private set
        {
            if (Math.Abs(field - value) < 1e-5)
                return;
            field = value;
            PerformanceDataChanged?.Invoke(
                this,
                new PerformanceChangedEventArgs() { PropertyName = nameof(MemoryUsage) }
            );
        }
    }

    public double TotalPhysicalMemoryMB
    {
        get => field;
        private set
        {
            if (Math.Abs(field - value) < 1e-5)
                return;
            field = value;
            PerformanceDataChanged?.Invoke(
                this,
                new PerformanceChangedEventArgs() { PropertyName = nameof(TotalPhysicalMemoryMB) }
            );
        }
    } = 1;

    public double NetworkSentBytes
    {
        get => field;
        private set
        {
            if (Math.Abs(field - value) < 1e-5)
                return;
            field = value;
            PerformanceDataChanged?.Invoke(
                this,
                new PerformanceChangedEventArgs() { PropertyName = nameof(NetworkSentBytes) }
            );
            PerformanceDataChanged?.Invoke(
                this,
                new PerformanceChangedEventArgs() { PropertyName = nameof(NetworkSentBytesString) }
            );
        }
    }

    public string NetworkSentBytesString => NetworkBytesToString(NetworkSentBytes);

    public double NetworkReceivedBytes
    {
        get => field;
        private set
        {
            if (Math.Abs(field - value) < 1e-5)
                return;
            field = value;
            PerformanceDataChanged?.Invoke(
                this,
                new PerformanceChangedEventArgs() { PropertyName = nameof(NetworkReceivedBytes) }
            );
            PerformanceDataChanged?.Invoke(
                this,
                new PerformanceChangedEventArgs() { PropertyName = nameof(NetworkReceivedBytesString) }
            );
        }
    }

    public string NetworkReceivedBytesString => NetworkBytesToString(NetworkReceivedBytes);

    private static string NetworkBytesToString(double bytes)
    {
        return bytes switch
        {
            <= 768 => $"{bytes:F2} B/S",
            > 768 and < 768 * 1024 => $"{bytes / 1024:F2} KB/S",
            > 768 * 1024 and < 768 * 1024 * 1024 => $"{bytes / 1024 / 1024:F2} MB/S",
            > 768 * 1024 * 1024 => $"{bytes / 1024 / 1024 / 1024:F2} GB/S",
            _ => $"{bytes:F2}"
        };
    }

    public PerformanceMonitor() : this(Log.Logger) { }

    public PerformanceMonitor(ILogger logger)
    {
        Logger = logger.ForContext<PerformanceMonitor>();

        // 这样统计CPU好像更准确
        CpuUsageCounter = new PdhPerformanceCounter(@"\Processor(*)\% Processor Time", CountWay.Average);
        //CpuUsageCounter = new PdhPerformanceCounter(@"\Processor(_Total)\% Processor Time");
        MemoryUsageCounter = new PdhPerformanceCounter(@"\Memory\Available MBytes");

        NetworkReceivedBytesCounter = new PdhPerformanceCounter(
            @"\Network Interface(*)\Bytes Received/sec",
            CountWay.Sum
        );
        NetworkSentBytesCounter = new PdhPerformanceCounter(@"\Network Interface(*)\Bytes Sent/sec", CountWay.Sum);

        MonitorTask = MonitorAsync(MonitorCancellationTokenSource.Token);

        Logger.Information("启动性能监视器");
    }

    private async Task MonitorAsync(CancellationToken cancellationToken = default)
    {
        const int TraceTimeMs = 1000;

        const int LogTimeMs = 10000;
        var logCount = 0;

        while (cancellationToken.IsCancellationRequested == false)
        {
            try
            {
                //TotalPhysicalMemoryMB = Vanara.PInvoke.Kernel32.GlobalMemoryStatusEx().TotalPhysicalMB;
                TotalPhysicalMemoryMB = GetTotalPhysicalMemoryBytes() / (1024.0 * 1024);
                var temp = CpuUsageCounter.NextValue();
                CpuUsage = temp == 0 ? CpuUsage : temp;
                MemoryUsage = (1 - MemoryUsageCounter.NextValue() / TotalPhysicalMemoryMB) * 100;
                //AdapterProvider.UpdateAdapters();
                NetworkSentBytes = NetworkSentBytesCounter.NextValue();
                NetworkReceivedBytes = NetworkReceivedBytesCounter.NextValue();
            }
            catch (Exception e)
            {
                Logger.Trace().Error(e, "监视器出错");
                break;
            }

            if (logCount == LogTimeMs / TraceTimeMs)
            {
                Logger.Trace().Verbose(
                    "记录一次性能数据 CPU:{CpuUsage,-5:F2} MEM:{MemoryUsage,-5:F2} UPLOAD:{NetworkSentBytes,-12} DOWNLOAD:{NetworkReceivedBytes,-12}",
                    CpuUsage,
                    MemoryUsage,
                    NetworkSentBytesString,
                    NetworkReceivedBytesString
                );
                logCount = 0;
            }

            logCount++;
            try
            {
                await Task.Delay(TraceTimeMs, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private static ulong GetTotalPhysicalMemoryBytes()
    {
        MEMORYSTATUSEX memStatus = new();
        memStatus.dwLength = (uint)Marshal.SizeOf(memStatus);
        if (GlobalMemoryStatusEx(ref memStatus))
            return memStatus.ullTotalPhys;
        return 1;
    }

    public void Dispose()
    {
        MonitorCancellationTokenSource.Cancel();
        MonitorCancellationTokenSource.Dispose();
        // if (MonitorTask is not null)
        //     await MonitorTask;
        // MonitorTask?.Dispose();
        // MonitorTask = null;
        CpuUsageCounter?.Dispose();
        //CpuUsageCounter = null;
        MemoryUsageCounter?.Dispose();
        //MemoryUsageCounter = null;
        //NetworkMonitor?.Dispose();
        //NetworkMonitor = null;
        Logger.Trace().Debug("性能监视器关闭并销毁");
    }
}