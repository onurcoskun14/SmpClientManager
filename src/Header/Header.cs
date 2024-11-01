
using System.Collections.Generic;

namespace SmpClient;

public class Header
{
    private static byte _nextSequence = 0;

    public OP Op { get; private set; }
    public Version Version { get; private set; }
    public byte Flags { get; }
    public ushort Length { get; }
    public ushort GroupId { get; }
    public byte Sequence { get; }
    public byte CommandId { get; }

    public Header(OP op, Version version, byte flags, ushort length, ushort groupId, byte commandId, byte? sequence = null)
    {
        Op = op;
        Version = version;
        Flags = flags;
        Length = length;
        GroupId = groupId;
        Sequence = sequence ?? _nextSequence++;
        CommandId = commandId;
    }

    public byte[] ToBytes()
    {
        byte opAndVersion = HeaderUtils.PackOpAndVersion(Op, Version);

        List<byte> bytes =
        [
            opAndVersion,
            Flags,
            (byte)(Length >> 8),
            (byte)Length,
            (byte)(GroupId >> 8),
            (byte)GroupId,
            Sequence,
            CommandId
        ];

        return [.. bytes];
    }

    public override string ToString()
    {
        return $"Header(op=<OP.{Op}: {(int)Op}>, version=<Version.{Version}: {(int)Version}>, flags=<Flag: {Flags}>, length={Length}, group_id=<GroupId.{GroupId}: {(int)GroupId}>, sequence={Sequence}, command_id=<OSManagement.{CommandId}: {(int)CommandId}>)";
    }
}

public enum OP
{
    READ = 0,
    READ_RSP = 1,
    WRITE = 2,
    WRITE_RSP = 3
}

public enum Version
{
    V1 = 0,
    V2 = 1
}

public enum GroupId
{
    OS_MANAGEMENT = 0,
    IMAGE_MANAGEMENT = 1
}

public enum ImageManagement
{
    STATE = 0,
    UPLOAD = 1,
    FILE = 2,
    CORELIST = 3,
    CORELOAD = 4,
    ERASE = 5
}

public enum OSManagement
{
    ECHO = 0,
    ECHO_CONTROL = 1,
    TASK_STATS = 2,
    MEMORY_POOL_STATS = 3,
    DATETIME_STRING = 4,
    RESET = 5,
    MCUMGR_PARAMETERS = 6,
    OS_APPLICATION_INFO = 7,
    BOOTLOADER_INFO = 8
}
