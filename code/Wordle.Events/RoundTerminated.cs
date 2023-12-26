namespace Wordle.Events;

public class RoundTerminated : IEvent
{
    public Guid Id { get; } = Ulid.NewUlid().ToGuid();
    
    public string EventType
    {
        get => GetType().Name;
        set { // no-op
        }
    }    
    
    public Guid SessionId { get; set; }
    public Guid RoundId { get; set; }

    public RoundTerminated(Guid sessionId, Guid roundId)
    {
        SessionId = sessionId;
        RoundId = roundId;
    }
}