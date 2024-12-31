using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DockBar.SystemMonitor.Internals;

internal sealed class PerformanceNetworkMonitor : IDisposable
{
    private string[] InstanceNames => new PerformanceCounterCategory("Network Interface").GetInstanceNames();
    private PerformanceCounter[]? NetworkSentCounter { get; set; }
    private PerformanceCounter[]? NetworkReceivedCounter { get; set; }

    public float TotalNetworkSent => NetworkSentCounter is not null ? NetworkSentCounter.Sum(counter => counter.NextValue()) : 0;
    public float TotalNetworkReceived =>
        NetworkReceivedCounter is not null ? NetworkReceivedCounter.Sum(counter => counter.NextValue()) : 0;

    public PerformanceNetworkMonitor()
    {
        var instanceNames = InstanceNames;
        if (instanceNames is not null)
        {
            NetworkSentCounter = new PerformanceCounter[instanceNames.Length];
            NetworkReceivedCounter = new PerformanceCounter[instanceNames.Length];
            for (int i = 0; i < instanceNames.Length; i++)
            {
                NetworkSentCounter[i] = new PerformanceCounter("Network Interface", "Bytes Sent/sec", instanceNames[i]);
                NetworkReceivedCounter[i] = new PerformanceCounter("Network Interface", "Bytes Received/sec", instanceNames[i]);
            }
        }
    }

    public void Dispose()
    {
        if (NetworkSentCounter is not null)
        {
            foreach (var counter in NetworkSentCounter)
                counter.Dispose();
            NetworkSentCounter = null;
        }
        if (NetworkReceivedCounter is not null)
        {
            foreach (var counter in NetworkReceivedCounter)
                counter.Dispose();
            NetworkReceivedCounter = null;
        }
    }
}
