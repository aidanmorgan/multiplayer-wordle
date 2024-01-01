namespace Wordle.Events;

public class BaseEvent : IEvent
{
    public Guid Id { get; private set; } = Ulid.NewUlid().ToGuid();
    
    public string EventType
    {
        get => GetType().Name;
        set { // no-op
        }
    }

    public string Tenant { get; set; }
    public string EventSourceId { get; set; }
    public string EventSourceType { get; set; }
    public DateTimeOffset Timestamp { get; set; }

    public BaseEvent(string tenant)
    {
        this.Tenant = tenant;
    }
}