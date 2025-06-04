using System.Text;
using MessagePack;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace DockBar.Core.Structs;

/// <summary>
/// 按键信息
/// 使用 Win32API 的枚举值
/// 而不是 Avalonia 的枚举值
/// </summary>
[MessagePackObject]
public readonly record struct HotKeyInfo([property: Key(0)] uint Modifiers, [property: Key(1)] uint Key)
{
    public bool IsValid() => Key != 0;
}