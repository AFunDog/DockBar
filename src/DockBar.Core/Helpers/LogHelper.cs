using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Serilog.Context;

namespace DockBar.Core.Helpers;

public static class LogHelper
{
    public static IDisposable Trace(
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0,
        [CallerMemberName] string member = ""
    )
    {
        return LogContext.PushProperty("Caller", $"{Path.GetFileName(file)} {line} {member} ");
    }
}
