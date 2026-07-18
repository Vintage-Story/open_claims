using ProtoBuf;

namespace OpenClaims;

[ProtoContract]
public class ClaimRequestPacket
{
    [ProtoMember(1)] public int MinX;
    [ProtoMember(2)] public int MinZ;
    [ProtoMember(3)] public int MaxX;
    [ProtoMember(4)] public int MaxZ;
}

[ProtoContract]
public class ClaimResizePacket
{
    [ProtoMember(1)] public int ClaimIndex;
    [ProtoMember(2)] public int MinX;
    [ProtoMember(3)] public int MinZ;
    [ProtoMember(4)] public int MaxX;
    [ProtoMember(5)] public int MaxZ;
}

[ProtoContract]
public class ClaimRenamePacket
{
    [ProtoMember(1)] public int ClaimIndex;
    [ProtoMember(2)] public string NewName = "";
}

[ProtoContract]
public class ClaimDeletePacket
{
    [ProtoMember(1)] public int ClaimIndex;
}

[ProtoContract]
public class ClaimAllowPacket
{
    [ProtoMember(1)] public int ClaimIndex;
    [ProtoMember(2)] public string PlayerName = "";
}

[ProtoContract]
public class ClaimUnallowPacket
{
    [ProtoMember(1)] public int ClaimIndex;
    [ProtoMember(2)] public string PlayerUID = "";
}

[ProtoContract]
public class ClaimResponsePacket
{
    [ProtoMember(1)] public string Message = "";
    [ProtoMember(2)] public bool Success;
}

