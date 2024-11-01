using PeterO.Cbor;

namespace SmpClient.Message;

public class ImageStatesWrite : MessageBase
{

    public ImageStatesWrite(byte[] hash, bool confirm, Header? header = null, byte? sequence = null)
    {
        CborData = SerializeToCbor(hash, confirm);
        Header = header ?? new Header(OP.WRITE, Version.V2, 0, (ushort)CborData.Length, (ushort)GroupId.IMAGE_MANAGEMENT, (byte)ImageManagement.STATE, sequence);

        SmpData = [.. Header.ToBytes(), .. CborData];
    }

    private static byte[] SerializeToCbor(byte[] hash, bool confirm)
    {
        var cborObject = CBORObject.NewOrderedMap()
            .Add("hash", hash)
            .Add("confirm", confirm);

        return cborObject.EncodeToBytes();
    }
}