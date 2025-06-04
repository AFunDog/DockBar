using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Data.Converters;

namespace DockBar.AvaloniaApp.Converters;

public class PercentageConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var percentage = double.TryParse(parameter?.ToString(), out var res) ? res : 1;
        return value switch
        {
            double real => real * percentage,
            int num => (int)(num * percentage),
            _ => value
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}