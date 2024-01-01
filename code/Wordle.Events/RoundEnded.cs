using MediatR;

namespace Wordle.Events;

public class RoundEnded : BaseEvent
{
    public Guid SessionId { get; set; }
    public Guid RoundId { get; set; }
    
    public RoundEnded(string tenant, Guid sessionId, Guid roundId) : base(tenant)
    {
        SessionId = sessionId;
        RoundId = roundId;
    }
}