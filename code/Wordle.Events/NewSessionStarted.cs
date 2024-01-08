using System.Text.Json.Serialization;

namespace Wordle.Events;

public class NewSessionStarted : BaseEvent
{
    public Guid SessionId { get; private set; }
    public long SessionVersion { get; private set; }

    public NewSessionStarted(string tenant, Guid id, long version) : base(tenant)
    {
        SessionId = id;
        SessionVersion = version;
    }
}