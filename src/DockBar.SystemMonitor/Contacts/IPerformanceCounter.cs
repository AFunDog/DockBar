using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DockBar.SystemMonitor.Contacts;

public interface IPerformanceCounter : IDisposable
{
    double NextValue();
}