using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Data.Converters;

namespace DockBar.AvaloniaApp.Converters;

internal sealed class DirectionTranslateConverter : IValueConverter
{
    public enum DirectionEnum
    {
        Left,
        Right,
        Up,
        Down,
    }

    public DirectionEnum Direction { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double dV && !double.IsNaN(dV))
            return Direction switch
            {
                DirectionEnum.Left => $"TranslateX({dV}px)",
                DirectionEnum.Right => $"TranslateX(-{dV}px)",
                DirectionEnum.Up => $"TranslateY({dV}px)",
                DirectionEnum.Down => $"TranslateY(-{dV}px)",
                _ => null,
            };
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}
