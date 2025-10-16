using System.IO;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace DockBar.AvaloniaApp.Extensions;

internal static class DockItemBaseExtension
{
    public static IImage? ToIImage(this Stream? stream)
    {
        if (stream is null || stream.Length == 0)
            return null;
        // 这里必须将流的位置重置为0，否则无法读取流中的数据
        // https://github.com/AvaloniaUI/Avalonia/discussions/12548
        stream.Position = 0;
        return new Bitmap(stream);
    }
}