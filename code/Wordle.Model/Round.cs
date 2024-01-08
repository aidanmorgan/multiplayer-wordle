namespace Wordle.Model;

public class Round : IAggregate, IVersioned
{
    public Guid Id { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public Guid SessionId { get; set; }

    public RoundState State { get; set; }
    
    public string? Guess { get; set; }

    public List<LetterState> Result { get; set; }

    public Round()
    {
        Result = new List<LetterState>();
        State = RoundState.INACTIVE;
        CreatedAt = DateTimeOffset.MinValue;
    }

    public long Version { get; set; }
}