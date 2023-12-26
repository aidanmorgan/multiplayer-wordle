namespace Wordle.Model;

public class Round : IAggregate
{
    public Guid Id { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.MinValue;
    public Guid SessionId { get; set; }

    public RoundState State { get; set; } = RoundState.INACTIVE;
    
    public string Guess { get; set; }
    
    public List<LetterState> Result { get; set; }
}