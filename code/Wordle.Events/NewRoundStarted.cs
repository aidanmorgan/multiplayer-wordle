using System.Text.Json.Serialization;

namespace Wordle.Events;

public class NewRoundStarted : BaseEvent
{
    public Guid RoundId { get; private set; }
    public long RoundVersion { get; private set; }
    public Guid SessionId { get; private set; }
    public long SessionVersion { get; private set; }
    public DateTimeOffset RoundExpiry { get; private set; }
    
    public bool IsFirstRound { get; private set; }


    public NewRoundStarted(string tenant, Guid sessionId, long sessionVersion, Guid roundId, long roundVersion, DateTimeOffset roundExpiry, bool isFirstRound = false) : base(tenant)
    {
        RoundId = roundId;
        RoundVersion = roundVersion;
        SessionId = sessionId;
        SessionVersion = sessionVersion;
        RoundExpiry = roundExpiry;
        IsFirstRound = isFirstRound;
    }
}