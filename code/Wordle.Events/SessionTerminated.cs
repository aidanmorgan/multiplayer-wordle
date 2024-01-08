namespace Wordle.Events;

public class SessionTerminated : BaseEvent
{
    public Guid SessionId { get; private set; }
    public long SessionVersion { get; private set; }

    public SessionTerminated(string tenant, Guid sessionId, long sessionVersion) : base(tenant)
    {
        SessionId = sessionId;
        SessionVersion = sessionVersion;
    }
}