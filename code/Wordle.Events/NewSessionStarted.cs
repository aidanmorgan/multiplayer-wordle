using System.Text.Json.Serialization;

namespace Wordle.Events;

public class NewSessionStarted : IEvent
{
    public Guid Id { get; private set; } = Ulid.NewUlid().ToGuid();
    
    public string EventType
    {
        get => GetType().Name;
        set { // no-op
        }
    }    
    
    public Guid SessionId { get; private set; }
    public string Tenant { get; private set; }

    public NewSessionStarted(Guid id, string tenant)
    {
        SessionId = id;
        Tenant = tenant;
    }
}