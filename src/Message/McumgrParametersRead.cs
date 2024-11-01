using PeterO.Cbor;

namespace SmpClient.Message;

public class McumgrParametersRead : MessageBase
{
    public McumgrParametersRead(byte? sequence = null)
    {
        CborData = CBORObject.NewMap().EncodeToBytes();
        Header = new Header(OP.READ, Version.V2, 0, (ushort)CborData.Length, (ushort)GroupId.OS_MANAGEMENT, (byte)OSManagement.MCUMGR_PARAMETERS, sequence);
        SmpData = [.. Header.ToBytes(), .. CborData];
    }
}