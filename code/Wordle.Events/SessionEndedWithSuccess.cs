namespace Wordle.Events;

public class SessionEndedWithSuccess : BaseEvent
{
    public Guid SessionId { get; private set; }
    
    public SessionEndedWithSuccess(string tenant, Guid id) : base(tenant)
    {
        SessionId = id;
    }
}