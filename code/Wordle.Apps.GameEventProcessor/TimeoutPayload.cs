namespace Wordle.Apps.GameEventProcessor;

public class TimeoutPayload
{
    public Guid SessionId { get; set; }
    
    public Guid RoundId { get; set; }
    
    public DateTimeOffset Timeout { get; set; }

    public TimeoutPayload(Guid sessionId, Guid roundId, DateTimeOffset timeout)
    {
        SessionId = sessionId;
        RoundId = roundId;
        Timeout = timeout;
    }
}