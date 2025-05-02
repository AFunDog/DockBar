using System;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Avalonia.Controls;
using Avalonia.Media;
using DockBar.AvaloniaApp.Structs;
using static Windows.Win32.PInvoke;

namespace DockBar.AvaloniaApp.Helpers;

internal static class AcrylicHelper
{
    

    private delegate bool SetWindowCompositionAttribute(HWND hwnd, ref WindowCompositionAttributeData data);

    private static SetWindowCompositionAttribute? SetWindowCompositionAttributeFunc { get; set; }

    private static void TryGetSetWindowFunc()
    {
        if (SetWindowCompositionAttributeFunc is null)
        {
            var hModule = LoadLibrary("user32.dll");
            if (hModule.IsInvalid is false)
            {
                SetWindowCompositionAttributeFunc = GetProcAddress(hModule, "SetWindowCompositionAttribute")
                    .CreateDelegate<SetWindowCompositionAttribute>();
                
            }
        }
    }
    
    /// <summary>
    /// 使用 Win32API 让窗口应用亚克力效果
    /// </summary>
    /// <param name="topLevel">目标</param>
    /// <param name="color">颜色，可以调整透明度</param>
    /// <returns>是否应用成功</returns>
    public static bool EnableAcrylic(TopLevel topLevel, Color color)
    {
        var colorValue = new COLORREF((uint)((color.A << 24) + (color.B << 16) + (color.G << 8) + color.R));

        if (topLevel.TryGetPlatformHandle() is not { Handle: {} handle})
        {
            return false;
        }
        var hwnd = new HWND(handle);
        
        TryGetSetWindowFunc();

        if (SetWindowCompositionAttributeFunc is not null)
        {
            unsafe
            {
                var accent = new AccentPolicy
                {
                    AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND,
                    AccentFlags = 0,
                    GradientColor = (int)colorValue.Value,
                    AnimationId = 0
                };

                var data = new WindowCompositionAttributeData
                {
                    Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                    Data = (IntPtr)(&accent),
                    SizeOfData = Marshal.SizeOf(typeof(AccentPolicy))
                };
                return SetWindowCompositionAttributeFunc(hwnd, ref data);
            }
        }

        return false;
    }
    /// <summary>
    /// 使用 Win32API 让窗口取消亚克力效果
    /// </summary>
    /// <param name="topLevel">目标</param>
    /// <returns>是否取消成功</returns>
    public static bool DisableAcrylic(TopLevel topLevel)
    {
        if (topLevel.TryGetPlatformHandle() is not { Handle: {} handle})
        {
            return false;
        }
        var hwnd = new HWND(handle);
        
        TryGetSetWindowFunc();

        if (SetWindowCompositionAttributeFunc is not null)
        {
            unsafe
            {
                var accent = new AccentPolicy
                {
                    AccentState = AccentState.ACCENT_DISABLED,
                    AccentFlags = 0,
                    GradientColor = 0,
                    AnimationId = 0
                };

                var data = new WindowCompositionAttributeData
                {
                    Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
                    Data = (IntPtr)(&accent),
                    SizeOfData = Marshal.SizeOf(typeof(AccentPolicy))
                };
                return SetWindowCompositionAttributeFunc(hwnd, ref data);
            }
        }

        return false;
    }
    
    
}