using System;
using System.Runtime.InteropServices;

namespace DockBar.AvaloniaApp.Structs;

public enum AccentState
{
    ACCENT_DISABLED = 0,
    ACCENT_ENABLE_GRADIENT,
    ACCENT_ENABLE_TRANSPARENTGRADIENT,
    ACCENT_ENABLE_BLURBEHIND,
    ACCENT_ENABLE_ACRYLICBLURBEHIND,
    ACCENT_INVALID_STATE
}

[StructLayout(LayoutKind.Sequential)]
public struct AccentPolicy
{
    public AccentState AccentState;
    public int AccentFlags;
    public int GradientColor;
    public int AnimationId;
}

[StructLayout(LayoutKind.Sequential)]
public struct WindowCompositionAttributeData
{
    public WindowCompositionAttribute Attribute;
    public IntPtr Data;
    public int SizeOfData;
}

public enum WindowCompositionAttribute
{
    WCA_ACCENT_POLICY = 19
}