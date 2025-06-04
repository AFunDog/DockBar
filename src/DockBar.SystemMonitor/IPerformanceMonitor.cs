using System.ComponentModel;
using DockBar.SystemMonitor.Internals;

namespace DockBar.SystemMonitor;

public interface IPerformanceMonitor : INotifyPropertyChanged
{
    double CpuUsage { get; }
    double MemoryUsage { get; }
    double TotalPhysicalMemoryMB { get; }
    double NetworkSentBytes { get; }
    double NetworkReceivedBytes { get; }
    string NetworkSentBytesString { get; }
    string NetworkReceivedBytesString { get; }

    public static Type ImplementationType { get; } = typeof(PerformanceMonitor);

    public static IPerformanceMonitor Empty { get; } = new EmptyMonitor();

    sealed class EmptyMonitor : IPerformanceMonitor
    {
        public double CpuUsage => 0;
        public double MemoryUsage => 0;
        public double TotalPhysicalMemoryMB => 0;
        public double NetworkSentBytes => 0;
        public double NetworkReceivedBytes => 0;

        public string NetworkSentBytesString { get; } = "0.00 KB/s";

        public string NetworkReceivedBytesString { get; } = "0.00 KB/s";

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}