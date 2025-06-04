using System;
using System.Globalization;
using System.IO;
using Avalonia.Data.Converters;
using DockBar.AvaloniaApp.Extensions;

namespace DockBar.AvaloniaApp.Converters;

public sealed partial class DockItemIconConverter : IValueConverter
{
    public static DockItemIconConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is byte[] bytes)
            return new MemoryStream(bytes).ToIImage();

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}