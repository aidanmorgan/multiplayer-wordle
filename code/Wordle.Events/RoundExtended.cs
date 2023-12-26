namespace Wordle.Events;

public class RoundExtended  : IEvent
{
    public Guid Id { get; private set; } = Ulid.NewUlid().ToGuid();
    
    public string EventType
    {
        get => GetType().Name;
        set { // no-op
        }
    }    
    
    public Guid RoundId
    {
        get; private set;
    }
    
    public Guid SessionId { get; private set; }
    
    public DateTimeOffset RoundExpiry { get; private set; }

    public RoundExtended(Guid sessionId, Guid roundId, DateTimeOffset roundExpiry)
    {
        RoundId = roundId;
        SessionId = sessionId;
        RoundExpiry = roundExpiry;
    }
}