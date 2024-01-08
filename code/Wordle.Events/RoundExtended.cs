using Wordle.Model;

namespace Wordle.Events;

public class RoundExtended  : BaseEvent
{
    public Guid RoundId
    {
        get; init;
    }
    
    public long RoundVersion { get; set; }
    
    public Guid SessionId { get;  init; }
    
    public long SessionVersion { get; set; }
    
    public DateTimeOffset RoundExpiry { get;  init; }
    
    public RoundExtensionReason Reason { get; init; }
    


    public RoundExtended(string tenant, Guid sessionId, long sessionVersion, Guid roundId, long roundVersion, DateTimeOffset roundExpiry, RoundExtensionReason reason) : base(tenant)
    {
        RoundId = roundId;
        RoundVersion = roundVersion;
        SessionId = sessionId;
        SessionVersion = sessionVersion;
        RoundExpiry = roundExpiry;
        Reason = reason;
    }
}