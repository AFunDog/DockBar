using Avalonia;
using Avalonia.Data.Converters;

namespace DockBar.AvaloniaApp.Converters;

public static class FuncValueConverters
{
    public static FuncValueConverter<double, Thickness> DoubleToThicknessNegTop { get; } = new(o => new(0, -o, 0, 0));
}