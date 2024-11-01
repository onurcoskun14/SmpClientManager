using PeterO.Cbor;

namespace SmpClient.Message;

public class ImageStatesRead : MessageBase
{
    public ImageStatesRead(byte? sequence = null)
    {
        CborData = CBORObject.NewMap().EncodeToBytes();
        Header = new Header(OP.READ, Version.V2, 0, (ushort)CborData.Length, (ushort)GroupId.IMAGE_MANAGEMENT, (byte)ImageManagement.STATE, sequence);
        SmpData = [.. Header.ToBytes(), .. CborData];
    }
}
