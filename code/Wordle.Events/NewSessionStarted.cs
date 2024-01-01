using System.Text.Json.Serialization;

namespace Wordle.Events;

public class NewSessionStarted : BaseEvent
{
    public Guid SessionId { get; private set; }

    public NewSessionStarted(string tenant, Guid id) : base(tenant)
    {
        SessionId = id;
    }
}