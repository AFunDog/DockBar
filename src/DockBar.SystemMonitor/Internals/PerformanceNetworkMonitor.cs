﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DockBar.Shared.Helpers;
using Serilog;

namespace DockBar.SystemMonitor.Internals;

internal sealed class PerformanceNetworkMonitor : IDisposable
{
    private static string[] InstanceNames => new PerformanceCounterCategory("Network Interface").GetInstanceNames();
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
            ResetCounter();
            try
            {
                return NetworkSentCounter.Sum(counter => counter.NextValue());
            }
            catch (Exception e)
            {
                Logger.Error(e, "网络监控器异常 {Instance}", InstanceNames);
            }
            return 0;
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
            ResetCounter();
            try
            {
                return NetworkReceivedCounter.Sum(counter => counter.NextValue());
            }
            catch (Exception e)
            {
                Logger.Error(e, "网络监控器异常 {Instance}", InstanceNames);
            }
            return 0;
        }
    }

    public PerformanceNetworkMonitor()
    {
        ResetCounter();
    }

    public PerformanceNetworkMonitor(ILogger logger)
        : this()
    {
        Logger = logger;
    }

    public void ResetCounter()
    {
        //foreach (var counter in NetworkSentCounter ?? [])
        //    counter.Dispose();
        //foreach (var counter in NetworkReceivedCounter ?? [])
        //    counter.Dispose();

        var instanceNames = InstanceNames;
        var newInstances = instanceNames.Except(NetworkSentCounter.Select(counter => counter.InstanceName)).ToArray();
        var removeInstances = NetworkSentCounter.Select(counter => counter.InstanceName).Except(instanceNames).ToArray();
        try
        {
            if (removeInstances.Length != 0)
            {
                NetworkSentCounter.RemoveAll(counter => removeInstances.Contains(counter.InstanceName));
                NetworkReceivedCounter.RemoveAll(counter => removeInstances.Contains(counter.InstanceName));
            }
            if (newInstances.Length != 0)
            {
                NetworkSentCounter.AddRange(newInstances.Select(ins => new PerformanceCounter("Network Interface", "Bytes Sent/sec", ins)));
                NetworkReceivedCounter.AddRange(
                    newInstances.Select(ins => new PerformanceCounter("Network Interface", "Bytes Received/sec", ins))
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
