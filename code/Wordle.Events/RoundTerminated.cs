namespace Wordle.Events;

public class RoundTerminated : BaseEvent
{
    public Guid SessionId { get; set; }
    public Guid RoundId { get; set; }
    

    public RoundTerminated(string tenant, Guid sessionId, Guid roundId) : base(tenant)
    {
        SessionId = sessionId;
        RoundId = roundId;
    }
}