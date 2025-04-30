using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace DockBar.AvaloniaApp.Converters;

public sealed class ExperimentalAcrylicBorderMaterialConverter : IMultiValueConverter
{
    public static ExperimentalAcrylicBorderMaterialConverter  Instance { get; } = new();
    
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        var material = new ExperimentalAcrylicMaterial()
        {
            TintColor = Colors.Black,
            TintOpacity = 0.8,
            MaterialOpacity = 0.4
        };
        try
        {
            material.TintColor = (Color)values[0]!;
            material.TintOpacity = (double)values[1]!;
            material.MaterialOpacity = (double)values[2]!;
        }
        catch (Exception e)
        {
            
        }

        return material;
    }
}