using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Platform;

namespace DockBar.AvaloniaApp;

internal static class IconResource
{
    public static Uri AppIconUri { get; } = new Uri("avares://DockBar.AvaloniaApp/Assets/icon.ico");
    public static Uri SettingIconUri { get; } = new Uri("avares://DockBar.AvaloniaApp/Assets/setting.png");

    public static MemoryStream ToMemoryStream(this Stream stream)
    {
        var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        memoryStream.Position = 0;
        return memoryStream;
    }

    //public static MemoryStream AppIconStream { get; } = AssetLoaderToMemoryStream("avares://DockBar.AvaloniaApp/Assets/icon.ico");
    //public static MemoryStream SettingIconStream { get; } = AssetLoaderToMemoryStream("avares://DockBar.AvaloniaApp/Assets/setting.png");

    //private static MemoryStream AssetLoaderToMemoryStream(string uri)
    //{
    //    using var stream = AssetLoader.Open(new Uri(uri));
    //    var memoryStream = new MemoryStream();
    //    stream.CopyTo(memoryStream);
    //    memoryStream.Position = 0;
    //    return memoryStream;
    //}
}
