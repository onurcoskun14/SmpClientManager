using System.IO;

namespace SmpClient.ImageUtils;

public class ImageTLV
{
    public byte Type { get; }
    public ushort Length { get; }
    public byte[] Value { get; }

    public ImageTLV(byte type, ushort length, byte[] value)
    {
        Type = type;
        Length = length;
        Value = value;
    }

    public static ImageTLV FromStream(BinaryReader reader)
    {
        byte type = reader.ReadByte();
        ushort length = reader.ReadUInt16();
        byte[] value = reader.ReadBytes(length);

        return new ImageTLV(type, length, value);
    }
}
