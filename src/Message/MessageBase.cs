namespace SmpClient.Message;

public class MessageBase
{
    public Header Header { get; set; }
    public byte[] SmpData { get; set; }
    public byte[] CborData { get; set; }
}