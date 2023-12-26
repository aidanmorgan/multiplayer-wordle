namespace Wordle.Events;

public class GuessAdded : IEvent
{
    public Guid Id { get; private set; } = Ulid.NewUlid().ToGuid();
    
    public string EventType
    {
        get => GetType().Name;
        set { // no-op
        }
    }    
    
    public Guid GuessId { get; private set; }
    public Guid RoundId { get; private set; }
    
    public Guid SessionId { get; private set; }

    public GuessAdded(Guid guessId, Guid roundId, Guid sessionId)
    {
        GuessId = guessId;
        RoundId = roundId;
        SessionId = sessionId;
    }
}