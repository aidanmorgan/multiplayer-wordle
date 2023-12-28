using System.Text.Json.Serialization;

namespace Wordle.Events;

public class NewRoundStarted : IEvent
{
    public Guid Id { get; private set; } = Ulid.NewUlid().ToGuid();
    
    public string EventType
    {
        get => GetType().Name;
        set { // no-op
        }
    }    
    
    public Guid RoundId { get; private set; }
    public Guid SessionId { get; private set; }
    
    public DateTimeOffset RoundExpiry { get; private set; }

    public NewRoundStarted(Guid sessionId, Guid roundId, DateTimeOffset roundExpiry)
    {
        RoundId = roundId;
        SessionId = sessionId;
        RoundExpiry = roundExpiry;
    }
}