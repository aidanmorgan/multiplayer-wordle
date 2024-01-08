using Wordle.Model;

namespace Wordle.Apps.GameEventProcessor;

public class TimeoutPayload
{
    public Guid SessionId { get; set; }
    public long SessionVersion { get; set; }
    
    public Guid RoundId { get; set; }
    public long RoundVersion { get; set; }

    public VersionId<Session> VersionedSession => new VersionId<Session>(SessionId, SessionVersion);
    public VersionId<Round> VersionedRound => new VersionId<Round>(RoundId, RoundVersion);
    
    
    public DateTimeOffset Timeout { get; set; }

    public TimeoutPayload(Guid sessionId, long sessionVersion, Guid roundId, long roundVersion,DateTimeOffset timeout)
    {
        SessionId = sessionId;
        SessionVersion = sessionVersion;
        RoundId = roundId;
        RoundVersion = roundVersion;
        Timeout = timeout;
    }
    
    
}