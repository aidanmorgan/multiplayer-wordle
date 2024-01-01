namespace Wordle.Events;

public class SessionTerminated : BaseEvent
{
    public Guid SessionId { get; private set; }

    public SessionTerminated(string tenant, Guid sessionId) : base(tenant)
    {
        SessionId = sessionId;
    }
}