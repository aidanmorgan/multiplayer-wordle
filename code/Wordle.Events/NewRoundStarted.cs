using System.Text.Json.Serialization;

namespace Wordle.Events;

public class NewRoundStarted : BaseEvent
{
    public Guid RoundId { get; private set; }
    public Guid SessionId { get; private set; }
    public DateTimeOffset RoundExpiry { get; private set; }
    
    public bool IsFirstRound { get; private set; }


    public NewRoundStarted(string tenant, Guid sessionId, Guid roundId, DateTimeOffset roundExpiry, bool isFirstRound = false) : base(tenant)
    {
        RoundId = roundId;
        SessionId = sessionId;
        RoundExpiry = roundExpiry;
        IsFirstRound = isFirstRound;
    }
}