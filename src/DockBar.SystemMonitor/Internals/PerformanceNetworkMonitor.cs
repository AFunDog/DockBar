using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DockBar.Core.Helpers;
using Serilog;

namespace DockBar.SystemMonitor.Internals;

internal sealed class PerformanceNetworkMonitor : IDisposable
{
    private const string CategoryName = "Network Interface";
    private const string ReceivedCounterName = "Bytes Received/sec";
    private const string SentCounterName = "Bytes Sent/sec";

    private static string[] InstanceNames => new PerformanceCounterCategory(CategoryName).GetInstanceNames();
    private ILogger Logger { get; set; } = Log.Logger;

    private List<PerformanceCounter> NetworkSentCounter { get; set; } = [];
    private List<PerformanceCounter> NetworkReceivedCounter { get; set; } = [];

    public float TotalNetworkSent
    {
        get
        {
            using var _ = LogHelper.Trace();
            //if (
            //    NetworkSentCounter is null
            //    || NetworkSentCounter.Select(counter => counter.InstanceName).SequenceEqual(InstanceNames) is false
            //)
            //{
            //    ResetCounter();
            //    return 0;
            //}
            //ResetCounter();


            float value = 0;
            try
            {
                foreach (var counter in NetworkSentCounter)
                    value += counter.NextValue();
            }
            catch (Exception e)
            {
                Logger.Verbose(e, "网络监控器异常 {Instance}", InstanceNames);
                ResetCounter();
            }

            return value;
        }
    }

    public float TotalNetworkReceived
    {
        get
        {
            using var _ = LogHelper.Trace();
            //if (
            //    NetworkReceivedCounter is null
            //    || NetworkReceivedCounter.Select(counter => counter.InstanceName).SequenceEqual(InstanceNames) is false
            //)
            //{
            //    ResetCounter();
            //    return 0;
            //}
            //ResetCounter();
            float value = 0;
            try
            {
                foreach (var counter in NetworkReceivedCounter)
                    value += counter.NextValue();
            }
            catch (Exception e)
            {
                Logger.Verbose(e, "网络监控器异常 {Instance}", InstanceNames);
                ResetCounter();
            }

            return value;
        }
    }

    public PerformanceNetworkMonitor() : this(Log.Logger)
    {
    }

    public PerformanceNetworkMonitor(ILogger logger)
    {
        Logger = logger;
        ResetCounter();
    }

    public void ResetCounter()
    {
        using var _ = LogHelper.Trace();
        Logger.Verbose("尝试重置网络性能计数器");
        //foreach (var counter in NetworkSentCounter ?? [])
        //    counter.Dispose();
        //foreach (var counter in NetworkReceivedCounter ?? [])
        //    counter.Dispose();

        var instanceNames = InstanceNames;
        var newInstances = instanceNames.Except(NetworkSentCounter.Select(counter => counter.InstanceName)).ToArray();
        var removeInstances
            = NetworkSentCounter.Select(counter => counter.InstanceName).Except(instanceNames).ToArray();
        try
        {
            if (removeInstances.Length != 0)
            {
                NetworkSentCounter.RemoveAll(counter =>
                    {
                        if (removeInstances.Contains(counter.InstanceName))
                        {
                            counter.Dispose();
                            return true;
                        }

                        return false;
                    }
                );
                NetworkReceivedCounter.RemoveAll(counter =>
                    {
                        if (removeInstances.Contains(counter.InstanceName))
                        {
                            counter.Dispose();
                            return true;
                        }

                        return false;
                    }
                );
            }

            if (newInstances.Length != 0)
            {
                NetworkSentCounter.AddRange(
                    newInstances.Select(ins => new PerformanceCounter(CategoryName, SentCounterName, ins))
                );
                NetworkReceivedCounter.AddRange(
                    newInstances.Select(ins => new PerformanceCounter(CategoryName, ReceivedCounterName, ins))
                );
            }

            //NetworkSentCounter = instanceNames.Select(ins => new PerformanceCounter("Network Interface", "Bytes Sent/sec", ins)).ToList();
            //NetworkReceivedCounter = instanceNames
            //    .Select(ins => new PerformanceCounter("Network Interface", "Bytes Received/sec", ins))
            //    .ToList();
        }
        catch (Exception e)
        {
            Logger.Error(e, "初始化或重置网络性能计数器失败");
        }
    }

    public void Dispose()
    {
        if (NetworkSentCounter is not null)
        {
            foreach (var counter in NetworkSentCounter)
                counter.Dispose();
            NetworkSentCounter.Clear();
        }

        if (NetworkReceivedCounter is not null)
        {
            foreach (var counter in NetworkReceivedCounter)
                counter.Dispose();
            NetworkReceivedCounter.Clear();
        }
    }
}