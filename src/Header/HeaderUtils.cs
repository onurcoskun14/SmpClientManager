namespace SmpClient;

public static class HeaderUtils
{
    private const int OP_SHIFT = 0;
    private const int VERSION_SHIFT = 3;
    private const int OP_MASK = 0b111;
    private const int VERSION_MASK = 0b11;

    public static byte PackOp(OP op)
    {
        return (byte)((int)op << OP_SHIFT);
    }

    public static OP UnpackOp(byte opAndVersionByte)
    {
        return (OP)((opAndVersionByte & OP_MASK) >> OP_SHIFT);
    }

    public static byte PackVersion(Version version)
    {
        return (byte)((int)version << VERSION_SHIFT);
    }

    public static Version UnpackVersion(byte opAndVersionByte)
    {
        return (Version)((opAndVersionByte >> VERSION_SHIFT) & VERSION_MASK);
    }

    public static byte PackOpAndVersion(OP op, Version version)
    {
        return (byte)(PackOp(op) | PackVersion(version));
    }
}

