namespace DockBar.AvaloniaApp.Converters;

// public sealed partial class DockItemKeyConverter : AvaloniaObject, IValueConverter
// {
//     public static DockItemKeyConverter Instance { get; } = new();
//
//     public static readonly DirectProperty<DockItemKeyConverter, IDockItemService?> DockItemServiceProperty
//         = AvaloniaProperty.RegisterDirect<DockItemKeyConverter, IDockItemService?>(
//             nameof(DockItemService),
//             o => o.DockItemService,
//             (o, v) => o.DockItemService = v
//         );
//
//     public IDockItemService? DockItemService { get; set; }
//
//     public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
//     {
//         // if (typeof(DockItemBase).IsAssignableFrom(targetType) && value is int key &&
//         //     parameter is IDockItemService dockItemService)
//         // {
//         //     return dockItemService.GetDockItem(key);
//         // }
//         // else if (targetType == typeof(int) && value is DockItemBase item)
//         // {
//         //     return item.Key;
//         // }
//         // var tp = (CompiledBindingExtension)parameter;
//         // if (value is IEnumerable<int> keys && DockItemService is not null)
//         // {
//         //     return keys.Select(k => DockItemService.GetDockItem(k)).OfType<DockItemBase>().ToArray();
//         // }
//         // else if (value is IEnumerable<DockItemBase> items)
//         // {
//         //     return items.Select(i => i.Key);
//         // }
//         if (value is int key && DockItemService is not null)
//             return DockItemService.GetDockItem(key);
//         else if (value is DockItemBase item)
//             return item.Key;
//
//         return null;
//     }
//
//     public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
//     {
//         // if (value is IEnumerable<int> keys && DockItemService is not null)
//         // {
//         //     return keys.Select(k => DockItemService.GetDockItem(k)).OfType<DockItemBase>();
//         // }
//         // else if (value is IEnumerable<DockItemBase> items)
//         // {
//         //     return items.Select(i => i.Key);
//         // }
//
//         if (value is int key && DockItemService is not null)
//             return DockItemService.GetDockItem(key);
//         else if (value is DockItemBase item)
//             return item.Key;
//         return null;
//     }
// }