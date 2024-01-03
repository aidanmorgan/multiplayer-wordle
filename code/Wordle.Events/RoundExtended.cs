using Wordle.Model;

namespace Wordle.Events;

public class RoundExtended  : BaseEvent
{
    public Guid RoundId
    {
        get; init;
    }
    
    public Guid SessionId { get;  init; }
    
    public DateTimeOffset RoundExpiry { get;  init; }
    
    public RoundExtensionReason Reason { get; init; }
    


    public RoundExtended(string tenant, Guid sessionId, Guid roundId, DateTimeOffset roundExpiry, RoundExtensionReason reason) : base(tenant)
    {
        RoundId = roundId;
        SessionId = sessionId;
        RoundExpiry = roundExpiry;
        Reason = reason;
    }
}