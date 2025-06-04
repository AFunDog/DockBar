using Windows.Win32.UI.Input.KeyboardAndMouse;
using Avalonia.Input;
using Avalonia.Win32.Input;
using DockBar.Core.Structs;

namespace DockBar.AvaloniaApp.Extensions;

public static class HotKeyInfoExtension
{
    public static HotKeyInfo FromKey(this HotKeyInfo hotKeyInfo, Key key)
    {
        return key switch
        {
            Key.LeftCtrl or Key.RightCtrl => hotKeyInfo with { Modifiers = (uint)HOT_KEY_MODIFIERS.MOD_CONTROL },
            Key.LeftAlt or Key.RightAlt => hotKeyInfo with { Modifiers = (uint)HOT_KEY_MODIFIERS.MOD_ALT },
            Key.LeftShift or Key.RightShift => hotKeyInfo with { Modifiers = (uint)HOT_KEY_MODIFIERS.MOD_SHIFT },
            Key.LWin or Key.RWin => hotKeyInfo with { Modifiers = (uint)HOT_KEY_MODIFIERS.MOD_WIN },
            _ => hotKeyInfo with { Key = (uint)KeyInterop.VirtualKeyFromKey(key) }
        };
    }

    public static HotKeyInfo FromKey(this HotKeyInfo hotKeyInfo, Key key, KeyModifiers modifiers)
    {
        var res = hotKeyInfo.FromKey(key);
        var mod = (uint)modifiers;
        return res with { Modifiers = res.Modifiers | mod };
    }
}