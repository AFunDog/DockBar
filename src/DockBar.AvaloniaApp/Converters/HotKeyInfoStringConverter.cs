using System;
using System.Globalization;
using System.Text;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Avalonia.Data.Converters;
using Avalonia.Win32.Input;
using DockBar.Core.Structs;

namespace DockBar.AvaloniaApp.Converters;

internal sealed class HotKeyInfoStringConverter : IValueConverter
{
    public static HotKeyInfoStringConverter  Instance { get; } = new();
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is HotKeyInfo hotKeyInfo)
        {
            if (!hotKeyInfo.IsValid()) return string.Empty;
            
            StringBuilder sb = new();
            var modifiers = (HOT_KEY_MODIFIERS)hotKeyInfo.Modifiers;
            if (modifiers.HasFlag(HOT_KEY_MODIFIERS.MOD_ALT))
            {
                sb.Append("Alt+");
            }

            if (modifiers.HasFlag(HOT_KEY_MODIFIERS.MOD_CONTROL))
            {
                sb.Append("Ctrl+");
            }

            if (modifiers.HasFlag(HOT_KEY_MODIFIERS.MOD_SHIFT))
            {
                sb.Append("Shift+");
            }

            if (modifiers.HasFlag(HOT_KEY_MODIFIERS.MOD_WIN))
            {
                sb.Append("Win+");
            }

            sb.Append(KeyInterop.KeyFromVirtualKey((int)hotKeyInfo.Key, 0));
            return sb.ToString().TrimEnd('+');
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}