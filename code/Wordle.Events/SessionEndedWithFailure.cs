namespace Wordle.Events;

public class SessionEndedWithFailure : BaseEvent
{
    public Guid SessionId { get; set; }
 

    public SessionEndedWithFailure(string tenant, Guid id) : base(tenant)
    {
        SessionId = id;
    }
}