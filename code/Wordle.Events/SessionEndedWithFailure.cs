namespace Wordle.Events;

public class SessionEndedWithFailure : IEvent
{
    public Guid Id { get; private set; } = Ulid.NewUlid().ToGuid();
    
    public string EventType
    {
        get => GetType().Name;
        set { // no-op
        }
    }
    
    public Guid SessionId { get; private set; }

    public SessionEndedWithFailure(Guid id)
    {
        Id = id;
    }
}