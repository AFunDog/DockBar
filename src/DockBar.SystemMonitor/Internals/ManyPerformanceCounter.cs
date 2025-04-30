using CoreLibrary.Core.Contacts;
using DockBar.SystemMonitor.Contacts;

namespace DockBar.SystemMonitor.Internals;

internal sealed class ManyPerformanceCounter<T> : DisposableObject, IPerformanceCounter
    where T : IPerformanceCounter
{
    private HashSet<T> CounterSet { get; } = [];

    public IEnumerable<T> Counters => CounterSet;

    public ManyPerformanceCounter(IEnumerable<T> counters)
    {
        //CounterSet.AddRange(counters);
        foreach (var counter in counters)
        {
            CounterSet.Add(counter);
        }
    }

    public double NextValue()
    {
        return CounterSet.Sum(x => x.NextValue());
    }

    public void AddCounter(T counter)
    {
        CounterSet.Add(counter);
    }

    public void RemoveCounter(T counter)
    {
        if (CounterSet.Remove(counter))
        {
            counter.Dispose();
        }
    }

    protected override void DisposeManagedResource() { }

    protected override void DisposeUnmanagedResource()
    {
        foreach (var counter in CounterSet)
        {
            counter.Dispose();
        }
        CounterSet.Clear();
    }

    protected override void OnDisposed() { }
}
