using PeterO.Cbor;

namespace SmpClient.Message;

public class ImageUploadWrite : MessageBase
{

    public int Off { get; set; }
    public byte[] Data { get; set; }
    public int? Image { get; set; }
    public int? Length { get; set; }
    public byte[]? Sha { get; set; }
    public bool? Upgrade { get; set; }

    public ImageUploadWrite(int off, byte[] data, int? image = null, int? length = null, byte[]? sha = null, bool? upgrade = null, byte? sequence = null, Header? header = null)
    {
        Off = off;
        Data = data;
        Image = image;
        Length = length;
        Sha = sha;
        Upgrade = upgrade;
        CborData = SerializeToCbor(off, data, image, length, sha, upgrade);
        Header = header ?? new Header(OP.WRITE, Version.V2, 0, (ushort)CborData.Length, (ushort)GroupId.IMAGE_MANAGEMENT, (byte)ImageManagement.UPLOAD, sequence);

        SmpData = [.. Header.ToBytes(), .. CborData];
    }

    private byte[] SerializeToCbor(int off, byte[] data, int? image, int? length, byte[]? sha, bool? upgrade)
    {
        var cborObject = CBORObject.NewOrderedMap()
            .Add("off", off)
            .Add("data", data);

        if (image.HasValue)
            cborObject.Add("image", image.Value);

        if (length.HasValue)
            cborObject.Add("len", length.Value);

        if (sha != null)
            cborObject.Add("sha", sha);

        if (upgrade.HasValue)
            cborObject.Add("upgrade", upgrade.Value);

        return cborObject.EncodeToBytes();
    }
}
