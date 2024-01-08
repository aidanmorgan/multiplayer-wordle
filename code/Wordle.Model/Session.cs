namespace Wordle.Model;

public class Session : IAggregate, IVersioned
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string Tenant { get; set; }
    
    public SessionState State { get; set; }

    public string Word { get; set; }

    public List<string> UsedLetters { get; set; }

    public Guid? ActiveRoundId { get; set; } = null;
    
    public DateTimeOffset? ActiveRoundEnd { get; set; }

    public Session()
    {
        CreatedAt = DateTimeOffset.MinValue;
        State = SessionState.INACTIVE;
        UsedLetters = new List<string>();
        ActiveRoundId = null;
        ActiveRoundEnd = null;
    }

    public long Version { get; set; } = 0;
}