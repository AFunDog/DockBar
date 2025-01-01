using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Platform;

namespace DockBar.Avalonia;

internal static class IconResource
{
    public static MemoryStream DefaultIconStream { get; } = AssetLoaderToMemoryStream("avares://DockBar.Avalonia/Assets/icon.png");
    public static MemoryStream SettingIconStream { get; } = AssetLoaderToMemoryStream("avares://DockBar.Avalonia/Assets/setting.png");

    private static MemoryStream AssetLoaderToMemoryStream(string uri)
    {
        using var stream = AssetLoader.Open(new Uri(uri));
        var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        memoryStream.Position = 0;
        return memoryStream;
    }
}
