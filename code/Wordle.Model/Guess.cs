namespace Wordle.Model;


public class Guess : IAggregate
{
    public Guid Id { get; set; }
    public string Word { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string User { get; set; }
    
    public Guid SessionId { get; set; }
    public Guid RoundId { get; set; }
}