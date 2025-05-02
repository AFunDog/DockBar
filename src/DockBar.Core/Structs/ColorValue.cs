using MessagePack;

namespace DockBar.Core.Structs;

[MessagePackObject]
public readonly partial record struct ColorValue([property: Key(0)] uint Rgba)
{
    [IgnoreMember]
    public byte R => (byte)(Rgba >> 24);
    [IgnoreMember]
    public byte G => (byte)(Rgba >> 16);
    [IgnoreMember]
    public byte B => (byte)(Rgba >> 8);
    [IgnoreMember]
    public byte A => (byte)Rgba;
    
    public ColorValue(byte r, byte g, byte b, byte a = Byte.MaxValue)
        : this((uint)(a + (r << 24) + (g << 16) + (b << 8)))
    {
    }
    
    public static ColorValue FromArgb(uint argb) =>
        new(
            ((argb & 0xFF000000) >> 24) + ((argb & 0x00FFFFFF) << 8)
        );

    [IgnoreMember]
    public uint Argb => ((Rgba & 0x000000FF) << 24) + ((Rgba & 0xFFFFFF00) >> 8);
}