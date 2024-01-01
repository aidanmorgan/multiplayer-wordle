namespace Wordle.Events;

public class RoundExtended  : BaseEvent
{
    public Guid RoundId
    {
        get; set;
    }
    
    public Guid SessionId { get;  set; }
    
    public DateTimeOffset RoundExpiry { get;  set; }
    


    public RoundExtended(string tenant, Guid sessionId, Guid roundId, DateTimeOffset roundExpiry) : base(tenant)
    {
        RoundId = roundId;
        SessionId = sessionId;
        RoundExpiry = roundExpiry;
    }
}