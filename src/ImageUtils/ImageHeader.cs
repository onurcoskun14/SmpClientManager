
using System.IO;

namespace SmpClient.ImageUtils;

public class ImageHeader
{
    public const uint ImageMagic = 0x96F3B83D;
    public uint Magic { get; }
    public uint LoadAddr { get; }
    public ushort HdrSize { get; }
    public ushort ProtectTlvSize { get; }
    public uint ImgSize { get; }
    public uint Flags { get; }
    public ImageVersion Version { get; }

    public ImageHeader(uint magic, uint loadAddr, ushort hdrSize, ushort protectTlvSize, uint imgSize, uint flags, ImageVersion version)
    {
        if (magic != ImageMagic)
            throw new InvalidDataException($"Invalid magic value: {magic:X}");

        Magic = magic;
        LoadAddr = loadAddr;
        HdrSize = hdrSize;
        ProtectTlvSize = protectTlvSize;
        ImgSize = imgSize;
        Flags = flags;
        Version = version;
    }

    public static ImageHeader FromStream(Stream stream)
    {
        using var reader = new BinaryReader(stream, System.Text.Encoding.Default, leaveOpen: true); // Leave the stream open
        uint magic = reader.ReadUInt32();
        uint loadAddr = reader.ReadUInt32();
        ushort hdrSize = reader.ReadUInt16();
        ushort protectTlvSize = reader.ReadUInt16();
        uint imgSize = reader.ReadUInt32();
        uint flags = reader.ReadUInt32();
        byte[] versionData = reader.ReadBytes(8);
        var version = ImageVersion.FromBytes(versionData);

        return new ImageHeader(magic, loadAddr, hdrSize, protectTlvSize, imgSize, flags, version);
    }
}