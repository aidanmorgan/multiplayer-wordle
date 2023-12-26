namespace Wordle.Events;

public class SessionTerminated : IEvent
{
    public Guid Id { get; } = Ulid.NewUlid().ToGuid();
    
    public string EventType
    {
        get => GetType().Name;
        set { // no-op
        }
    }    
    
    public Guid SessionId { get; private set; }

    public SessionTerminated(Guid sessionId)
    {
        SessionId = sessionId;
    }
}