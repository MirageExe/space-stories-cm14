using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Telephone;

[Serializable, NetSerializable]
public enum RMCTelephoneUiKey
{
    Key,
}

// SSCM start
[Serializable, NetSerializable]
public sealed class RMCTelephoneBuiState(
    List<RMCPhone> phones,
    bool canDnd,
    bool dnd,
    List<RMCCallLogEntry> callLog,
    List<NetEntity> blockedPhones) : BoundUserInterfaceState
{
    public readonly List<RMCPhone> Phones = phones;
    public readonly bool CanDnd = canDnd;
    public readonly bool Dnd = dnd;
    public readonly List<RMCCallLogEntry> CallLog = callLog;
    public readonly List<NetEntity> BlockedPhones = blockedPhones;
}

[Serializable, NetSerializable]
public sealed record RMCCallLogEntry(string CallerName, string? CallerJob, string DeviceName, string TimeString);

[Serializable, NetSerializable]
public sealed class RMCTelephoneBlockBuiMsg(NetEntity id, bool block) : BoundUserInterfaceMessage
{
    public readonly NetEntity Id = id;
    public readonly bool Block = block;
}
// SSCM end

[Serializable, NetSerializable]
public sealed class RMCTelephoneCallBuiMsg(NetEntity id) : BoundUserInterfaceMessage
{
    public readonly NetEntity Id = id;
}

[Serializable, NetSerializable]
public sealed class RMCTelephoneDndBuiMsg(bool dnd) : BoundUserInterfaceMessage
{
    public readonly bool Dnd = dnd;
}

[Serializable, NetSerializable]
public readonly record struct RMCPhone(NetEntity Id, string Category, string Name);

