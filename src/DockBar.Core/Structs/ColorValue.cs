using MessagePack;

namespace DockBar.Core.Structs;

[MessagePackObject]
public readonly partial record struct ColorValue([property: Key(0)] uint Rgba)
{
    public ColorValue(byte r, byte g, byte b, byte a = Byte.MaxValue)
        : this((uint)((a << 24) + (r << 16) + (g << 8) + b))
    {
    }
    
    public static ColorValue FromArgb(uint argb) =>
        new(
            ((argb & 0xFF000000) >> 24) + ((argb & 0x00FFFFFF) << 8)
        );

    [IgnoreMember]
    public uint Argb => ((Rgba & 0x000000FF) << 24) + ((Rgba & 0xFFFFFF00) >> 8);
}