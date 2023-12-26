namespace Wordle.Events;

public class SessionEndedWithSuccess : IEvent
{
    public Guid Id { get; private set; } = Ulid.NewUlid().ToGuid();
    
    public string EventType
    {
        get => GetType().Name;
        set { // no-op
        }
    }    
    
    public Guid SessionId { get; private set; }

    public SessionEndedWithSuccess(Guid id)
    {
        SessionId = id;
    }
}