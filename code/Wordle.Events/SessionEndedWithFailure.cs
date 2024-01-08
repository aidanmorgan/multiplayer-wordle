namespace Wordle.Events;

public class SessionEndedWithFailure : BaseEvent
{
    public Guid SessionId { get; set; }
    public long SessionVersion { get; set; }
 

    public SessionEndedWithFailure(string tenant, Guid id, long sessionVersion) : base(tenant)
    {
        SessionId = id;
        SessionVersion = sessionVersion;
    }
}