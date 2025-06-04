using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using DockBar.Core.Structs;

namespace DockBar.AvaloniaApp.Converters;

public sealed class ColorValueColorConverter : IValueConverter
{
    public static ColorValueColorConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ColorValue colorValue)
            switch (parameter)
            {
                default:
                case "Argb":
                    return Color.FromUInt32(colorValue.Argb);
                case "Rgba":
                    return Color.FromUInt32(colorValue.Rgba);
            }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Color color)
            return new ColorValue(color.R, color.G, color.B, color.A);

        return null;
    }
}