using PeterO.Cbor;

namespace SmpClient.Message;

public class ResetWrite : MessageBase
{
    public ResetWrite(byte? sequence = null, Header? header = null)
    {

        CborData = CBORObject.NewMap().EncodeToBytes();
        Header = header ?? new Header(OP.WRITE, Version.V2, 0, (ushort)CborData.Length, (ushort)GroupId.OS_MANAGEMENT, (byte)OSManagement.RESET, sequence);
        SmpData = [.. Header.ToBytes(), .. CborData];
    }
}