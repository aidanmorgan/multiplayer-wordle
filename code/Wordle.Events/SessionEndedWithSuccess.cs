namespace Wordle.Events;

public class SessionEndedWithSuccess : BaseEvent
{
    public Guid SessionId { get; private set; }
    public long SessionVersion { get; private set; }
    
    public SessionEndedWithSuccess(string tenant, Guid id, long sessionVersion) : base(tenant)
    {
        SessionId = id;
        SessionVersion = sessionVersion;
    }
}