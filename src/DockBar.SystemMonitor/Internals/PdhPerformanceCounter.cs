using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using CoreLibrary.Core;
using CoreLibrary.Core.Contacts;
using DockBar.SystemMonitor.Contacts;
using Windows.Win32.System.Performance;
using static Windows.Win32.PInvoke;

namespace DockBar.SystemMonitor.Internals;

internal enum CountWay
{
    Single,
    Sum,
    Average,
}

internal sealed class PdhPerformanceCounter : DisposableObject, IPerformanceCounter
{
    //private string CounterPath { get; }
    //private readonly bool _isMany;
    private readonly nint _HQUERY;
    private readonly nint _HCOUNTER;
    private readonly CountWay _countWay;

    public PdhPerformanceCounter(string counterPath, CountWay countWay = CountWay.Single)
    {
        PdhOpenQuery(null, 0, out _HQUERY);
        PdhAddCounter(_HQUERY, counterPath, 0, out _HCOUNTER);
        _countWay = countWay;
    }

    public double NextValue()
    {
        PdhCollectQueryData(_HQUERY);
        unsafe
        {
            if (_countWay is not CountWay.Single)
            {
                // 获取多个Counter数据
#pragma warning disable CS8500 // 这会获取托管类型的地址、获取其大小或声明指向它的指针
                uint dwBufferSize = 0;
                uint itemCount = 0;
                //PDH_FMT_COUNTERVALUE_ITEM_W items;
                PdhGetFormattedCounterArray(_HCOUNTER, PDH_FMT.PDH_FMT_DOUBLE, &dwBufferSize, &itemCount);
                var values = (PDH_FMT_COUNTERVALUE_ITEM_W*)Marshal.AllocHGlobal((int)dwBufferSize);
                //nint values = new nint(dwBufferSize);
                PdhGetFormattedCounterArray(_HCOUNTER, PDH_FMT.PDH_FMT_DOUBLE, ref dwBufferSize, out itemCount, values);
                //var items = (PDH_FMT_COUNTERVALUE_ITEM*)values.ToPointer();
                double value = 0;
                switch (_countWay)
                {
                    case CountWay.Sum:
                        for (int i = 0; i < itemCount; i++)
                        {
                            value += values[i].FmtValue.Anonymous.doubleValue;
                        }
                        break;
                    case CountWay.Average:
                        for (int i = 0; i < itemCount; i++)
                        {
                            value += values[i].FmtValue.Anonymous.doubleValue;
                        }
                        if (itemCount is not 0)
                            value /= itemCount;
                        break;
                    default:
                        break;
                }
                Marshal.FreeHGlobal((nint)values);
                return value;
#pragma warning restore CS8500 // 这会获取托管类型的地址、获取其大小或声明指向它的指针
            }
            else
            {
                PdhGetFormattedCounterValue(_HCOUNTER, PDH_FMT.PDH_FMT_DOUBLE, null, out var value);
                return value.Anonymous.doubleValue;
            }
        }
    }

    protected override void DisposeManagedResource() { }

    protected override void DisposeUnmanagedResource()
    {
        PdhRemoveCounter(_HCOUNTER);
        PdhCloseQuery(_HQUERY);
    }

    protected override void OnDisposed() { }
}
