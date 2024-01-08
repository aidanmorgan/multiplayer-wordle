using System.Runtime.Serialization;
using Wordle.Model;
using Wordle.Queries;

namespace Wordle.Events;

public class RoundExtended  : BaseEvent
{
    public Guid RoundId
    {
        get; init;
    }
    
    public long RoundVersion { get; set; }
    
    public Guid SessionId { get;  init; }
    
    public long SessionVersion { get; set; }
    
    public DateTimeOffset RoundExpiry { get;  init; }
    
    public RoundExtensionReason Reason { get; init; }

    [IgnoreDataMember]
    public VersionId<Session> VersionedSession => new(SessionId, SessionVersion);
    [IgnoreDataMember]
    public VersionId<Round> VersionedRound => new(RoundId, RoundVersion);

    public RoundExtended(string tenant, Guid sessionId, long sessionVersion, Guid roundId, long roundVersion, DateTimeOffset roundExpiry, RoundExtensionReason reason) : base(tenant)
    {
        RoundId = roundId;
        RoundVersion = roundVersion;
        SessionId = sessionId;
        SessionVersion = sessionVersion;
        RoundExpiry = roundExpiry;
        Reason = reason;
    }
}