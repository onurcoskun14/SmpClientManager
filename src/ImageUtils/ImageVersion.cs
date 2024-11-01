
using System.IO;

namespace SmpClient.ImageUtils;

public class ImageVersion
    {
        public byte Major { get; }
        public byte Minor { get; }
        public ushort Revision { get; }
        public uint BuildNum { get; }

        public ImageVersion(byte major, byte minor, ushort revision, uint buildNum)
        {
            Major = major;
            Minor = minor;
            Revision = revision;
            BuildNum = buildNum;
        }

        public static ImageVersion FromBytes(byte[] data)
        {
            using var reader = new BinaryReader(new MemoryStream(data));
            var major = reader.ReadByte();
            var minor = reader.ReadByte();
            var revision = reader.ReadUInt16();
            var buildNum = reader.ReadUInt32();
            return new ImageVersion(major, minor, revision, buildNum);
        }
    }
