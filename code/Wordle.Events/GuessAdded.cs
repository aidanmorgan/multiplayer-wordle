namespace Wordle.Events;

public class GuessAdded : BaseEvent
{
    public Guid GuessId { get; private set; }
    public Guid RoundId { get; private set; }
    public long RoundVersion { get; private set; }
    
    public Guid SessionId { get; private set; }
    public long SessionVersion { get; private set; }

    public GuessAdded(string tenant, Guid guessId, Guid roundId, long roundVersion, Guid sessionId, long sessionVersion) : base(tenant)
    {
        GuessId = guessId;
        RoundId = roundId;
        RoundVersion = roundVersion;
        SessionId = sessionId;
        SessionVersion = sessionVersion;
    }
}