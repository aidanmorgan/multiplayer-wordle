using System.Runtime.Serialization;
using Wordle.Model;

namespace Wordle.Events;

public class RoundTerminated : BaseEvent
{
    public Guid SessionId { get; set; }
    public long SessionVersion { get; set; }
    public Guid RoundId { get; set; }
    public long RoundVersion { get; set; }
    
    [IgnoreDataMember]
    public VersionId<Session> VersionedSession => new(SessionId, SessionVersion);
    [IgnoreDataMember]
    public VersionId<Round> VersionedRound => new(RoundId, RoundVersion);

    

    public RoundTerminated(string tenant, Guid sessionId, long sessionVersion, Guid roundId, long roundVersion) : base(tenant)
    {
        SessionId = sessionId;
        SessionVersion = sessionVersion;
        RoundId = roundId;
        RoundVersion = roundVersion;
    }
}