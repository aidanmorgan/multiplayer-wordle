namespace Wordle.Events;

public class GuessAdded : BaseEvent
{
    public Guid GuessId { get; private set; }
    public Guid RoundId { get; private set; }
    
    public Guid SessionId { get; private set; }

    public GuessAdded(string tenant, Guid guessId, Guid roundId, Guid sessionId) : base(tenant)
    {
        GuessId = guessId;
        RoundId = roundId;
        SessionId = sessionId;
    }
}