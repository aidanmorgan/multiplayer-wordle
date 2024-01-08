namespace Wordle.Apps.GameEventProcessor;

public class TimeoutPayload
{
    public Guid SessionId { get; set; }
    public long SessionVersion { get; set; }
    
    public Guid RoundId { get; set; }
    public long RoundVersion { get; set; }
    
    public DateTimeOffset Timeout { get; set; }

    public TimeoutPayload(Guid sessionId, long sessionVersion, Guid roundId, long roundVersion,DateTimeOffset timeout)
    {
        SessionId = sessionId;
        SessionVersion = SessionVersion;
        RoundId = roundId;
        RoundVersion = roundVersion;
        Timeout = timeout;
    }
    
    
}