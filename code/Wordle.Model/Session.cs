namespace Wordle.Model;

public class Session : IAggregate
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.MinValue;
    public string Tenant { get; set; }
    
    public SessionState State = SessionState.INACTIVE;

    public string Word { get; set; }
    
    public List<string> UsedLetters { get; set; }

    public Guid? ActiveRoundId { get; set; } = null;
    
    public DateTimeOffset? ActiveRoundEnd = null;
}